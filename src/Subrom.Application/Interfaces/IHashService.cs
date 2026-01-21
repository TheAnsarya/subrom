using Subrom.Domain.ValueObjects;

namespace Subrom.Application.Interfaces;

/// <summary>
/// Service interface for computing file hashes.
/// </summary>
public interface IHashService {
	/// <summary>
	/// Computes all hashes (CRC32, MD5, SHA-1) for a file.
	/// </summary>
	/// <param name="filePath">Absolute path to the file.</param>
	/// <param name="progress">Optional progress callback for large files.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Computed hashes.</returns>
	Task<RomHashes> ComputeHashesAsync(
		string filePath,
		IProgress<HashProgress>? progress = null,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Computes all hashes from a stream.
	/// </summary>
	/// <param name="stream">Stream to hash.</param>
	/// <param name="progress">Optional progress callback.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Computed hashes.</returns>
	Task<RomHashes> ComputeHashesAsync(
		Stream stream,
		IProgress<HashProgress>? progress = null,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Computes hashes for a file, optionally skipping a header.
	/// </summary>
	/// <param name="filePath">Absolute path to the file.</param>
	/// <param name="skipBytes">Number of bytes to skip at start (header size).</param>
	/// <param name="progress">Optional progress callback for large files.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Computed hashes.</returns>
	Task<RomHashes> ComputeHashesAsync(
		string filePath,
		int skipBytes,
		IProgress<HashProgress>? progress = null,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Computes hashes from a stream, optionally skipping a header.
	/// </summary>
	/// <param name="stream">Stream to hash (must be seekable if skipBytes > 0).</param>
	/// <param name="skipBytes">Number of bytes to skip at start (header size).</param>
	/// <param name="progress">Optional progress callback.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Computed hashes.</returns>
	Task<RomHashes> ComputeHashesAsync(
		Stream stream,
		int skipBytes,
		IProgress<HashProgress>? progress = null,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Computes hashes for a file inside an archive.
	/// </summary>
	/// <param name="archivePath">Path to the archive.</param>
	/// <param name="entryPath">Path within the archive.</param>
	/// <param name="progress">Optional progress callback.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Computed hashes.</returns>
	Task<RomHashes> ComputeArchiveEntryHashesAsync(
		string archivePath,
		string entryPath,
		IProgress<HashProgress>? progress = null,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Computes hashes for a file inside an archive, optionally skipping a header.
	/// </summary>
	/// <param name="archivePath">Path to the archive.</param>
	/// <param name="entryPath">Path within the archive.</param>
	/// <param name="skipBytes">Number of bytes to skip at start (header size).</param>
	/// <param name="progress">Optional progress callback.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Computed hashes.</returns>
	Task<RomHashes> ComputeArchiveEntryHashesAsync(
		string archivePath,
		string entryPath,
		int skipBytes,
		IProgress<HashProgress>? progress = null,
		CancellationToken cancellationToken = default);
}

/// <summary>
/// Progress information for hash computation.
/// </summary>
public record HashProgress {
	/// <summary>
	/// File being hashed.
	/// </summary>
	public required string FileName { get; init; }

	/// <summary>
	/// Bytes processed so far.
	/// </summary>
	public required long ProcessedBytes { get; init; }

	/// <summary>
	/// Total file size.
	/// </summary>
	public required long TotalBytes { get; init; }

	/// <summary>
	/// Progress percentage (0-100).
	/// </summary>
	public double Percentage => TotalBytes > 0 ? (double)ProcessedBytes / TotalBytes * 100 : 0;
}
