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

	public async Task<RomHashes> ComputeHashesAsync(
		string filePath,
		IProgress<HashProgress>? progress = null,
		CancellationToken cancellationToken = default) {
		ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

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

		return await ComputeHashesInternalAsync(stream, fileInfo.Length, progress, cancellationToken);
	}

	public async Task<RomHashes> ComputeHashesAsync(
		Stream stream,
		IProgress<HashProgress>? progress = null,
		CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(stream);

		var length = stream.CanSeek ? stream.Length : -1;
		return await ComputeHashesInternalAsync(stream, length, progress, cancellationToken);
	}

	public async Task<RomHashes> ComputeArchiveEntryHashesAsync(
		string archivePath,
		string entryPath,
		IProgress<HashProgress>? progress = null,
		CancellationToken cancellationToken = default) {
		ArgumentException.ThrowIfNullOrWhiteSpace(archivePath);
		ArgumentException.ThrowIfNullOrWhiteSpace(entryPath);

		// Support .zip archives using built-in ZipFile
		var extension = Path.GetExtension(archivePath).ToLowerInvariant();
		if (extension == ".zip") {
			using var archive = ZipFile.OpenRead(archivePath);
			var entry = archive.GetEntry(entryPath)
				?? throw new FileNotFoundException($"Entry not found in archive: {entryPath}", entryPath);

			await using var stream = entry.Open();

			// ZipArchiveEntry streams may not be seekable, pass length from entry
			return await ComputeHashesInternalAsync(stream, entry.Length, progress, cancellationToken);
		}

		// TODO: Add support for 7z and other formats using SharpCompress or native 7z
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
