using Microsoft.EntityFrameworkCore;
using Subrom.Domain.Aggregates.Storage;
using Subrom.Infrastructure.Persistence;

namespace Subrom.Server.Endpoints;

/// <summary>
/// ROM file management endpoints.
/// </summary>
public static class RomFileEndpoints {
	public static IEndpointRouteBuilder MapRomFileEndpoints(this IEndpointRouteBuilder endpoints) {
		var group = endpoints.MapGroup("/romfiles")
			.WithTags("ROM Files");

		// Get ROM files (paginated with cursor)
		group.MapGet("/", async (
			Guid? driveId,
			VerificationStatus? status,
			string? search,
			Guid? cursor,
			int limit,
			SubromDbContext db,
			CancellationToken ct) => {
			limit = Math.Clamp(limit, 1, 1000);

			var query = db.RomFiles.AsNoTracking();

			if (driveId.HasValue) {
				query = query.Where(f => f.DriveId == driveId.Value);
			}

			if (status.HasValue) {
				query = query.Where(f => f.VerificationStatus == status.Value);
			}

			if (!string.IsNullOrWhiteSpace(search)) {
				query = query.Where(f => f.FileName.Contains(search));
			}

			// Cursor-based pagination
			if (cursor.HasValue) {
				query = query.Where(f => f.Id.CompareTo(cursor.Value) > 0);
			}

			var files = await query
				.OrderBy(f => f.Id)
				.Take(limit + 1)
				.Select(f => new {
					f.Id,
					f.FileName,
					f.RelativePath,
					f.Size,
					f.DriveId,
					f.VerificationStatus,
					f.ScannedAt,
					f.HashedAt,
					HasHashes = f.Crc != null && f.Md5 != null && f.Sha1 != null
				})
				.ToListAsync(ct);

			var hasMore = files.Count > limit;
			var items = hasMore ? files.Take(limit).ToList() : files;
			var nextCursor = hasMore && items.Count > 0 ? items[^1].Id : (Guid?)null;

			return Results.Ok(new {
				Items = items,
				NextCursor = nextCursor,
				HasMore = hasMore
			});
		});

		// Get ROM file by ID
		group.MapGet("/{id:guid}", async (Guid id, SubromDbContext db, CancellationToken ct) => {
			var file = await db.RomFiles
				.AsNoTracking()
				.FirstOrDefaultAsync(f => f.Id == id, ct);

			return file is null
				? Results.NotFound()
				: Results.Ok(file);
		});

		// Get ROM file with match details
		group.MapGet("/{id:guid}/match", async (Guid id, SubromDbContext db, CancellationToken ct) => {
			var file = await db.RomFiles
				.AsNoTracking()
				.FirstOrDefaultAsync(f => f.Id == id, ct);

			if (file is null) {
				return Results.NotFound();
			}

			if (!file.MatchedDatFileId.HasValue || !file.MatchedRomEntryId.HasValue) {
				return Results.Ok(new { File = file, Match = (object?)null });
			}

			var datFile = await db.DatFiles
				.AsNoTracking()
				.Where(d => d.Id == file.MatchedDatFileId)
				.Select(d => new { d.Id, d.Name, d.System })
				.FirstOrDefaultAsync(ct);

			var romEntry = await db.Roms
				.AsNoTracking()
				.Where(r => r.Id == file.MatchedRomEntryId)
				.FirstOrDefaultAsync(ct);

			var game = romEntry?.GameId != null
				? await db.Games.AsNoTracking()
					.Where(g => g.Id == romEntry.GameId)
					.Select(g => new { g.Id, g.Name, g.Description, g.Region })
					.FirstOrDefaultAsync(ct)
				: null;

			return Results.Ok(new {
				File = file,
				Match = new {
					DatFile = datFile,
					Game = game,
					RomEntry = romEntry
				}
			});
		});

		// Get statistics
		group.MapGet("/stats", async (SubromDbContext db, CancellationToken ct) => {
			var totalCount = await db.RomFiles.CountAsync(ct);
			var totalSize = await db.RomFiles.SumAsync(f => f.Size, ct);
			var byStatus = await db.RomFiles
				.GroupBy(f => f.VerificationStatus)
				.Select(g => new { Status = g.Key, Count = g.Count() })
				.ToListAsync(ct);
			var hashedCount = await db.RomFiles.CountAsync(f => f.Crc != null, ct);

			return Results.Ok(new {
				TotalCount = totalCount,
				TotalSize = totalSize,
				HashedCount = hashedCount,
				UnhashedCount = totalCount - hashedCount,
				ByStatus = byStatus
			});
		});

		// Search by hash
		group.MapGet("/search/hash", async (
			string? crc,
			string? sha1,
			SubromDbContext db,
			CancellationToken ct) => {
			if (string.IsNullOrWhiteSpace(crc) && string.IsNullOrWhiteSpace(sha1)) {
				return Results.BadRequest(new { Message = "Provide at least one hash" });
			}

			var query = db.RomFiles.AsNoTracking();

			if (!string.IsNullOrWhiteSpace(crc)) {
				var normalizedCrc = crc.ToLowerInvariant();
				query = query.Where(f => f.Crc == normalizedCrc);
			}

			if (!string.IsNullOrWhiteSpace(sha1)) {
				var normalizedSha1 = sha1.ToLowerInvariant();
				query = query.Where(f => f.Sha1 == normalizedSha1);
			}

			var files = await query.Take(100).ToListAsync(ct);
			return Results.Ok(files);
		});

		return endpoints;
	}
}
