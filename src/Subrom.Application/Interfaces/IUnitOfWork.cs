namespace Subrom.Application.Interfaces;

/// <summary>
/// Unit of work interface for transactional operations.
/// </summary>
public interface IUnitOfWork {
	/// <summary>
	/// Saves all pending changes to the database.
	/// </summary>
	Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Begins a new transaction.
	/// </summary>
	Task BeginTransactionAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Commits the current transaction.
	/// </summary>
	Task CommitTransactionAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Rolls back the current transaction.
	/// </summary>
	Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
