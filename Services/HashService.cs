using System.Buffers;
using System.IO.Hashing;
using System.Security.Cryptography;

using Subrom.Domain.Hash;
using Subrom.Services.Interfaces;

namespace Subrom.Services;

/// <summary>
/// Service for computing multiple hash algorithms in parallel on streams.
/// Uses modern .NET APIs with optimized memory usage via ArrayPool.
/// </summary>
public sealed class HashService : IHashService {
	private const int ChunkSize = 64 * 1024; // 64KB chunks for better throughput

	public async Task<Hashes> GetAllAsync(Stream stream, CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(stream);

		// Use modern incremental hash APIs
		var crc32 = new Crc32();
		using var md5 = IncrementalHash.CreateHash(HashAlgorithmName.MD5);
		using var sha1 = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);

		var buffer = ArrayPool<byte>.Shared.Rent(ChunkSize);

		try {
			int bytesRead;
			while ((bytesRead = await stream.ReadAsync(buffer.AsMemory(0, ChunkSize), cancellationToken).ConfigureAwait(false)) > 0) {
				var span = buffer.AsSpan(0, bytesRead);
				crc32.Append(span);
				md5.AppendData(span);
				sha1.AppendData(span);
			}

			return new Hashes {
				Crc32 = Crc.From(Convert.ToHexStringLower(crc32.GetCurrentHash())),
				Md5 = Md5.From(Convert.ToHexStringLower(md5.GetCurrentHash())),
				Sha1 = Sha1.From(Convert.ToHexStringLower(sha1.GetCurrentHash())),
			};
		}
		finally {
			ArrayPool<byte>.Shared.Return(buffer);
		}
	}

	/// <summary>
	/// Computes hashes for a file at the specified path.
	/// </summary>
	public async Task<Hashes> GetAllFromFileAsync(string filePath, CancellationToken cancellationToken = default) {
		ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

		await using var stream = new FileStream(
			filePath,
			FileMode.Open,
			FileAccess.Read,
			FileShare.Read,
			bufferSize: ChunkSize,
			FileOptions.Asynchronous | FileOptions.SequentialScan
		);

		return await GetAllAsync(stream, cancellationToken).ConfigureAwait(false);
	}
}
