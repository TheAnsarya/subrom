using Subrom.Domain.Hash;

namespace Subrom.Services.Interfaces;

/// <summary>
/// Service for computing cryptographic hashes on streams and files.
/// </summary>
public interface IHashService {
	/// <summary>
	/// Computes CRC32, MD5, and SHA1 hashes from a stream.
	/// </summary>
	Task<Hashes> GetAllAsync(Stream stream, CancellationToken cancellationToken = default);

	/// <summary>
	/// Computes CRC32, MD5, and SHA1 hashes from a file.
	/// </summary>
	Task<Hashes> GetAllFromFileAsync(string filePath, CancellationToken cancellationToken = default);
}
