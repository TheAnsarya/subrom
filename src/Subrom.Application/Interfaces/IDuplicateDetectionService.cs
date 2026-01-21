using Subrom.Domain.ValueObjects;

namespace Subrom.Application.Interfaces;

/// <summary>
/// Service for detecting duplicate ROMs based on hash values.
/// </summary>
public interface IDuplicateDetectionService {
	/// <summary>
	/// Finds all duplicate groups in a collection of ROM entries.
	/// </summary>
	/// <param name="entries">ROM entries to check for duplicates</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Groups of duplicate ROMs</returns>
	Task<IReadOnlyList<DuplicateGroup>> FindDuplicatesAsync(
		IEnumerable<ScannedRomEntry> entries,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Finds duplicates of a specific ROM in a collection.
	/// </summary>
	/// <param name="targetHashes">Hashes to search for</param>
	/// <param name="entries">ROM entries to search within</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>All entries matching the target hashes</returns>
	Task<IReadOnlyList<ScannedRomEntry>> FindDuplicatesOfAsync(
		RomHashes targetHashes,
		IEnumerable<ScannedRomEntry> entries,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Groups ROM entries by their hash values.
	/// </summary>
	/// <param name="entries">ROM entries to group</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Dictionary mapping hashes to list of matching entries</returns>
	Task<IReadOnlyDictionary<RomHashes, IReadOnlyList<ScannedRomEntry>>> GroupByHashesAsync(
		IEnumerable<ScannedRomEntry> entries,
		CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a scanned ROM file entry with location and hash information.
/// Named to distinguish from Domain's RomEntry aggregate.
/// </summary>
/// <param name="Path">Full path to the ROM file (or archive path for archived ROMs)</param>
/// <param name="EntryPath">Path within archive if ROM is inside an archive, otherwise null</param>
/// <param name="Hashes">Computed hash values for the ROM</param>
/// <param name="Size">File size in bytes</param>
/// <param name="FileName">Original file name</param>
public sealed record ScannedRomEntry(
	string Path,
	string? EntryPath,
	RomHashes Hashes,
	long Size,
	string FileName) {

	/// <summary>
	/// Whether this ROM is inside an archive.
	/// </summary>
	public bool IsArchived => EntryPath is not null;

	/// <summary>
	/// Gets a display-friendly location string.
	/// </summary>
	public string DisplayLocation => IsArchived
		? $"{System.IO.Path.GetFileName(Path)}:{EntryPath}"
		: System.IO.Path.GetFileName(Path);
}

/// <summary>
/// Represents a group of duplicate ROM files.
/// </summary>
/// <param name="Hashes">Common hash values shared by all duplicates</param>
/// <param name="Entries">All ROM entries that share these hashes</param>
/// <param name="TotalSize">Combined size of all duplicates</param>
public sealed record DuplicateGroup(
	RomHashes Hashes,
	IReadOnlyList<ScannedRomEntry> Entries,
	long TotalSize) {

	/// <summary>
	/// Number of duplicates (including original).
	/// </summary>
	public int Count => Entries.Count;

	/// <summary>
	/// Size that could be saved by removing duplicates (keeping one).
	/// </summary>
	public long WastedSpace => TotalSize - (Entries.Count > 0 ? Entries[0].Size : 0);
}
