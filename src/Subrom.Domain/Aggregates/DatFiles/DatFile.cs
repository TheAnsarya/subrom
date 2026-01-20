using Subrom.Domain.Common;

namespace Subrom.Domain.Aggregates.DatFiles;

/// <summary>
/// Aggregate root for DAT files.
/// A DAT file is a catalog of ROMs for a specific system/platform.
/// </summary>
public class DatFile : AggregateRoot {
	private readonly List<GameEntry> _games = [];

	/// <summary>
	/// Original filename of the DAT file.
	/// </summary>
	public required string FileName { get; init; }

	/// <summary>
	/// Name/title of the DAT (from header).
	/// </summary>
	public required string Name { get; set; }

	/// <summary>
	/// Description of the DAT contents.
	/// </summary>
	public string? Description { get; set; }

	/// <summary>
	/// Version string.
	/// </summary>
	public string? Version { get; set; }

	/// <summary>
	/// Author of the DAT file.
	/// </summary>
	public string? Author { get; set; }

	/// <summary>
	/// Homepage URL.
	/// </summary>
	public string? Homepage { get; set; }

	/// <summary>
	/// Date the DAT was created/updated.
	/// </summary>
	public DateTime? DatDate { get; set; }

	/// <summary>
	/// DAT file format.
	/// </summary>
	public DatFormat Format { get; init; } = DatFormat.LogiqxXml;

	/// <summary>
	/// Provider/source of the DAT.
	/// </summary>
	public DatProvider Provider { get; set; } = DatProvider.Unknown;

	/// <summary>
	/// System/platform this DAT covers.
	/// </summary>
	public string? System { get; set; }

	/// <summary>
	/// Category path for hierarchical organization.
	/// Example: "TOSEC/Games/Amstrad CPC"
	/// </summary>
	public string? CategoryPath { get; set; }

	/// <summary>
	/// When this DAT was imported.
	/// </summary>
	public DateTime ImportedAt { get; init; } = DateTime.UtcNow;

	/// <summary>
	/// When this DAT was last updated.
	/// </summary>
	public DateTime? UpdatedAt { get; set; }

	/// <summary>
	/// File size of the original DAT file.
	/// </summary>
	public long FileSize { get; init; }

	/// <summary>
	/// Whether this DAT is enabled for verification.
	/// </summary>
	public bool IsEnabled { get; set; } = true;

	/// <summary>
	/// Games in this DAT file.
	/// </summary>
	public IReadOnlyList<GameEntry> Games => _games.AsReadOnly();

	/// <summary>
	/// Number of games in this DAT.
	/// </summary>
	public int GameCount => _games.Count;

	/// <summary>
	/// Total number of ROMs across all games.
	/// </summary>
	public int RomCount => _games.Sum(g => g.Roms.Count);

	/// <summary>
	/// Total size of all ROMs in this DAT.
	/// </summary>
	public long TotalSize => _games.Sum(g => g.TotalSize);

	/// <summary>
	/// Creates a new DAT file.
	/// </summary>
	public static DatFile Create(string fileName, string name, DatFormat format = DatFormat.LogiqxXml) {
		var datFile = new DatFile {
			FileName = fileName,
			Name = name,
			Format = format
		};

		datFile.AddDomainEvent(new DatFileImportedEvent(datFile.Id, fileName, name));
		return datFile;
	}

	/// <summary>
	/// Adds a game to this DAT file.
	/// </summary>
	public void AddGame(GameEntry game) {
		_games.Add(game);
	}

	/// <summary>
	/// Adds multiple games to this DAT file.
	/// </summary>
	public void AddGames(IEnumerable<GameEntry> games) {
		_games.AddRange(games);
	}

	/// <summary>
	/// Clears all games (for reimport).
	/// </summary>
	public void ClearGames() {
		_games.Clear();
	}
}

/// <summary>
/// Domain event raised when a DAT file is imported.
/// </summary>
public sealed record DatFileImportedEvent(Guid DatFileId, string FileName, string Name) : DomainEvent;
