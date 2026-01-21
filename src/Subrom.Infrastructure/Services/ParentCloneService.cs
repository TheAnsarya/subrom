using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Subrom.Application.Interfaces;
using Subrom.Domain.Aggregates.Organization;

namespace Subrom.Infrastructure.Services;

/// <summary>
/// Service for managing parent/clone relationships.
/// </summary>
public partial class ParentCloneService : IParentCloneService {
	private readonly ILogger<ParentCloneService> _logger;
	private readonly IDatFileRepository _datFileRepository;
	private readonly Dictionary<Guid, ParentCloneIndex> _indexCache = [];

	public ParentCloneService(ILogger<ParentCloneService> logger, IDatFileRepository datFileRepository) {
		_logger = logger;
		_datFileRepository = datFileRepository;
	}

	/// <inheritdoc />
	public async Task<string?> GetParentAsync(string romName, Guid? datFileId = null, CancellationToken cancellationToken = default) {
		if (datFileId.HasValue) {
			var index = await GetOrBuildIndexAsync(datFileId.Value, cancellationToken);
			return index?.GetParent(romName);
		}

		// No DAT - try to infer from name
		return InferParentFromName(romName);
	}

	/// <inheritdoc />
	public async Task<IReadOnlyList<string>> GetClonesAsync(string parentName, Guid? datFileId = null, CancellationToken cancellationToken = default) {
		if (datFileId.HasValue) {
			var index = await GetOrBuildIndexAsync(datFileId.Value, cancellationToken);
			return index?.GetClones(parentName) ?? [];
		}

		return [];
	}

	/// <inheritdoc />
	public async Task<IReadOnlyList<ParentCloneGroup>> GroupByParentAsync(IEnumerable<string> romNames, Guid? datFileId = null, CancellationToken cancellationToken = default) {
		var names = romNames.ToList();

		if (datFileId.HasValue) {
			var index = await GetOrBuildIndexAsync(datFileId.Value, cancellationToken);
			if (index != null) {
				return GroupByIndex(names, index);
			}
		}

		// Fallback to inference
		return InferRelationships(names);
	}

	/// <inheritdoc />
	public async Task<ParentCloneIndex> BuildIndexFromDatAsync(Guid datFileId, CancellationToken cancellationToken = default) {
		_logger.LogInformation("Building parent/clone index for DAT {DatId}", datFileId);

		var datFile = await _datFileRepository.GetByIdWithGamesAsync(datFileId, cancellationToken);
		if (datFile == null) {
			throw new KeyNotFoundException($"DAT file not found: {datFileId}");
		}

		var index = new ParentCloneIndex { DatFileId = datFileId };

		// Build index from game entries that have CloneOf set
		foreach (var game in datFile.Games) {
			if (!string.IsNullOrEmpty(game.CloneOf)) {
				index.AddRelationship(game.CloneOf, game.Name);
			}
		}

		// Cache the index
		_indexCache[datFileId] = index;

		_logger.LogInformation("Built index with {Parents} parents and {Clones} clones",
			index.ParentCount, index.CloneCount);

		return index;
	}

	/// <inheritdoc />
	public IReadOnlyList<ParentCloneGroup> InferRelationships(IEnumerable<string> romNames) {
		var names = romNames.ToList();
		var groups = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

		foreach (var name in names) {
			// Get clean name removing region AND revision/version markers
			// This groups all variants of the same game together
			var groupKey = GetGroupKey(name);

			if (!groups.TryGetValue(groupKey, out var members)) {
				members = [];
				groups[groupKey] = members;
			}
			members.Add(name);
		}

		// Convert to ParentCloneGroup, determining which is parent
		return groups.Select(kvp => {
			var members = kvp.Value;
			// The parent is the one with the lowest complexity (usually Japan or simplest name)
			var orderedMembers = members.OrderBy(m => GetNameComplexity(m)).ToList();
			var parent = orderedMembers.First();
			var clones = orderedMembers.Skip(1).ToList();

			return new ParentCloneGroup {
				Parent = parent,
				Clones = clones
			};
		}).ToList();
	}

	private static string GetGroupKey(string name) {
		// Remove region, revision, and verification markers to group all variants together
		var cleaned = name;
		cleaned = RegionMarkerRegex().Replace(cleaned, "");
		cleaned = RevisionMarkerRegex().Replace(cleaned, "");
		cleaned = VerificationMarkerRegex().Replace(cleaned, "");
		return cleaned.Trim();
	}

	private async Task<ParentCloneIndex?> GetOrBuildIndexAsync(Guid datFileId, CancellationToken cancellationToken) {
		if (_indexCache.TryGetValue(datFileId, out var cached)) {
			return cached;
		}

		try {
			return await BuildIndexFromDatAsync(datFileId, cancellationToken);
		} catch (Exception ex) {
			_logger.LogWarning(ex, "Failed to build index for DAT {DatId}", datFileId);
			return null;
		}
	}

	private static IReadOnlyList<ParentCloneGroup> GroupByIndex(IReadOnlyList<string> names, ParentCloneIndex index) {
		var groups = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

		foreach (var name in names) {
			var parent = index.GetParent(name) ?? name;

			if (!groups.TryGetValue(parent, out var members)) {
				members = [];
				groups[parent] = members;
			}

			if (!string.Equals(name, parent, StringComparison.OrdinalIgnoreCase)) {
				members.Add(name);
			}
		}

		return groups.Select(kvp => new ParentCloneGroup {
			Parent = kvp.Key,
			Clones = kvp.Value
		}).ToList();
	}

	private static string? InferParentFromName(string name) {
		// Check for common clone indicators
		// Examples: "Super Mario Bros 2 (USA)" is clone of "Super Mario Bros 2 (Japan)"
		// "Zelda (USA) (Rev A)" might be revision of "Zelda (USA)"

		// If name contains version/revision markers, the base might be the parent
		if (RevisionMarkerRegex().IsMatch(name)) {
			var baseName = RevisionMarkerRegex().Replace(name, "").Trim();
			if (!string.Equals(baseName, name, StringComparison.OrdinalIgnoreCase)) {
				return baseName;
			}
		}

		return null;
	}

	private static string GetCleanNameForGrouping(string name) {
		// Remove region, revision, and other markers for grouping
		var cleaned = name;

		// Remove region markers
		cleaned = RegionMarkerRegex().Replace(cleaned, "");

		// Remove revision markers
		cleaned = RevisionMarkerRegex().Replace(cleaned, "");

		// Remove verification markers
		cleaned = VerificationMarkerRegex().Replace(cleaned, "");

		return cleaned.Trim();
	}

	private static int GetNameComplexity(string name) {
		var complexity = name.Length;

		// Add penalty for revision markers (clones often have revisions)
		if (RevisionMarkerRegex().IsMatch(name)) complexity += 100;

		// Add penalty for special regions (non-primary regions might be clones)
		if (name.Contains("(Japan)", StringComparison.OrdinalIgnoreCase)) complexity += 20;
		if (name.Contains("(Europe)", StringComparison.OrdinalIgnoreCase)) complexity += 10;

		// Add penalty for special markers
		if (name.Contains("[b]", StringComparison.OrdinalIgnoreCase)) complexity += 200;
		if (name.Contains("[!]", StringComparison.OrdinalIgnoreCase)) complexity -= 10; // Verified is good

		return complexity;
	}

	[GeneratedRegex(@"\s*\(Rev\s*[A-Z0-9]+\)|\s*\(v?\d+\.?\d*\)", RegexOptions.IgnoreCase)]
	private static partial Regex RevisionMarkerRegex();

	[GeneratedRegex(@"\s*\((USA|Europe|Japan|World|Germany|France|Spain|Italy|Netherlands|Sweden|Korea|China|Australia|Brazil)(?:,\s*[^)]+)?\)", RegexOptions.IgnoreCase)]
	private static partial Regex RegionMarkerRegex();

	[GeneratedRegex(@"\s*\[!?\]|\s*\[b\d*\]|\s*\[o\d*\]|\s*\[a\d*\]", RegexOptions.IgnoreCase)]
	private static partial Regex VerificationMarkerRegex();
}
