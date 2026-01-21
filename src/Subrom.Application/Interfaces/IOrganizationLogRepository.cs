using Subrom.Domain.Aggregates.Organization;

namespace Subrom.Application.Interfaces;

/// <summary>
/// Repository for organization operation logs.
/// </summary>
public interface IOrganizationLogRepository {
	/// <summary>
	/// Adds a new operation log.
	/// </summary>
	Task<OrganizationOperationLog> AddAsync(OrganizationOperationLog log, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets an operation log by ID.
	/// </summary>
	Task<OrganizationOperationLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets an operation log with all its entries.
	/// </summary>
	Task<OrganizationOperationLog?> GetWithEntriesAsync(Guid id, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets recent operation logs.
	/// </summary>
	Task<IReadOnlyList<OrganizationOperationLog>> GetRecentAsync(int count, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets operations that can be rolled back.
	/// </summary>
	Task<IReadOnlyList<OrganizationOperationLog>> GetRollbackableAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Updates an operation log.
	/// </summary>
	Task UpdateAsync(OrganizationOperationLog log, CancellationToken cancellationToken = default);

	/// <summary>
	/// Adds entries to an operation.
	/// </summary>
	Task AddEntriesAsync(Guid operationId, IEnumerable<OrganizationOperationEntry> entries, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets operation statistics.
	/// </summary>
	Task<OrganizationLogStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Statistics about organization operations.
/// </summary>
public record OrganizationLogStatistics {
	/// <summary>Total number of operations.</summary>
	public int TotalOperations { get; init; }

	/// <summary>Number of successful operations.</summary>
	public int SuccessfulOperations { get; init; }

	/// <summary>Number of failed operations.</summary>
	public int FailedOperations { get; init; }

	/// <summary>Total files processed.</summary>
	public long TotalFilesProcessed { get; init; }

	/// <summary>Total bytes processed.</summary>
	public long TotalBytesProcessed { get; init; }

	/// <summary>Number of operations rolled back.</summary>
	public int RolledBackOperations { get; init; }

	/// <summary>Last operation date.</summary>
	public DateTime? LastOperationAt { get; init; }
}
