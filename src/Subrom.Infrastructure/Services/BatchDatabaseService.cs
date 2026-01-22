using System.Data;
using System.Diagnostics;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Subrom.Application.Interfaces;
using Subrom.Domain.Aggregates.DatFiles;
using Subrom.Domain.Aggregates.Storage;
using Subrom.Infrastructure.Persistence;

namespace Subrom.Infrastructure.Services;

/// <summary>
/// Service for efficient batch database operations.
/// Uses raw SQL and SQLite PRAGMA optimizations for 60K+ record inserts.
/// </summary>
public class BatchDatabaseService : IBatchDatabaseService {
	private readonly SubromDbContext _context;
	private readonly ILogger<BatchDatabaseService> _logger;

	public BatchDatabaseService(SubromDbContext context, ILogger<BatchDatabaseService> logger) {
		_context = context;
		_logger = logger;
	}

	public async Task<int> BulkInsertRomFilesAsync(
		IAsyncEnumerable<RomFile> romFiles,
		int batchSize = 1000,
		IProgress<BatchInsertProgress>? progress = null,
		CancellationToken cancellationToken = default) {

		var stopwatch = Stopwatch.StartNew();
		var totalInserted = 0;
		var batchNumber = 0;
		var batch = new List<RomFile>(batchSize);

		await foreach (var romFile in romFiles.WithCancellation(cancellationToken)) {
			batch.Add(romFile);

			if (batch.Count >= batchSize) {
				await InsertRomFileBatchAsync(batch, cancellationToken);
				totalInserted += batch.Count;
				batchNumber++;

				progress?.Report(new BatchInsertProgress(
					totalInserted,
					batchNumber,
					batch.Count,
					stopwatch.Elapsed,
					totalInserted / stopwatch.Elapsed.TotalSeconds));

				batch.Clear();
			}
		}

		// Insert remaining items
		if (batch.Count > 0) {
			await InsertRomFileBatchAsync(batch, cancellationToken);
			totalInserted += batch.Count;
			batchNumber++;

			progress?.Report(new BatchInsertProgress(
				totalInserted,
				batchNumber,
				batch.Count,
				stopwatch.Elapsed,
				totalInserted / stopwatch.Elapsed.TotalSeconds));
		}

		_logger.LogInformation(
			"Bulk inserted {Count} ROM files in {Elapsed:N2}s ({Rate:N0} records/sec)",
			totalInserted,
			stopwatch.Elapsed.TotalSeconds,
			totalInserted / stopwatch.Elapsed.TotalSeconds);

		return totalInserted;
	}

	public async Task<int> BulkInsertRomEntriesAsync(
		IAsyncEnumerable<RomEntry> romEntries,
		int batchSize = 1000,
		IProgress<BatchInsertProgress>? progress = null,
		CancellationToken cancellationToken = default) {

		var stopwatch = Stopwatch.StartNew();
		var totalInserted = 0;
		var batchNumber = 0;
		var batch = new List<RomEntry>(batchSize);

		await foreach (var romEntry in romEntries.WithCancellation(cancellationToken)) {
			batch.Add(romEntry);

			if (batch.Count >= batchSize) {
				await InsertRomEntryBatchAsync(batch, cancellationToken);
				totalInserted += batch.Count;
				batchNumber++;

				progress?.Report(new BatchInsertProgress(
					totalInserted,
					batchNumber,
					batch.Count,
					stopwatch.Elapsed,
					totalInserted / stopwatch.Elapsed.TotalSeconds));

				batch.Clear();
			}
		}

		// Insert remaining items
		if (batch.Count > 0) {
			await InsertRomEntryBatchAsync(batch, cancellationToken);
			totalInserted += batch.Count;
			batchNumber++;

			progress?.Report(new BatchInsertProgress(
				totalInserted,
				batchNumber,
				batch.Count,
				stopwatch.Elapsed,
				totalInserted / stopwatch.Elapsed.TotalSeconds));
		}

		_logger.LogInformation(
			"Bulk inserted {Count} ROM entries in {Elapsed:N2}s ({Rate:N0} records/sec)",
			totalInserted,
			stopwatch.Elapsed.TotalSeconds,
			totalInserted / stopwatch.Elapsed.TotalSeconds);

		return totalInserted;
	}

	public async Task<int> BulkInsertGamesAsync(
		IAsyncEnumerable<GameEntry> games,
		int batchSize = 500,
		IProgress<BatchInsertProgress>? progress = null,
		CancellationToken cancellationToken = default) {

		var stopwatch = Stopwatch.StartNew();
		var totalInserted = 0;
		var batchNumber = 0;
		var batch = new List<GameEntry>(batchSize);

		await foreach (var game in games.WithCancellation(cancellationToken)) {
			batch.Add(game);

			if (batch.Count >= batchSize) {
				await InsertGameBatchAsync(batch, cancellationToken);
				totalInserted += batch.Count;
				batchNumber++;

				progress?.Report(new BatchInsertProgress(
					totalInserted,
					batchNumber,
					batch.Count,
					stopwatch.Elapsed,
					totalInserted / stopwatch.Elapsed.TotalSeconds));

				batch.Clear();
			}
		}

		// Insert remaining items
		if (batch.Count > 0) {
			await InsertGameBatchAsync(batch, cancellationToken);
			totalInserted += batch.Count;
			batchNumber++;

			progress?.Report(new BatchInsertProgress(
				totalInserted,
				batchNumber,
				batch.Count,
				stopwatch.Elapsed,
				totalInserted / stopwatch.Elapsed.TotalSeconds));
		}

		_logger.LogInformation(
			"Bulk inserted {Count} games in {Elapsed:N2}s ({Rate:N0} records/sec)",
			totalInserted,
			stopwatch.Elapsed.TotalSeconds,
			totalInserted / stopwatch.Elapsed.TotalSeconds);

		return totalInserted;
	}

	public async Task<int> BulkUpdateRomFileHashesAsync(
		IAsyncEnumerable<RomFileHashUpdate> updates,
		int batchSize = 500,
		IProgress<BatchInsertProgress>? progress = null,
		CancellationToken cancellationToken = default) {

		var stopwatch = Stopwatch.StartNew();
		var totalUpdated = 0;
		var batchNumber = 0;
		var batch = new List<RomFileHashUpdate>(batchSize);

		await foreach (var update in updates.WithCancellation(cancellationToken)) {
			batch.Add(update);

			if (batch.Count >= batchSize) {
				await UpdateRomFileHashBatchAsync(batch, cancellationToken);
				totalUpdated += batch.Count;
				batchNumber++;

				progress?.Report(new BatchInsertProgress(
					totalUpdated,
					batchNumber,
					batch.Count,
					stopwatch.Elapsed,
					totalUpdated / stopwatch.Elapsed.TotalSeconds));

				batch.Clear();
			}
		}

		// Update remaining items
		if (batch.Count > 0) {
			await UpdateRomFileHashBatchAsync(batch, cancellationToken);
			totalUpdated += batch.Count;
			batchNumber++;

			progress?.Report(new BatchInsertProgress(
				totalUpdated,
				batchNumber,
				batch.Count,
				stopwatch.Elapsed,
				totalUpdated / stopwatch.Elapsed.TotalSeconds));
		}

		_logger.LogInformation(
			"Bulk updated {Count} ROM file hashes in {Elapsed:N2}s ({Rate:N0} records/sec)",
			totalUpdated,
			stopwatch.Elapsed.TotalSeconds,
			totalUpdated / stopwatch.Elapsed.TotalSeconds);

		return totalUpdated;
	}

	public async Task<int> BulkDeleteRomFilesAsync(
		IEnumerable<Guid> romFileIds,
		int batchSize = 1000,
		CancellationToken cancellationToken = default) {

		var ids = romFileIds.ToList();
		var totalDeleted = 0;

		foreach (var batch in ids.Chunk(batchSize)) {
			var idList = string.Join(",", batch.Select(id => $"'{id}'"));
			var sql = $"DELETE FROM RomFiles WHERE Id IN ({idList})";

			totalDeleted += await _context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
		}

		_logger.LogInformation("Bulk deleted {Count} ROM files", totalDeleted);
		return totalDeleted;
	}

	public async Task<T> ExecuteInBulkTransactionAsync<T>(
		Func<CancellationToken, Task<T>> operation,
		CancellationToken cancellationToken = default) {

		var connection = _context.Database.GetDbConnection();

		if (connection.State != ConnectionState.Open) {
			await connection.OpenAsync(cancellationToken);
		}

		// Enable SQLite optimizations for bulk operations
		await using var pragmaCommand = connection.CreateCommand();
		pragmaCommand.CommandText = """
			PRAGMA journal_mode = OFF;
			PRAGMA synchronous = OFF;
			PRAGMA temp_store = MEMORY;
			PRAGMA cache_size = -64000;
			""";
		await pragmaCommand.ExecuteNonQueryAsync(cancellationToken);

		try {
			await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
			_context.Database.UseTransaction(transaction as System.Data.Common.DbTransaction);

			var result = await operation(cancellationToken);

			await transaction.CommitAsync(cancellationToken);

			return result;
		} finally {
			// Restore safe defaults
			await using var restoreCommand = connection.CreateCommand();
			restoreCommand.CommandText = """
				PRAGMA journal_mode = WAL;
				PRAGMA synchronous = NORMAL;
				""";
			await restoreCommand.ExecuteNonQueryAsync(cancellationToken);
		}
	}

	public async Task<DatabaseStats> GetDatabaseStatsAsync(CancellationToken cancellationToken = default) {
		var romFileCount = await _context.RomFiles.LongCountAsync(cancellationToken);
		var romEntryCount = await _context.Roms.LongCountAsync(cancellationToken);
		var gameCount = await _context.Games.LongCountAsync(cancellationToken);
		var datFileCount = await _context.DatFiles.LongCountAsync(cancellationToken);

		// Get database size and fragmentation
		var connection = _context.Database.GetDbConnection();
		if (connection.State != ConnectionState.Open) {
			await connection.OpenAsync(cancellationToken);
		}

		await using var sizeCommand = connection.CreateCommand();
		sizeCommand.CommandText = "SELECT page_count * page_size FROM pragma_page_count(), pragma_page_size()";
		var sizeResult = await sizeCommand.ExecuteScalarAsync(cancellationToken);
		var databaseSize = Convert.ToInt64(sizeResult ?? 0);

		await using var fragCommand = connection.CreateCommand();
		fragCommand.CommandText = "SELECT (freelist_count * 100) / page_count FROM pragma_freelist_count(), pragma_page_count()";
		var fragResult = await fragCommand.ExecuteScalarAsync(cancellationToken);
		var fragmentation = Convert.ToInt32(fragResult ?? 0);

		return new DatabaseStats(
			romFileCount,
			romEntryCount,
			gameCount,
			datFileCount,
			databaseSize,
			fragmentation);
	}

	private async Task InsertRomFileBatchAsync(List<RomFile> batch, CancellationToken cancellationToken) {
		// Use parameterized batch insert for safety and performance
		var sql = """
			INSERT INTO RomFiles (Id, RelativePath, FileName, Size, Crc, Md5, Sha1, DriveId, ScannedAt, HashedAt, LastModified, IsArchived, ArchivePath, PathInArchive, VerificationStatus)
			VALUES
			""";

		var parameters = new List<SqliteParameter>();
		var valuesClauses = new List<string>();

		for (var i = 0; i < batch.Count; i++) {
			var rf = batch[i];
			valuesClauses.Add($"(@id{i}, @rp{i}, @fn{i}, @sz{i}, @crc{i}, @md5{i}, @sha1{i}, @did{i}, @sa{i}, @ha{i}, @lm{i}, @ia{i}, @ap{i}, @pia{i}, @vs{i})");

			parameters.Add(new SqliteParameter($"@id{i}", rf.Id));
			parameters.Add(new SqliteParameter($"@rp{i}", rf.RelativePath));
			parameters.Add(new SqliteParameter($"@fn{i}", rf.FileName));
			parameters.Add(new SqliteParameter($"@sz{i}", rf.Size));
			parameters.Add(new SqliteParameter($"@crc{i}", rf.Crc ?? (object)DBNull.Value));
			parameters.Add(new SqliteParameter($"@md5{i}", rf.Md5 ?? (object)DBNull.Value));
			parameters.Add(new SqliteParameter($"@sha1{i}", rf.Sha1 ?? (object)DBNull.Value));
			parameters.Add(new SqliteParameter($"@did{i}", rf.DriveId));
			parameters.Add(new SqliteParameter($"@sa{i}", rf.ScannedAt));
			parameters.Add(new SqliteParameter($"@ha{i}", rf.HashedAt ?? (object)DBNull.Value));
			parameters.Add(new SqliteParameter($"@lm{i}", rf.LastModified));
			parameters.Add(new SqliteParameter($"@ia{i}", rf.IsArchived));
			parameters.Add(new SqliteParameter($"@ap{i}", rf.ArchivePath ?? (object)DBNull.Value));
			parameters.Add(new SqliteParameter($"@pia{i}", rf.PathInArchive ?? (object)DBNull.Value));
			parameters.Add(new SqliteParameter($"@vs{i}", (int)rf.VerificationStatus));
		}

		sql += string.Join(",\n", valuesClauses);

		await _context.Database.ExecuteSqlRawAsync(sql, parameters, cancellationToken);
	}

	private async Task InsertRomEntryBatchAsync(List<RomEntry> batch, CancellationToken cancellationToken) {
		var sql = """
			INSERT INTO Roms (Id, Name, Size, Crc, Md5, Sha1, Status, Serial, IsBios, Merge, GameId)
			VALUES
			""";

		var parameters = new List<SqliteParameter>();
		var valuesClauses = new List<string>();

		for (var i = 0; i < batch.Count; i++) {
			var re = batch[i];
			valuesClauses.Add($"(@id{i}, @nm{i}, @sz{i}, @crc{i}, @md5{i}, @sha1{i}, @st{i}, @sr{i}, @ib{i}, @mg{i}, @gid{i})");

			parameters.Add(new SqliteParameter($"@id{i}", re.Id));
			parameters.Add(new SqliteParameter($"@nm{i}", re.Name));
			parameters.Add(new SqliteParameter($"@sz{i}", re.Size));
			parameters.Add(new SqliteParameter($"@crc{i}", re.Crc ?? (object)DBNull.Value));
			parameters.Add(new SqliteParameter($"@md5{i}", re.Md5 ?? (object)DBNull.Value));
			parameters.Add(new SqliteParameter($"@sha1{i}", re.Sha1 ?? (object)DBNull.Value));
			parameters.Add(new SqliteParameter($"@st{i}", (int)re.Status));
			parameters.Add(new SqliteParameter($"@sr{i}", re.Serial ?? (object)DBNull.Value));
			parameters.Add(new SqliteParameter($"@ib{i}", re.IsBios));
			parameters.Add(new SqliteParameter($"@mg{i}", re.Merge ?? (object)DBNull.Value));
			parameters.Add(new SqliteParameter($"@gid{i}", re.GameId ?? (object)DBNull.Value));
		}

		sql += string.Join(",\n", valuesClauses);

		await _context.Database.ExecuteSqlRawAsync(sql, parameters, cancellationToken);
	}

	private async Task InsertGameBatchAsync(List<GameEntry> batch, CancellationToken cancellationToken) {
		var sql = """
			INSERT INTO Games (Id, Name, Description, Year, Publisher, Region, Languages, CloneOf, RomOf, SampleOf, IsBios, IsDevice, IsMechanical, Category, DatFileId)
			VALUES
			""";

		var parameters = new List<SqliteParameter>();
		var valuesClauses = new List<string>();

		for (var i = 0; i < batch.Count; i++) {
			var g = batch[i];
			valuesClauses.Add($"(@id{i}, @nm{i}, @ds{i}, @yr{i}, @pb{i}, @rg{i}, @lg{i}, @co{i}, @ro{i}, @so{i}, @ib{i}, @id{i}d, @im{i}, @ct{i}, @dfid{i})");

			parameters.Add(new SqliteParameter($"@id{i}", g.Id));
			parameters.Add(new SqliteParameter($"@nm{i}", g.Name));
			parameters.Add(new SqliteParameter($"@ds{i}", g.Description ?? (object)DBNull.Value));
			parameters.Add(new SqliteParameter($"@yr{i}", g.Year ?? (object)DBNull.Value));
			parameters.Add(new SqliteParameter($"@pb{i}", g.Publisher ?? (object)DBNull.Value));
			parameters.Add(new SqliteParameter($"@rg{i}", g.Region ?? (object)DBNull.Value));
			parameters.Add(new SqliteParameter($"@lg{i}", g.Languages ?? (object)DBNull.Value));
			parameters.Add(new SqliteParameter($"@co{i}", g.CloneOf ?? (object)DBNull.Value));
			parameters.Add(new SqliteParameter($"@ro{i}", g.RomOf ?? (object)DBNull.Value));
			parameters.Add(new SqliteParameter($"@so{i}", g.SampleOf ?? (object)DBNull.Value));
			parameters.Add(new SqliteParameter($"@ib{i}", g.IsBios));
			parameters.Add(new SqliteParameter($"@id{i}d", g.IsDevice));
			parameters.Add(new SqliteParameter($"@im{i}", g.IsMechanical));
			parameters.Add(new SqliteParameter($"@ct{i}", g.Category ?? (object)DBNull.Value));
			parameters.Add(new SqliteParameter($"@dfid{i}", g.DatFileId));
		}

		sql += string.Join(",\n", valuesClauses);

		await _context.Database.ExecuteSqlRawAsync(sql, parameters, cancellationToken);
	}

	private async Task UpdateRomFileHashBatchAsync(List<RomFileHashUpdate> batch, CancellationToken cancellationToken) {
		// SQLite doesn't support bulk UPDATE, so we use a CASE statement approach
		// or individual updates within a transaction
		foreach (var update in batch) {
			await _context.Database.ExecuteSqlRawAsync(
				"""
				UPDATE RomFiles
				SET Crc = @crc, Md5 = @md5, Sha1 = @sha1, HashedAt = @hashedAt
				WHERE Id = @id
				""",
				[
					new SqliteParameter("@id", update.RomFileId),
					new SqliteParameter("@crc", update.Crc ?? (object)DBNull.Value),
					new SqliteParameter("@md5", update.Md5 ?? (object)DBNull.Value),
					new SqliteParameter("@sha1", update.Sha1 ?? (object)DBNull.Value),
					new SqliteParameter("@hashedAt", update.HashedAt)
				],
				cancellationToken);
		}
	}
}
