using Subrom.Domain.Aggregates.Organization;

namespace Subrom.Application.Interfaces;

/// <summary>
/// Service for 1G1R (One Game One ROM) filtering.
/// Selects the best ROM for each game based on region and language priorities.
/// </summary>
public interface IOneGameOneRomService {
	/// <summary>
	/// Filters a collection of ROMs to select one ROM per game based on priorities.
	/// </summary>
	/// <param name="roms">All ROMs to filter.</param>
	/// <param name="options">1G1R options including region/language priorities.</param>
	/// <returns>The filtered ROM collection with one ROM per game.</returns>
	IReadOnlyList<RomCandidate> Filter(IEnumerable<RomCandidate> roms, OneGameOneRomOptions options);

	/// <summary>
	/// Groups ROMs by their parent/clone relationship and selects best from each group.
	/// </summary>
	/// <param name="roms">All ROMs to process.</param>
	/// <param name="options">1G1R options.</param>
	/// <returns>Groups of ROMs with the selected best ROM for each game.</returns>
	IReadOnlyList<RomGroup> GroupAndSelect(IEnumerable<RomCandidate> roms, OneGameOneRomOptions options);

	/// <summary>
	/// Scores a ROM based on the given priorities.
	/// Higher scores are better.
	/// </summary>
	/// <param name="rom">The ROM to score.</param>
	/// <param name="options">The scoring options.</param>
	/// <returns>The ROM's score.</returns>
	int ScoreRom(RomCandidate rom, OneGameOneRomOptions options);
}

/// <summary>
/// Options for 1G1R filtering.
/// </summary>
public record OneGameOneRomOptions {
	/// <summary>
	/// Region priority order. First region is highest priority.
	/// </summary>
	public IReadOnlyList<string> RegionPriority { get; init; } =
		["USA", "World", "Europe", "Japan", "Germany", "France", "Spain", "Italy"];

	/// <summary>
	/// Language priority order. First language is highest priority.
	/// </summary>
	public IReadOnlyList<string> LanguagePriority { get; init; } =
		["En", "En,Fr", "En,De", "En,Es", "Ja", "Fr", "De", "Es", "It"];

	/// <summary>
	/// Categories to exclude from selection.
	/// </summary>
	public IReadOnlyList<string> ExcludeCategories { get; init; } =
		["BIOS", "Demo", "Beta", "Proto", "Sample", "Debug", "SDK"];

	/// <summary>
	/// Whether to prefer parent ROMs over clones when scores are equal.
	/// </summary>
	public bool PreferParent { get; init; } = true;

	/// <summary>
	/// Whether to prefer verified/good dumps.
	/// </summary>
	public bool PreferVerified { get; init; } = true;

	/// <summary>
	/// Whether to exclude unlicensed ROMs.
	/// </summary>
	public bool ExcludeUnlicensed { get; init; } = false;

	/// <summary>
	/// Whether to include revision updates (prefer latest revision).
	/// </summary>
	public bool PreferLatestRevision { get; init; } = true;

	/// <summary>
	/// Creates options from an OrganizationTemplate.
	/// </summary>
	public static OneGameOneRomOptions FromTemplate(OrganizationTemplate template) => new() {
		RegionPriority = template.RegionPriority,
		LanguagePriority = template.LanguagePriority,
		ExcludeCategories = template.ExcludeCategories
	};
}

/// <summary>
/// Represents a ROM candidate for 1G1R selection.
/// </summary>
public record RomCandidate {
	/// <summary>ROM file path.</summary>
	public required string FilePath { get; init; }

	/// <summary>ROM name without extension.</summary>
	public required string Name { get; init; }

	/// <summary>Clean name without flags (for grouping).</summary>
	public required string CleanName { get; init; }

	/// <summary>Region (USA, Europe, Japan, etc).</summary>
	public string? Region { get; init; }

	/// <summary>Languages (En, En,Fr, Ja, etc).</summary>
	public string? Languages { get; init; }

	/// <summary>Parent game name if this is a clone.</summary>
	public string? Parent { get; init; }

	/// <summary>Whether this ROM is verified/good.</summary>
	public bool IsVerified { get; init; }

	/// <summary>Revision number (0 = original, 1+ = revisions).</summary>
	public int Revision { get; init; }

	/// <summary>Categories/flags (Demo, Beta, Proto, etc).</summary>
	public IReadOnlyList<string> Categories { get; init; } = [];

	/// <summary>File size in bytes.</summary>
	public long Size { get; init; }

	/// <summary>CRC32 hash.</summary>
	public string? Crc { get; init; }

	/// <summary>Additional metadata.</summary>
	public IDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}

/// <summary>
/// A group of ROMs representing the same game.
/// </summary>
public record RomGroup {
	/// <summary>The clean game name (group key).</summary>
	public required string GameName { get; init; }

	/// <summary>The selected best ROM for this game.</summary>
	public required RomCandidate Selected { get; init; }

	/// <summary>All ROMs in this group (including rejected).</summary>
	public required IReadOnlyList<RomCandidate> AllRoms { get; init; }

	/// <summary>Score of the selected ROM.</summary>
	public int SelectedScore { get; init; }

	/// <summary>Reason the selected ROM was chosen.</summary>
	public string? SelectionReason { get; init; }
}
