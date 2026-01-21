using System.Buffers;
using System.IO.Compression;
using System.IO.Hashing;
using System.Security.Cryptography;
using Subrom.Application.Interfaces;
using Subrom.Domain.ValueObjects;

namespace Subrom.Infrastructure.Services;

/// <summary>
/// Service for computing file hashes (CRC32, MD5, SHA-1) in parallel.
/// Uses modern .NET APIs with optimized memory usage via ArrayPool.
/// </summary>
public sealed class HashService : IHashService {
	private const int ChunkSize = 64 * 1024; // 64KB chunks for better throughput
	private readonly IArchiveService _archiveService;

	public HashService(IArchiveService archiveService) {
		_archiveService = archiveService;
	}

	public Task<RomHashes> ComputeHashesAsync(
		string filePath,
		IProgress<HashProgress>? progress = null,
		CancellationToken cancellationToken = default) {
		return ComputeHashesAsync(filePath, skipBytes: 0, progress, cancellationToken);
	}

	public async Task<RomHashes> ComputeHashesAsync(
		string filePath,
		int skipBytes,
		IProgress<HashProgress>? progress = null,
		CancellationToken cancellationToken = default) {
		ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
		ArgumentOutOfRangeException.ThrowIfNegative(skipBytes);

		var fileInfo = new FileInfo(filePath);
		if (!fileInfo.Exists) {
			throw new FileNotFoundException("File not found", filePath);
		}

		await using var stream = new FileStream(
			filePath,
			FileMode.Open,
			FileAccess.Read,
			FileShare.Read,
			bufferSize: ChunkSize,
			FileOptions.Asynchronous | FileOptions.SequentialScan);

		// Skip header bytes if specified
		if (skipBytes > 0) {
			if (stream.Length <= skipBytes) {
				throw new ArgumentException($"File is smaller than header size ({skipBytes} bytes)", nameof(skipBytes));
			}
			stream.Position = skipBytes;
		}

		var effectiveLength = fileInfo.Length - skipBytes;
		return await ComputeHashesInternalAsync(stream, effectiveLength, progress, cancellationToken);
	}

	public Task<RomHashes> ComputeHashesAsync(
		Stream stream,
		IProgress<HashProgress>? progress = null,
		CancellationToken cancellationToken = default) {
		return ComputeHashesAsync(stream, skipBytes: 0, progress, cancellationToken);
	}

	public async Task<RomHashes> ComputeHashesAsync(
		Stream stream,
		int skipBytes,
		IProgress<HashProgress>? progress = null,
		CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(stream);
		ArgumentOutOfRangeException.ThrowIfNegative(skipBytes);

		// Skip header bytes if specified
		if (skipBytes > 0) {
			if (!stream.CanSeek) {
				// For non-seekable streams, read and discard header bytes
				var skipBuffer = new byte[skipBytes];
				var totalRead = 0;
				while (totalRead < skipBytes) {
					var read = await stream.ReadAsync(skipBuffer.AsMemory(totalRead, skipBytes - totalRead), cancellationToken);
					if (read == 0) throw new EndOfStreamException("Stream ended before header could be skipped");
					totalRead += read;
				}
			} else {
				stream.Position = skipBytes;
			}
		}

		var length = stream.CanSeek ? stream.Length - skipBytes : -1;
		return await ComputeHashesInternalAsync(stream, length, progress, cancellationToken);
	}

	public Task<RomHashes> ComputeArchiveEntryHashesAsync(
		string archivePath,
		string entryPath,
		IProgress<HashProgress>? progress = null,
		CancellationToken cancellationToken = default) {
		return ComputeArchiveEntryHashesAsync(archivePath, entryPath, skipBytes: 0, progress, cancellationToken);
	}

	public async Task<RomHashes> ComputeArchiveEntryHashesAsync(
		string archivePath,
		string entryPath,
		int skipBytes,
		IProgress<HashProgress>? progress = null,
		CancellationToken cancellationToken = default) {
		ArgumentException.ThrowIfNullOrWhiteSpace(archivePath);
		ArgumentException.ThrowIfNullOrWhiteSpace(entryPath);
		ArgumentOutOfRangeException.ThrowIfNegative(skipBytes);

		var extension = Path.GetExtension(archivePath).ToLowerInvariant();

		// Use built-in ZipFile for .zip (slightly more efficient than SharpCompress)
		if (extension == ".zip") {
			using var archive = ZipFile.OpenRead(archivePath);
			var entry = archive.GetEntry(entryPath)
				?? throw new FileNotFoundException($"Entry not found in archive: {entryPath}", entryPath);

			await using var stream = entry.Open();

			// Skip header bytes for non-seekable zip streams
			if (skipBytes > 0) {
				var skipBuffer = new byte[skipBytes];
				var totalRead = 0;
				while (totalRead < skipBytes) {
					var read = await stream.ReadAsync(skipBuffer.AsMemory(totalRead, skipBytes - totalRead), cancellationToken);
					if (read == 0) throw new EndOfStreamException("Stream ended before header could be skipped");
					totalRead += read;
				}
			}

			return await ComputeHashesInternalAsync(stream, entry.Length - skipBytes, progress, cancellationToken);
		}

		// Use SharpCompress for all other supported formats (7z, RAR, TAR, etc.)
		if (_archiveService.SupportsFormat(extension)) {
			await using var stream = await _archiveService.OpenEntryAsync(archivePath, entryPath, cancellationToken);
			var entries = await _archiveService.ListEntriesAsync(archivePath, cancellationToken);
			var entry = entries.FirstOrDefault(e =>
				string.Equals(e.Path, entryPath, StringComparison.OrdinalIgnoreCase));
			var length = (entry?.UncompressedSize ?? 0) - skipBytes;

			// Skip header bytes
			if (skipBytes > 0) {
				var skipBuffer = new byte[skipBytes];
				var totalRead = 0;
				while (totalRead < skipBytes) {
					var read = await stream.ReadAsync(skipBuffer.AsMemory(totalRead, skipBytes - totalRead), cancellationToken);
					if (read == 0) throw new EndOfStreamException("Stream ended before header could be skipped");
					totalRead += read;
				}
			}

			return await ComputeHashesInternalAsync(stream, length, progress, cancellationToken);
		}

		throw new NotSupportedException($"Archive format not supported: {extension}");
	}

	private async Task<RomHashes> ComputeHashesInternalAsync(
		Stream stream,
		long totalBytes,
		IProgress<HashProgress>? progress,
		CancellationToken cancellationToken) {
		// Use modern incremental hash APIs
		var crc32 = new Crc32();
		using var md5 = IncrementalHash.CreateHash(HashAlgorithmName.MD5);
		using var sha1 = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);

		var buffer = ArrayPool<byte>.Shared.Rent(ChunkSize);
		long bytesProcessed = 0;

		try {
			int bytesRead;
			while ((bytesRead = await stream.ReadAsync(buffer.AsMemory(0, ChunkSize), cancellationToken)) > 0) {
				var span = buffer.AsSpan(0, bytesRead);
				crc32.Append(span);
				md5.AppendData(span);
				sha1.AppendData(span);

				bytesProcessed += bytesRead;

				if (progress is not null && totalBytes > 0) {
					progress.Report(new HashProgress {
						FileName = "",
						ProcessedBytes = bytesProcessed,
						TotalBytes = totalBytes
					});
				}
			}

			var crcValue = Crc.Create(Convert.ToHexStringLower(crc32.GetCurrentHash()));
			var md5Value = Md5.Create(Convert.ToHexStringLower(md5.GetCurrentHash()));
			var sha1Value = Sha1.Create(Convert.ToHexStringLower(sha1.GetCurrentHash()));

			return new RomHashes(crcValue, md5Value, sha1Value);
		}
		finally {
			ArrayPool<byte>.Shared.Return(buffer);
		}
	}
}
