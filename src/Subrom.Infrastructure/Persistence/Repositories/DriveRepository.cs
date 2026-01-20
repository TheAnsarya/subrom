using Microsoft.EntityFrameworkCore;
using Subrom.Application.Interfaces;
using Subrom.Domain.Aggregates.Storage;

namespace Subrom.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of IDriveRepository.
/// </summary>
public sealed class DriveRepository : IDriveRepository {
	private readonly SubromDbContext _context;

	public DriveRepository(SubromDbContext context) {
		_context = context;
	}

	public async Task<Drive?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
		await _context.Drives.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

	public async Task<Drive?> GetByPathAsync(string rootPath, CancellationToken cancellationToken = default) =>
		await _context.Drives.FirstOrDefaultAsync(d => d.RootPath == rootPath, cancellationToken);

	public async Task<IReadOnlyList<Drive>> GetAllAsync(CancellationToken cancellationToken = default) =>
		await _context.Drives
			.OrderBy(d => d.Label)
			.ToListAsync(cancellationToken);

	public async Task<IReadOnlyList<Drive>> GetOnlineAsync(CancellationToken cancellationToken = default) =>
		await _context.Drives
			.Where(d => d.IsOnline)
			.OrderBy(d => d.Label)
			.ToListAsync(cancellationToken);

	public async Task AddAsync(Drive drive, CancellationToken cancellationToken = default) {
		await _context.Drives.AddAsync(drive, cancellationToken);
	}

	public Task UpdateAsync(Drive drive, CancellationToken cancellationToken = default) {
		_context.Drives.Update(drive);
		return Task.CompletedTask;
	}

	public Task RemoveAsync(Drive drive, CancellationToken cancellationToken = default) {
		_context.Drives.Remove(drive);
		return Task.CompletedTask;
	}
}
