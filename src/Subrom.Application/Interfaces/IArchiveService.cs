namespace Subrom.Application.Interfaces;

/// <summary>
/// Service interface for reading and extracting archive files.
/// Supports ZIP, 7z, RAR, and other common ROM archive formats.
/// </summary>
public interface IArchiveService {
	/// <summary>
	/// Lists all entries in an archive without extracting.
	/// </summary>
	/// <param name="archivePath">Absolute path to the archive file.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>List of archive entries with metadata.</returns>
	Task<IReadOnlyList<ArchiveEntry>> ListEntriesAsync(
		string archivePath,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Opens a stream to read a specific entry from an archive.
	/// Caller is responsible for disposing the returned stream.
	/// </summary>
	/// <param name="archivePath">Absolute path to the archive file.</param>
	/// <param name="entryPath">Path of the entry within the archive.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Stream to read the entry content.</returns>
	Task<Stream> OpenEntryAsync(
		string archivePath,
		string entryPath,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Extracts a single entry from an archive to a destination path.
	/// </summary>
	/// <param name="archivePath">Absolute path to the archive file.</param>
	/// <param name="entryPath">Path of the entry within the archive.</param>
	/// <param name="destinationPath">Full path where the entry should be extracted.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	Task ExtractEntryAsync(
		string archivePath,
		string entryPath,
		string destinationPath,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Extracts all entries from an archive to a destination directory.
	/// </summary>
	/// <param name="archivePath">Absolute path to the archive file.</param>
	/// <param name="destinationDirectory">Directory where entries should be extracted.</param>
	/// <param name="progress">Optional progress callback.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	Task ExtractAllAsync(
		string archivePath,
		string destinationDirectory,
		IProgress<ExtractionProgress>? progress = null,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Checks if this service supports the given archive format.
	/// </summary>
	/// <param name="extension">File extension including the dot (e.g., ".zip", ".7z").</param>
	/// <returns>True if the format is supported.</returns>
	bool SupportsFormat(string extension);

	/// <summary>
	/// Gets all supported archive extensions.
	/// </summary>
	IReadOnlySet<string> SupportedExtensions { get; }
}

/// <summary>
/// Metadata for an entry within an archive.
/// </summary>
public sealed record ArchiveEntry {
	/// <summary>
	/// Full path of the entry within the archive.
	/// </summary>
	public required string Path { get; init; }

	/// <summary>
	/// True if this entry is a directory.
	/// </summary>
	public required bool IsDirectory { get; init; }

	/// <summary>
	/// Uncompressed size in bytes, or null if unknown.
	/// </summary>
	public long? UncompressedSize { get; init; }

	/// <summary>
	/// Compressed size in bytes, or null if unknown.
	/// </summary>
	public long? CompressedSize { get; init; }

	/// <summary>
	/// Last modification time, or null if unknown.
	/// </summary>
	public DateTimeOffset? LastModified { get; init; }

	/// <summary>
	/// CRC32 checksum stored in the archive, or null if not available.
	/// </summary>
	public uint? Crc32 { get; init; }
}

/// <summary>
/// Progress information for archive extraction operations.
/// </summary>
public sealed record ExtractionProgress {
	/// <summary>
	/// Current entry being extracted.
	/// </summary>
	public required string CurrentEntry { get; init; }

	/// <summary>
	/// Number of entries processed so far.
	/// </summary>
	public required int ProcessedEntries { get; init; }

	/// <summary>
	/// Total number of entries in the archive.
	/// </summary>
	public required int TotalEntries { get; init; }

	/// <summary>
	/// Bytes extracted so far across all entries.
	/// </summary>
	public required long ProcessedBytes { get; init; }

	/// <summary>
	/// Total uncompressed size of all entries.
	/// </summary>
	public required long TotalBytes { get; init; }

	/// <summary>
	/// Progress percentage (0-100) based on entry count.
	/// </summary>
	public double EntryPercentage => TotalEntries > 0 ? (double)ProcessedEntries / TotalEntries * 100 : 0;

	/// <summary>
	/// Progress percentage (0-100) based on bytes.
	/// </summary>
	public double BytePercentage => TotalBytes > 0 ? (double)ProcessedBytes / TotalBytes * 100 : 0;
}
