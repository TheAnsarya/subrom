using Microsoft.EntityFrameworkCore;
using Subrom.Application.Interfaces;
using Subrom.Domain.Aggregates.Organization;

namespace Subrom.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for organization operation logs.
/// </summary>
public class OrganizationLogRepository : IOrganizationLogRepository {
	private readonly SubromDbContext _context;

	public OrganizationLogRepository(SubromDbContext context) {
		_context = context;
	}

	/// <inheritdoc />
	public async Task<OrganizationOperationLog> AddAsync(OrganizationOperationLog log, CancellationToken cancellationToken = default) {
		_context.OrganizationOperationLogs.Add(log);
		await _context.SaveChangesAsync(cancellationToken);
		return log;
	}

	/// <inheritdoc />
	public async Task<OrganizationOperationLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) {
		return await _context.OrganizationOperationLogs
			.AsNoTracking()
			.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
	}

	/// <inheritdoc />
	public async Task<OrganizationOperationLog?> GetWithEntriesAsync(Guid id, CancellationToken cancellationToken = default) {
		return await _context.OrganizationOperationLogs
			.Include(x => x.Entries)
			.AsNoTracking()
			.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
	}

	/// <inheritdoc />
	public async Task<IReadOnlyList<OrganizationOperationLog>> GetRecentAsync(int count, CancellationToken cancellationToken = default) {
		return await _context.OrganizationOperationLogs
			.AsNoTracking()
			.OrderByDescending(x => x.PerformedAt)
			.Take(count)
			.ToListAsync(cancellationToken);
	}

	/// <inheritdoc />
	public async Task<IReadOnlyList<OrganizationOperationLog>> GetRollbackableAsync(CancellationToken cancellationToken = default) {
		return await _context.OrganizationOperationLogs
			.AsNoTracking()
			.Where(x => x.WasMoveOperation && x.Success && !x.IsRolledBack && x.RollbackDataJson != null)
			.OrderByDescending(x => x.PerformedAt)
			.ToListAsync(cancellationToken);
	}

	/// <inheritdoc />
	public async Task UpdateAsync(OrganizationOperationLog log, CancellationToken cancellationToken = default) {
		_context.OrganizationOperationLogs.Update(log);
		await _context.SaveChangesAsync(cancellationToken);
	}

	/// <inheritdoc />
	public async Task AddEntriesAsync(Guid operationId, IEnumerable<OrganizationOperationEntry> entries, CancellationToken cancellationToken = default) {
		var entryList = entries.ToList();
		foreach (var entry in entryList) {
			// Create a new entry with the operation ID set
			var newEntry = new OrganizationOperationEntry {
				Id = entry.Id,
				OperationId = operationId,
				OperationType = entry.OperationType,
				SourcePath = entry.SourcePath,
				DestinationPath = entry.DestinationPath,
				Size = entry.Size,
				Success = entry.Success,
				ErrorMessage = entry.ErrorMessage,
				ProcessedAt = entry.ProcessedAt,
				Crc = entry.Crc
			};
			_context.OrganizationOperationEntries.Add(newEntry);
		}
		await _context.SaveChangesAsync(cancellationToken);
	}

	/// <inheritdoc />
	public async Task<OrganizationLogStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default) {
		var logs = await _context.OrganizationOperationLogs
			.AsNoTracking()
			.ToListAsync(cancellationToken);

		return new OrganizationLogStatistics {
			TotalOperations = logs.Count,
			SuccessfulOperations = logs.Count(x => x.Success),
			FailedOperations = logs.Count(x => !x.Success),
			TotalFilesProcessed = logs.Sum(x => x.FilesProcessed),
			TotalBytesProcessed = logs.Sum(x => x.BytesProcessed),
			RolledBackOperations = logs.Count(x => x.IsRolledBack),
			LastOperationAt = logs.MaxBy(x => x.PerformedAt)?.PerformedAt
		};
	}
}
