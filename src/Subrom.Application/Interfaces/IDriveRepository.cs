using Subrom.Domain.Aggregates.Storage;

namespace Subrom.Application.Interfaces;

/// <summary>
/// Repository interface for drive persistence.
/// </summary>
public interface IDriveRepository {
	/// <summary>
	/// Gets a drive by ID.
	/// </summary>
	Task<Drive?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets a drive by root path.
	/// </summary>
	Task<Drive?> GetByPathAsync(string rootPath, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets all registered drives.
	/// </summary>
	Task<IReadOnlyList<Drive>> GetAllAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets all online drives.
	/// </summary>
	Task<IReadOnlyList<Drive>> GetOnlineAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Adds a new drive.
	/// </summary>
	Task AddAsync(Drive drive, CancellationToken cancellationToken = default);

	/// <summary>
	/// Updates an existing drive.
	/// </summary>
	Task UpdateAsync(Drive drive, CancellationToken cancellationToken = default);

	/// <summary>
	/// Removes a drive.
	/// </summary>
	Task RemoveAsync(Drive drive, CancellationToken cancellationToken = default);
}
