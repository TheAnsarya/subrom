using Subrom.Domain.Common;
using Subrom.Domain.ValueObjects;

namespace Subrom.Domain.Aggregates.DatFiles;

/// <summary>
/// Represents a ROM entry within a game in a DAT file.
/// </summary>
public class RomEntry : Entity<int> {
	/// <summary>
	/// ROM filename as specified in the DAT.
	/// </summary>
	public required string Name { get; init; }

	/// <summary>
	/// ROM file size in bytes.
	/// </summary>
	public required long Size { get; init; }

	/// <summary>
	/// Hash values for verification.
	/// </summary>
	public required RomHashes Hashes { get; init; }

	/// <summary>
	/// ROM status (good, baddump, nodump, verified).
	/// </summary>
	public RomStatus Status { get; init; } = RomStatus.Good;

	/// <summary>
	/// Serial number if available (for disc-based ROMs).
	/// </summary>
	public string? Serial { get; init; }

	/// <summary>
	/// Whether this is a BIOS/firmware file.
	/// </summary>
	public bool IsBios { get; init; }

	/// <summary>
	/// Merge name if this ROM can be merged with parent.
	/// </summary>
	public string? Merge { get; init; }

	/// <summary>
	/// Parent game ID for merged/clone sets.
	/// </summary>
	public int? GameId { get; init; }
}

/// <summary>
/// ROM verification status.
/// </summary>
public enum RomStatus {
	Good,
	BadDump,
	NoDump,
	Verified
}
