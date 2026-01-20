using Subrom.Application.Interfaces;

namespace Subrom.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of IUnitOfWork.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork {
	private readonly SubromDbContext _context;

	public UnitOfWork(SubromDbContext context) {
		_context = context;
	}

	public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
		await _context.SaveChangesAsync(cancellationToken);

	public async Task BeginTransactionAsync(CancellationToken cancellationToken = default) =>
		await _context.Database.BeginTransactionAsync(cancellationToken);

	public async Task CommitTransactionAsync(CancellationToken cancellationToken = default) {
		if (_context.Database.CurrentTransaction is not null) {
			await _context.Database.CommitTransactionAsync(cancellationToken);
		}
	}

	public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default) {
		if (_context.Database.CurrentTransaction is not null) {
			await _context.Database.RollbackTransactionAsync(cancellationToken);
		}
	}
}
