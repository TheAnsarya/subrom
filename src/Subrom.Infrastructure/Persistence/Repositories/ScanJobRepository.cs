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
			.OrderByDescending(s => s.QueuedAt)
			.ToListAsync(cancellationToken);

	public async Task<IReadOnlyList<ScanJob>> GetByDriveAsync(Guid driveId, CancellationToken cancellationToken = default) =>
		await _context.ScanJobs
			.Where(s => s.DriveId == driveId)
			.OrderByDescending(s => s.QueuedAt)
			.ToListAsync(cancellationToken);

	public async Task<IReadOnlyList<ScanJob>> GetActiveAsync(CancellationToken cancellationToken = default) =>
		await _context.ScanJobs
			.Where(s => s.Status == ScanStatus.Running || s.Status == ScanStatus.Queued)
			.OrderByDescending(s => s.QueuedAt)
			.ToListAsync(cancellationToken);

	public async Task<bool> HasActiveJobForDriveAsync(Guid driveId, CancellationToken cancellationToken = default) =>
		await _context.ScanJobs
			.AnyAsync(s =>
				s.DriveId == driveId &&
				(s.Status == ScanStatus.Running || s.Status == ScanStatus.Queued),
				cancellationToken);

	public async Task AddAsync(ScanJob scanJob, CancellationToken cancellationToken = default) {
		await _context.ScanJobs.AddAsync(scanJob, cancellationToken);
	}

	public Task UpdateAsync(ScanJob scanJob, CancellationToken cancellationToken = default) {
		_context.ScanJobs.Update(scanJob);
		return Task.CompletedTask;
	}

	public async Task<IReadOnlyList<ScanJob>> GetResumableAsync(CancellationToken cancellationToken = default) =>
		await _context.ScanJobs
			.Where(s => s.Status == ScanStatus.Paused || s.Status == ScanStatus.Failed)
			.OrderByDescending(s => s.PausedAt ?? s.CompletedAt)
			.ToListAsync(cancellationToken);

	public async Task<ScanJob?> GetResumableForPathAsync(string targetPath, CancellationToken cancellationToken = default) =>
		await _context.ScanJobs
			.Where(s =>
				(s.Status == ScanStatus.Paused || s.Status == ScanStatus.Failed) &&
				s.TargetPath == targetPath)
			.OrderByDescending(s => s.PausedAt ?? s.CompletedAt)
			.FirstOrDefaultAsync(cancellationToken);
}
