using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Subrom.Domain.Aggregates.DatFiles;
using Subrom.Domain.Aggregates.Storage;
using Subrom.Infrastructure.Persistence;

namespace Subrom.Server.Endpoints;

/// <summary>
/// Streaming and cursor-based endpoints for large datasets (60K+ entries).
/// </summary>
public static class StreamingEndpoints {
	public static IEndpointRouteBuilder MapStreamingEndpoints(this IEndpointRouteBuilder endpoints) {
		var group = endpoints.MapGroup("/api/stream")
			.WithTags("Streaming");

		// Stream ROM files with cursor pagination
		group.MapGet("/romfiles", StreamRomFiles);

		// Stream games from a DAT file with cursor pagination
		group.MapGet("/datfiles/{datFileId:guid}/games", StreamDatGames);

		// Stream ROMs from a game (int ID)
		group.MapGet("/games/{gameId:int}/roms", StreamGameRoms);

		// Keyset pagination for ROM files (more efficient than offset)
		group.MapGet("/romfiles/keyset", GetRomFilesKeysetPaginated);

		// Keyset pagination for games
		group.MapGet("/games/keyset", GetGamesKeysetPaginated);

		return endpoints;
	}

	/// <summary>
	/// Streams ROM files with cursor-based pagination, ideal for large datasets.
	/// </summary>
	private static async IAsyncEnumerable<RomFileDto> StreamRomFiles(
		[FromServices] SubromDbContext db,
		[FromQuery] Guid? driveId = null,
		[FromQuery] VerificationStatus? status = null,
		[FromQuery] string? cursor = null,
		[FromQuery] int batchSize = 100,
		[EnumeratorCancellation] CancellationToken ct = default) {
		batchSize = Math.Clamp(batchSize, 10, 1000);

		// Decode cursor (Id > cursor for keyset pagination)
		Guid? cursorId = null;
		if (!string.IsNullOrEmpty(cursor) && Guid.TryParse(cursor, out var parsed)) {
			cursorId = parsed;
		}

		var query = db.RomFiles.AsNoTracking();

		if (driveId.HasValue) {
			query = query.Where(f => f.DriveId == driveId.Value);
		}

		if (status.HasValue) {
			query = query.Where(f => f.VerificationStatus == status.Value);
		}

		// Keyset/cursor-based pagination
		if (cursorId.HasValue) {
			query = query.Where(f => f.Id.CompareTo(cursorId.Value) > 0);
		}

		// Stream batches
		var files = query
			.OrderBy(f => f.Id)
			.Select(f => new RomFileDto(
				f.Id,
				f.FileName,
				f.RelativePath,
				f.Size,
				f.DriveId,
				f.VerificationStatus,
				f.Crc,
				f.ScannedAt
			))
			.AsAsyncEnumerable();

		await foreach (var file in files.WithCancellation(ct)) {
			yield return file;
		}
	}

	/// <summary>
	/// Streams games from a DAT file.
	/// </summary>
	private static async IAsyncEnumerable<GameDto> StreamDatGames(
		[FromServices] SubromDbContext db,
		Guid datFileId,
		[FromQuery] string? cursor = null,
		[EnumeratorCancellation] CancellationToken ct = default) {
		// GameEntry.Id is int, so parse cursor as int
		int? cursorId = null;
		if (!string.IsNullOrEmpty(cursor) && int.TryParse(cursor, out var parsed)) {
			cursorId = parsed;
		}

		var query = db.Games
			.AsNoTracking()
			.Where(g => g.DatFileId == datFileId);

		if (cursorId.HasValue) {
			query = query.Where(g => g.Id > cursorId.Value);
		}

		var games = query
			.OrderBy(g => g.Id)
			.Select(g => new GameDto(
				g.Id,
				g.Name,
				g.Description,
				g.Year,
				g.Publisher,
				g.Region,
				g.CloneOf,
				g.Roms.Count
			))
			.AsAsyncEnumerable();

		await foreach (var game in games.WithCancellation(ct)) {
			yield return game;
		}
	}

	/// <summary>
	/// Streams ROMs from a game.
	/// </summary>
	private static async IAsyncEnumerable<RomEntryDto> StreamGameRoms(
		[FromServices] SubromDbContext db,
		int gameId,
		[EnumeratorCancellation] CancellationToken ct = default) {
		var roms = db.Roms
			.AsNoTracking()
			.Where(r => r.GameId == gameId)
			.OrderBy(r => r.Name)
			.Select(r => new RomEntryDto(
				r.Id,
				r.Name,
				r.Size,
				r.Crc,
				r.Md5,
				r.Sha1,
				r.Status
			))
			.AsAsyncEnumerable();

		await foreach (var rom in roms.WithCancellation(ct)) {
			yield return rom;
		}
	}

	/// <summary>
	/// Keyset pagination for ROM files (more efficient than offset for large datasets).
	/// </summary>
	private static async Task<IResult> GetRomFilesKeysetPaginated(
		[FromServices] SubromDbContext db,
		[FromQuery] Guid? afterId = null,
		[FromQuery] int limit = 100,
		[FromQuery] Guid? driveId = null,
		[FromQuery] VerificationStatus? status = null,
		[FromQuery] string? search = null,
		CancellationToken ct = default) {
		limit = Math.Clamp(limit, 1, 1000);

		var query = db.RomFiles.AsNoTracking();

		if (driveId.HasValue) {
			query = query.Where(f => f.DriveId == driveId.Value);
		}

		if (status.HasValue) {
			query = query.Where(f => f.VerificationStatus == status.Value);
		}

		if (!string.IsNullOrWhiteSpace(search)) {
			query = query.Where(f => EF.Functions.Like(f.FileName, $"%{search}%"));
		}

		// Keyset pagination: filter by afterId
		if (afterId.HasValue) {
			query = query.Where(f => f.Id.CompareTo(afterId.Value) > 0);
		}

		// Fetch one extra to determine if there's more
		var items = await query
			.OrderBy(f => f.Id)
			.Take(limit + 1)
			.Select(f => new RomFileDto(
				f.Id,
				f.FileName,
				f.RelativePath,
				f.Size,
				f.DriveId,
				f.VerificationStatus,
				f.Crc,
				f.ScannedAt
			))
			.ToListAsync(ct);

		var hasMore = items.Count > limit;
		if (hasMore) items = items.Take(limit).ToList();

		var nextCursor = hasMore && items.Count > 0 ? items[^1].Id : (Guid?)null;

		return Results.Ok(new RomFileKeysetPage(
			Items: items,
			NextCursor: nextCursor,
			HasMore: hasMore,
			PageSize: items.Count
		));
	}

	/// <summary>
	/// Keyset pagination for games.
	/// </summary>
	private static async Task<IResult> GetGamesKeysetPaginated(
		[FromServices] SubromDbContext db,
		[FromQuery] Guid? datFileId = null,
		[FromQuery] int? afterId = null,
		[FromQuery] int limit = 100,
		[FromQuery] string? search = null,
		CancellationToken ct = default) {
		limit = Math.Clamp(limit, 1, 1000);

		var query = db.Games.AsNoTracking();

		if (datFileId.HasValue) {
			query = query.Where(g => g.DatFileId == datFileId.Value);
		}

		if (!string.IsNullOrWhiteSpace(search)) {
			query = query.Where(g => EF.Functions.Like(g.Name, $"%{search}%"));
		}

		if (afterId.HasValue) {
			query = query.Where(g => g.Id > afterId.Value);
		}

		var items = await query
			.OrderBy(g => g.Id)
			.Take(limit + 1)
			.Select(g => new GameDto(
				g.Id,
				g.Name,
				g.Description,
				g.Year,
				g.Publisher,
				g.Region,
				g.CloneOf,
				g.Roms.Count
			))
			.ToListAsync(ct);

		var hasMore = items.Count > limit;
		if (hasMore) items = items.Take(limit).ToList();

		var nextCursor = hasMore && items.Count > 0 ? items[^1].Id : (int?)null;

		return Results.Ok(new GameKeysetPage(
			Items: items,
			NextCursor: nextCursor,
			HasMore: hasMore,
			PageSize: items.Count
		));
	}
}

// DTOs for streaming
public record RomFileDto(
	Guid Id,
	string FileName,
	string RelativePath,
	long Size,
	Guid? DriveId,
	VerificationStatus Status,
	string? Crc,
	DateTime? ScannedAt);

public record GameDto(
	int Id,
	string Name,
	string? Description,
	string? Year,
	string? Publisher,
	string? Region,
	string? CloneOf,
	int RomCount);

public record RomEntryDto(
	int Id,
	string Name,
	long Size,
	string? Crc,
	string? Md5,
	string? Sha1,
	RomStatus Status);

public record RomFileKeysetPage(
	IReadOnlyList<RomFileDto> Items,
	Guid? NextCursor,
	bool HasMore,
	int PageSize);

public record GameKeysetPage(
	IReadOnlyList<GameDto> Items,
	int? NextCursor,
	bool HasMore,
	int PageSize);
