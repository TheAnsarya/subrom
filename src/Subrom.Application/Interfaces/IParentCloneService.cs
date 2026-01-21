using Subrom.Application.Interfaces;
using Subrom.Domain.Aggregates.DatFiles;

namespace Subrom.Application.Interfaces;

/// <summary>
/// Service for managing parent/clone relationships in ROM collections.
/// </summary>
public interface IParentCloneService {
	/// <summary>
	/// Gets the parent game for a ROM if it's a clone.
	/// </summary>
	/// <param name="romName">The ROM name to check.</param>
	/// <param name="datFileId">Optional DAT file to use for lookup.</param>
	/// <returns>Parent game name or null if this is a parent.</returns>
	Task<string?> GetParentAsync(string romName, Guid? datFileId = null, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets all clones for a parent game.
	/// </summary>
	/// <param name="parentName">The parent game name.</param>
	/// <param name="datFileId">Optional DAT file for lookup.</param>
	/// <returns>List of clone names.</returns>
	Task<IReadOnlyList<string>> GetClonesAsync(string parentName, Guid? datFileId = null, CancellationToken cancellationToken = default);

	/// <summary>
	/// Groups ROMs by their parent/clone relationships.
	/// </summary>
	/// <param name="romNames">ROM names to group.</param>
	/// <param name="datFileId">Optional DAT file for lookup.</param>
	/// <returns>Parent/clone groupings.</returns>
	Task<IReadOnlyList<ParentCloneGroup>> GroupByParentAsync(IEnumerable<string> romNames, Guid? datFileId = null, CancellationToken cancellationToken = default);

	/// <summary>
	/// Builds parent/clone relationships from a DAT file.
	/// </summary>
	Task<ParentCloneIndex> BuildIndexFromDatAsync(Guid datFileId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Infers parent/clone relationships from ROM names (for when DAT info isn't available).
	/// </summary>
	IReadOnlyList<ParentCloneGroup> InferRelationships(IEnumerable<string> romNames);
}

/// <summary>
/// A group of parent and clone ROMs.
/// </summary>
public record ParentCloneGroup {
	/// <summary>Parent ROM name.</summary>
	public required string Parent { get; init; }

	/// <summary>List of clone ROM names.</summary>
	public IReadOnlyList<string> Clones { get; init; } = [];

	/// <summary>Total ROMs in this group (parent + clones).</summary>
	public int TotalCount => 1 + Clones.Count;

	/// <summary>Whether this group has any clones.</summary>
	public bool HasClones => Clones.Count > 0;
}

/// <summary>
/// Index of parent/clone relationships for fast lookup.
/// </summary>
public class ParentCloneIndex {
	private readonly Dictionary<string, string> _cloneToParent = new(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, List<string>> _parentToClones = new(StringComparer.OrdinalIgnoreCase);

	/// <summary>DAT file ID this index was built from.</summary>
	public Guid? DatFileId { get; init; }

	/// <summary>When the index was built.</summary>
	public DateTime BuiltAt { get; init; } = DateTime.UtcNow;

	/// <summary>Number of parent games.</summary>
	public int ParentCount => _parentToClones.Count;

	/// <summary>Number of clone games.</summary>
	public int CloneCount => _cloneToParent.Count;

	/// <summary>
	/// Adds a parent/clone relationship.
	/// </summary>
	public void AddRelationship(string parent, string clone) {
		_cloneToParent[clone] = parent;

		if (!_parentToClones.TryGetValue(parent, out var clones)) {
			clones = [];
			_parentToClones[parent] = clones;
		}
		clones.Add(clone);
	}

	/// <summary>
	/// Gets the parent for a clone, or null if it's a parent.
	/// </summary>
	public string? GetParent(string romName) {
		return _cloneToParent.TryGetValue(romName, out var parent) ? parent : null;
	}

	/// <summary>
	/// Gets clones for a parent.
	/// </summary>
	public IReadOnlyList<string> GetClones(string parentName) {
		return _parentToClones.TryGetValue(parentName, out var clones) ? clones : [];
	}

	/// <summary>
	/// Checks if a ROM is a clone.
	/// </summary>
	public bool IsClone(string romName) => _cloneToParent.ContainsKey(romName);

	/// <summary>
	/// Checks if a ROM is a parent (has clones).
	/// </summary>
	public bool IsParent(string romName) => _parentToClones.ContainsKey(romName);

	/// <summary>
	/// Gets all groups.
	/// </summary>
	public IReadOnlyList<ParentCloneGroup> GetAllGroups() {
		return _parentToClones.Select(kvp => new ParentCloneGroup {
			Parent = kvp.Key,
			Clones = kvp.Value
		}).ToList();
	}
}
