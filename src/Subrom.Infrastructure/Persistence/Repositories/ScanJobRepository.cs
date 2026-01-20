using Microsoft.EntityFrameworkCore;
using Subrom.Application.Interfaces;
using Subrom.Domain.Aggregates.Scanning;

namespace Subrom.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of IScanJobRepository.
/// </summary>
public sealed class ScanJobRepository : IScanJobRepository {
	private readonly SubromDbContext _context;

	public ScanJobRepository(SubromDbContext context) {
		_context = context;
	}

	public async Task<ScanJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
		await _context.ScanJobs.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

	public async Task<IReadOnlyList<ScanJob>> GetAllAsync(CancellationToken cancellationToken = default) =>
		await _context.ScanJobs
			.OrderByDescending(s => s.StartedAt)
			.ToListAsync(cancellationToken);

	public async Task<IReadOnlyList<ScanJob>> GetByDriveAsync(Guid driveId, CancellationToken cancellationToken = default) =>
		await _context.ScanJobs
			.Where(s => s.DriveId == driveId)
			.OrderByDescending(s => s.StartedAt)
			.ToListAsync(cancellationToken);

	public async Task<ScanJob?> GetActiveAsync(CancellationToken cancellationToken = default) =>
		await _context.ScanJobs
			.FirstOrDefaultAsync(s => s.Status == ScanStatus.Running || s.Status == ScanStatus.Queued, cancellationToken);

	public async Task AddAsync(ScanJob scanJob, CancellationToken cancellationToken = default) {
		await _context.ScanJobs.AddAsync(scanJob, cancellationToken);
	}

	public Task UpdateAsync(ScanJob scanJob, CancellationToken cancellationToken = default) {
		_context.ScanJobs.Update(scanJob);
		return Task.CompletedTask;
	}
}
