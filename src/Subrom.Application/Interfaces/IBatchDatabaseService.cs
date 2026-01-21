using Subrom.Domain.Aggregates.DatFiles;
using Subrom.Domain.Aggregates.Storage;

namespace Subrom.Application.Interfaces;

/// <summary>
/// Service for efficient batch database operations.
/// Optimized for inserting 60K+ records efficiently.
/// </summary>
public interface IBatchDatabaseService {
	/// <summary>
	/// Bulk insert ROM files with chunked transactions.
	/// </summary>
	/// <param name="romFiles">ROM files to insert.</param>
	/// <param name="batchSize">Number of records per batch (default: 1000).</param>
	/// <param name="progress">Optional progress callback.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Number of records inserted.</returns>
	Task<int> BulkInsertRomFilesAsync(
		IAsyncEnumerable<RomFile> romFiles,
		int batchSize = 1000,
		IProgress<BatchInsertProgress>? progress = null,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Bulk insert ROM entries from DAT files.
	/// </summary>
	/// <param name="romEntries">ROM entries to insert.</param>
	/// <param name="batchSize">Number of records per batch (default: 1000).</param>
	/// <param name="progress">Optional progress callback.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Number of records inserted.</returns>
	Task<int> BulkInsertRomEntriesAsync(
		IAsyncEnumerable<RomEntry> romEntries,
		int batchSize = 1000,
		IProgress<BatchInsertProgress>? progress = null,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Bulk insert game entries from DAT files.
	/// </summary>
	/// <param name="games">Game entries to insert.</param>
	/// <param name="batchSize">Number of records per batch (default: 500).</param>
	/// <param name="progress">Optional progress callback.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Number of records inserted.</returns>
	Task<int> BulkInsertGamesAsync(
		IAsyncEnumerable<GameEntry> games,
		int batchSize = 500,
		IProgress<BatchInsertProgress>? progress = null,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Bulk update ROM files with computed hashes.
	/// </summary>
	/// <param name="updates">ROM file hash updates.</param>
	/// <param name="batchSize">Number of records per batch (default: 500).</param>
	/// <param name="progress">Optional progress callback.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Number of records updated.</returns>
	Task<int> BulkUpdateRomFileHashesAsync(
		IAsyncEnumerable<RomFileHashUpdate> updates,
		int batchSize = 500,
		IProgress<BatchInsertProgress>? progress = null,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Bulk delete ROM files by IDs.
	/// </summary>
	/// <param name="romFileIds">IDs of ROM files to delete.</param>
	/// <param name="batchSize">Number of records per batch (default: 1000).</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Number of records deleted.</returns>
	Task<int> BulkDeleteRomFilesAsync(
		IEnumerable<Guid> romFileIds,
		int batchSize = 1000,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Execute operation within an optimized SQLite transaction.
	/// Disables journal mode and synchronous writes for bulk operations.
	/// </summary>
	/// <typeparam name="T">Return type.</typeparam>
	/// <param name="operation">Operation to execute.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Operation result.</returns>
	Task<T> ExecuteInBulkTransactionAsync<T>(
		Func<CancellationToken, Task<T>> operation,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Get database statistics for performance monitoring.
	/// </summary>
	Task<DatabaseStats> GetDatabaseStatsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Progress information for batch insert operations.
/// </summary>
public record BatchInsertProgress(
	int TotalProcessed,
	int BatchNumber,
	int BatchSize,
	TimeSpan Elapsed,
	double RecordsPerSecond);

/// <summary>
/// ROM file hash update payload.
/// </summary>
public record RomFileHashUpdate(
	Guid RomFileId,
	string? Crc,
	string? Md5,
	string? Sha1,
	DateTime HashedAt);

/// <summary>
/// Database statistics for monitoring.
/// </summary>
public record DatabaseStats(
	long TotalRomFiles,
	long TotalRomEntries,
	long TotalGames,
	long TotalDatFiles,
	long DatabaseSizeBytes,
	int FragmentationPercent);
