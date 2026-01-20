using Subrom.Domain.Aggregates.Storage;
using Subrom.Domain.ValueObjects;

namespace Subrom.Application.Interfaces;

/// <summary>
/// Repository interface for ROM file persistence.
/// </summary>
public interface IRomFileRepository {
	/// <summary>
	/// Gets a ROM file by ID.
	/// </summary>
	Task<RomFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets ROM files for a drive.
	/// </summary>
	Task<IReadOnlyList<RomFile>> GetByDriveAsync(Guid driveId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets ROM files by hash (any matching hash).
	/// </summary>
	Task<IReadOnlyList<RomFile>> GetByHashAsync(RomHashes hashes, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets ROM files by CRC.
	/// </summary>
	Task<IReadOnlyList<RomFile>> GetByCrcAsync(Crc crc, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets ROM files by SHA-1.
	/// </summary>
	Task<IReadOnlyList<RomFile>> GetBySha1Async(Sha1 sha1, CancellationToken cancellationToken = default);

	/// <summary>
	/// Checks if a file exists at the given path on a drive.
	/// </summary>
	Task<bool> ExistsByPathAsync(Guid driveId, string relativePath, CancellationToken cancellationToken = default);

	/// <summary>
	/// Adds a new ROM file.
	/// </summary>
	Task AddAsync(RomFile romFile, CancellationToken cancellationToken = default);

	/// <summary>
	/// Adds multiple ROM files in a batch.
	/// </summary>
	Task AddRangeAsync(IEnumerable<RomFile> romFiles, CancellationToken cancellationToken = default);

	/// <summary>
	/// Updates an existing ROM file.
	/// </summary>
	Task UpdateAsync(RomFile romFile, CancellationToken cancellationToken = default);

	/// <summary>
	/// Updates multiple ROM files in a batch.
	/// </summary>
	Task UpdateRangeAsync(IEnumerable<RomFile> romFiles, CancellationToken cancellationToken = default);

	/// <summary>
	/// Removes a ROM file.
	/// </summary>
	Task RemoveAsync(RomFile romFile, CancellationToken cancellationToken = default);

	/// <summary>
	/// Removes all ROM files for a drive.
	/// </summary>
	Task RemoveByDriveAsync(Guid driveId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets the count of ROM files for a drive.
	/// </summary>
	Task<int> GetCountByDriveAsync(Guid driveId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets the total count of ROM files.
	/// </summary>
	Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets ROM files that need hashing (have no hashes).
	/// </summary>
	Task<IReadOnlyList<RomFile>> GetUnhashedAsync(int limit, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets ROM files that need verification.
	/// </summary>
	Task<IReadOnlyList<RomFile>> GetUnverifiedAsync(int limit, CancellationToken cancellationToken = default);
}
