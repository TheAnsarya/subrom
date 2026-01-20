using Subrom.Domain.Aggregates.Scanning;

namespace Subrom.Application.Interfaces;

/// <summary>
/// Repository interface for scan job persistence.
/// </summary>
public interface IScanJobRepository {
	/// <summary>
	/// Gets a scan job by ID.
	/// </summary>
	Task<ScanJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets all scan jobs ordered by most recent.
	/// </summary>
	Task<IReadOnlyList<ScanJob>> GetAllAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets scan jobs for a specific drive.
	/// </summary>
	Task<IReadOnlyList<ScanJob>> GetByDriveAsync(Guid driveId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets the currently active scan jobs (running or queued).
	/// </summary>
	Task<IReadOnlyList<ScanJob>> GetActiveAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Checks if a drive has an active scan job.
	/// </summary>
	Task<bool> HasActiveJobForDriveAsync(Guid driveId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Adds a new scan job.
	/// </summary>
	Task AddAsync(ScanJob scanJob, CancellationToken cancellationToken = default);

	/// <summary>
	/// Updates an existing scan job.
	/// </summary>
	Task UpdateAsync(ScanJob scanJob, CancellationToken cancellationToken = default);
}
