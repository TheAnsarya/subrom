using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Subrom.Application.Interfaces;
using Subrom.Domain.Aggregates.Organization;

namespace Subrom.Infrastructure.Services;

/// <summary>
/// Service for 1G1R (One Game One ROM) filtering.
/// </summary>
public partial class OneGameOneRomService : IOneGameOneRomService {
	private readonly ILogger<OneGameOneRomService> _logger;

	public OneGameOneRomService(ILogger<OneGameOneRomService> logger) {
		_logger = logger;
	}

	/// <inheritdoc />
	public IReadOnlyList<RomCandidate> Filter(IEnumerable<RomCandidate> roms, OneGameOneRomOptions options) {
		var groups = GroupAndSelect(roms, options);
		return groups.Select(g => g.Selected).ToList();
	}

	/// <inheritdoc />
	public IReadOnlyList<RomGroup> GroupAndSelect(IEnumerable<RomCandidate> roms, OneGameOneRomOptions options) {
		var romList = roms.ToList();
		_logger.LogDebug("Processing {Count} ROMs for 1G1R selection", romList.Count);

		// Group by clean name (or parent name if available)
		var groups = romList
			.GroupBy(r => GetGroupKey(r))
			.Select(g => CreateRomGroup(g, options))
			.ToList();

		_logger.LogInformation("1G1R filtered {Input} ROMs to {Output} games",
			romList.Count, groups.Count);

		return groups;
	}

	/// <inheritdoc />
	public int ScoreRom(RomCandidate rom, OneGameOneRomOptions options) {
		var score = 0;

		// Check for excluded categories first
		if (rom.Categories.Any(c => options.ExcludeCategories.Contains(c, StringComparer.OrdinalIgnoreCase))) {
			return -1000; // Effectively exclude
		}

		// Region priority (higher = better, max 100 points)
		if (!string.IsNullOrEmpty(rom.Region)) {
			var regionIndex = GetIndex(rom.Region, options.RegionPriority);
			if (regionIndex >= 0) {
				score += (options.RegionPriority.Count - regionIndex) * 10;
			}
		}

		// Language priority (max 50 points)
		if (!string.IsNullOrEmpty(rom.Languages)) {
			var langIndex = GetIndex(rom.Languages, options.LanguagePriority);
			if (langIndex >= 0) {
				score += (options.LanguagePriority.Count - langIndex) * 5;
			}
		}

		// Verified/good dump bonus
		if (options.PreferVerified && rom.IsVerified) {
			score += 25;
		}

		// Parent preference (when this is the parent game, not a clone)
		if (options.PreferParent && string.IsNullOrEmpty(rom.Parent)) {
			score += 15;
		}

		// Revision scoring
		if (options.PreferLatestRevision && rom.Revision > 0) {
			score += rom.Revision * 2; // Prefer later revisions
		}

		// Unlicensed penalty
		if (options.ExcludeUnlicensed && rom.Categories.Contains("Unlicensed", StringComparer.OrdinalIgnoreCase)) {
			score -= 50;
		}

		return score;
	}

	private static string GetGroupKey(RomCandidate rom) {
		// If parent is specified, use it for grouping
		if (!string.IsNullOrEmpty(rom.Parent)) {
			return rom.Parent;
		}

		// Otherwise use clean name
		return rom.CleanName;
	}

	private RomGroup CreateRomGroup(IGrouping<string, RomCandidate> group, OneGameOneRomOptions options) {
		var roms = group.ToList();

		// Score all ROMs
		var scored = roms
			.Select(r => (Rom: r, Score: ScoreRom(r, options)))
			.OrderByDescending(x => x.Score)
			.ToList();

		var (Rom, Score) = scored.First();

		// Determine selection reason
		var reason = DetermineSelectionReason(Rom, Score, options);

		return new RomGroup {
			GameName = group.Key,
			Selected = Rom,
			AllRoms = roms,
			SelectedScore = Score,
			SelectionReason = reason
		};
	}

	private static string DetermineSelectionReason(RomCandidate rom, int score, OneGameOneRomOptions options) {
		var reasons = new List<string>();

		if (!string.IsNullOrEmpty(rom.Region)) {
			var regionIndex = GetIndex(rom.Region, options.RegionPriority);
			if (regionIndex == 0) {
				reasons.Add($"Best region ({rom.Region})");
			} else if (regionIndex > 0) {
				reasons.Add($"Region #{regionIndex + 1} ({rom.Region})");
			}
		}

		if (rom.IsVerified) {
			reasons.Add("Verified dump");
		}

		if (rom.Revision > 0) {
			reasons.Add($"Rev {rom.Revision}");
		}

		if (string.IsNullOrEmpty(rom.Parent)) {
			reasons.Add("Parent ROM");
		}

		return reasons.Count > 0 ? string.Join(", ", reasons) : $"Score: {score}";
	}

	private static int GetIndex(string value, IReadOnlyList<string> priorities) {
		for (var i = 0; i < priorities.Count; i++) {
			if (string.Equals(priorities[i], value, StringComparison.OrdinalIgnoreCase)) {
				return i;
			}
		}

		return -1;
	}

	/// <summary>
	/// Creates a RomCandidate from a file path by parsing the name.
	/// </summary>
	public static RomCandidate FromFilePath(string filePath) {
		var fileName = Path.GetFileNameWithoutExtension(filePath);
		var region = TemplateContext.ExtractRegion(fileName);
		var languages = TemplateContext.ExtractLanguages(fileName);
		var cleanName = TemplateContext.ExtractCleanName(fileName);
		var revision = ExtractRevision(fileName);
		var categories = ExtractCategories(fileName);
		var isVerified = fileName.Contains("[!]") || !categories.Any();

		return new RomCandidate {
			FilePath = filePath,
			Name = fileName,
			CleanName = cleanName,
			Region = region,
			Languages = languages,
			Revision = revision,
			Categories = categories,
			IsVerified = isVerified,
			Size = File.Exists(filePath) ? new FileInfo(filePath).Length : 0
		};
	}

	private static int ExtractRevision(string name) {
		// Match patterns like (Rev 1), (Rev A), (v1.1)
		var match = RevisionRegex().Match(name);
		if (!match.Success) {
			return 0;
		}

		// Check which group matched
		var revGroup = match.Groups[1].Value;
		var versionGroup = match.Groups[2].Value;

		if (!string.IsNullOrEmpty(revGroup)) {
			// Try numeric revision
			if (int.TryParse(revGroup, out var numRev)) {
				return numRev;
			}

			// Letter revision (A=1, B=2, etc)
			if (revGroup.Length == 1 && char.IsLetter(revGroup[0])) {
				return char.ToUpperInvariant(revGroup[0]) - 'A' + 1;
			}
		}

		if (!string.IsNullOrEmpty(versionGroup)) {
			// Version string (1.1 = 11, 2.0 = 20)
			if (versionGroup.Contains('.')) {
				var parts = versionGroup.Split('.');
				if (parts.Length >= 2 && int.TryParse(parts[0], out var major) && int.TryParse(parts[1], out var minor)) {
					return major * 10 + minor;
				}
			}
			// Plain number (v2 = 2)
			if (int.TryParse(versionGroup, out var ver)) {
				return ver;
			}
		}

		return 1;
	}

	private static List<string> ExtractCategories(string name) {
		var categories = new List<string>();

		// Check for common categories in parentheses or brackets
		if (DemoRegex().IsMatch(name)) categories.Add("Demo");
		if (BetaRegex().IsMatch(name)) categories.Add("Beta");
		if (ProtoRegex().IsMatch(name)) categories.Add("Proto");
		if (SampleRegex().IsMatch(name)) categories.Add("Sample");
		if (DebugRegex().IsMatch(name)) categories.Add("Debug");
		if (UnlicensedRegex().IsMatch(name)) categories.Add("Unlicensed");
		if (BiosRegex().IsMatch(name)) categories.Add("BIOS");
		if (PirateRegex().IsMatch(name)) categories.Add("Pirate");
		if (BadDumpRegex().IsMatch(name)) categories.Add("BadDump");

		return categories;
	}

	[GeneratedRegex(@"\(Rev\s*([A-Z0-9]+)\)|\(v?(\d+\.?\d*)\)", RegexOptions.IgnoreCase)]
	private static partial Regex RevisionRegex();

	[GeneratedRegex(@"\(Demo\)|\[Demo\]", RegexOptions.IgnoreCase)]
	private static partial Regex DemoRegex();

	[GeneratedRegex(@"\(Beta\)|\[Beta\]", RegexOptions.IgnoreCase)]
	private static partial Regex BetaRegex();

	[GeneratedRegex(@"\(Proto\)|\[Proto\]|\(Prototype\)", RegexOptions.IgnoreCase)]
	private static partial Regex ProtoRegex();

	[GeneratedRegex(@"\(Sample\)|\[Sample\]", RegexOptions.IgnoreCase)]
	private static partial Regex SampleRegex();

	[GeneratedRegex(@"\(Debug\)|\[Debug\]", RegexOptions.IgnoreCase)]
	private static partial Regex DebugRegex();

	[GeneratedRegex(@"\(Unl\)|\(Unlicensed\)|\[Unl\]", RegexOptions.IgnoreCase)]
	private static partial Regex UnlicensedRegex();

	[GeneratedRegex(@"\[BIOS\]|\(BIOS\)", RegexOptions.IgnoreCase)]
	private static partial Regex BiosRegex();

	[GeneratedRegex(@"\(Pirate\)|\[Pirate\]|\[p\]", RegexOptions.IgnoreCase)]
	private static partial Regex PirateRegex();

	[GeneratedRegex(@"\[b\]|\[bad\]|\(Bad Dump\)", RegexOptions.IgnoreCase)]
	private static partial Regex BadDumpRegex();
}
