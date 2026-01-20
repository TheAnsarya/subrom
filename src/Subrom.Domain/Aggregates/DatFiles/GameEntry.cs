using Subrom.Domain.Common;

namespace Subrom.Domain.Aggregates.DatFiles;

/// <summary>
/// Represents a game/machine entry in a DAT file.
/// Contains one or more ROMs that make up the game.
/// </summary>
public class GameEntry : Entity<int> {
	private readonly List<RomEntry> _roms = [];

	/// <summary>
	/// Game name as specified in the DAT (usually the folder/archive name).
	/// </summary>
	public required string Name { get; init; }

	/// <summary>
	/// Human-readable description/title.
	/// </summary>
	public string? Description { get; init; }

	/// <summary>
	/// Region code (USA, Europe, Japan, etc.).
	/// </summary>
	public string? Region { get; init; }

	/// <summary>
	/// Language codes.
	/// </summary>
	public string? Languages { get; init; }

	/// <summary>
	/// Release year.
	/// </summary>
	public string? Year { get; init; }

	/// <summary>
	/// Publisher/developer.
	/// </summary>
	public string? Publisher { get; init; }

	/// <summary>
	/// Parent game name for clones.
	/// </summary>
	public string? CloneOf { get; init; }

	/// <summary>
	/// ROM parent for merging.
	/// </summary>
	public string? RomOf { get; init; }

	/// <summary>
	/// Sample parent for audio samples (MAME).
	/// </summary>
	public string? SampleOf { get; init; }

	/// <summary>
	/// Whether this is a BIOS set.
	/// </summary>
	public bool IsBios { get; init; }

	/// <summary>
	/// Whether this is a device (MAME).
	/// </summary>
	public bool IsDevice { get; init; }

	/// <summary>
	/// Whether this is a mechanical game (MAME).
	/// </summary>
	public bool IsMechanical { get; init; }

	/// <summary>
	/// Category for organization.
	/// </summary>
	public string? Category { get; init; }

	/// <summary>
	/// The ROMs that belong to this game.
	/// </summary>
	public IReadOnlyList<RomEntry> Roms => _roms.AsReadOnly();

	/// <summary>
	/// Parent DAT file ID.
	/// </summary>
	public Guid DatFileId { get; init; }

	/// <summary>
	/// Adds a ROM to this game.
	/// </summary>
	internal void AddRom(RomEntry rom) {
		_roms.Add(rom);
	}

	/// <summary>
	/// Total size of all ROMs in this game.
	/// </summary>
	public long TotalSize => _roms.Sum(r => r.Size);

	/// <summary>
	/// Checks if this is a clone of another game.
	/// </summary>
	public bool IsClone => !string.IsNullOrEmpty(CloneOf);
}
