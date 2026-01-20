using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Subrom.SubromAPI.Data;

namespace Subrom.SubromAPI.Controllers;

/// <summary>
/// API controller for ROM verification operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class VerificationController(SubromDbContext db, ILogger<VerificationController> logger) : ControllerBase {
	/// <summary>Gets verification summary statistics.</summary>
	[HttpGet("summary")]
	public async Task<ActionResult<VerificationSummaryDto>> GetSummary(CancellationToken ct) {
		var totalRomFiles = await db.RomFiles.CountAsync(ct);

		// Count ROM files with at least one DAT match (by SHA1, MD5, or CRC+size)
		var verifiedCount = await db.RomFiles
			.Where(r => db.RomEntries.Any(e =>
				(e.Sha1 != null && e.Sha1 == r.Sha1) ||
				(e.Md5 != null && e.Md5 == r.Md5) ||
				(e.Crc32 != null && e.Crc32 == r.Crc32 && e.Size == r.Size)))
			.CountAsync(ct);

		var unknownCount = totalRomFiles - verifiedCount;

		// Files that haven't been hashed yet
		var neverScannedCount = await db.RomFiles
			.Where(r => r.Sha1 == null && r.Md5 == null && r.Crc32 == null)
			.CountAsync(ct);

		// DAT statistics
		var totalDatGames = await db.Games
			.Where(g => g.DatFile.IsEnabled)
			.CountAsync(ct);

		// Games with at least one ROM file match
		var matchedGames = await db.Games
			.Where(g => g.DatFile.IsEnabled)
			.Where(g => g.Roms.Any(romEntry =>
				db.RomFiles.Any(r =>
					(romEntry.Sha1 != null && romEntry.Sha1 == r.Sha1) ||
					(romEntry.Md5 != null && romEntry.Md5 == r.Md5) ||
					(romEntry.Crc32 != null && romEntry.Crc32 == r.Crc32 && romEntry.Size == r.Size))))
			.CountAsync(ct);

		var missingGames = totalDatGames - matchedGames;

		return Ok(new VerificationSummaryDto(
			totalRomFiles,
			verifiedCount,
			unknownCount,
			neverScannedCount,
			totalDatGames,
			matchedGames,
			missingGames,
			totalRomFiles > 0 ? (double)verifiedCount / totalRomFiles * 100 : 0,
			totalDatGames > 0 ? (double)matchedGames / totalDatGames * 100 : 0
		));
	}

	/// <summary>Verifies a single ROM file against DAT files.</summary>
	[HttpGet("rom/{id:guid}")]
	public async Task<ActionResult<RomVerificationDto>> VerifyRom(Guid id, CancellationToken ct) {
		var romFile = await db.RomFiles.FindAsync([id], ct);
		if (romFile is null) return NotFound();

		var matches = await db.RomEntries
			.Include(e => e.Game)
			.ThenInclude(g => g.DatFile)
			.Where(e => e.Game.DatFile.IsEnabled)
			.Where(e =>
				(romFile.Sha1 != null && e.Sha1 == romFile.Sha1) ||
				(romFile.Md5 != null && e.Md5 == romFile.Md5) ||
				(romFile.Crc32 != null && e.Crc32 == romFile.Crc32 && e.Size == romFile.Size))
			.Select(e => new DatMatchDto(
				e.Game.DatFileId,
				e.Game.DatFile.Name,
				e.GameId,
				e.Game.Name,
				e.Id,
				e.Name,
				DetermineMatchType(romFile.Sha1, romFile.Md5, romFile.Crc32, romFile.Size, e.Sha1, e.Md5, e.Crc32, e.Size)
			))
			.ToListAsync(ct);

		return Ok(new RomVerificationDto(
			romFile.Id,
			romFile.FileName,
			matches.Count > 0 ? "Verified" : "Unknown",
			matches
		));
	}

	/// <summary>Finds ROM files that match a specific DAT game entry.</summary>
	[HttpGet("game/{id:guid}/matches")]
	public async Task<ActionResult<IEnumerable<MatchedRomDto>>> FindMatchesForGame(Guid id, CancellationToken ct) {
		var game = await db.Games
			.Include(g => g.Roms)
			.FirstOrDefaultAsync(g => g.Id == id, ct);

		if (game is null) return NotFound();

		var results = new List<MatchedRomDto>();

		foreach (var romEntry in game.Roms) {
			// Try SHA1 first
			if (!string.IsNullOrEmpty(romEntry.Sha1)) {
				var sha1Match = await db.RomFiles.FirstOrDefaultAsync(r => r.Sha1 == romEntry.Sha1, ct);
				if (sha1Match is not null) {
					results.Add(new MatchedRomDto(sha1Match.Id, sha1Match.Path, "SHA1"));
					continue;
				}
			}

			// Try MD5
			if (!string.IsNullOrEmpty(romEntry.Md5)) {
				var md5Match = await db.RomFiles.FirstOrDefaultAsync(r => r.Md5 == romEntry.Md5, ct);
				if (md5Match is not null) {
					results.Add(new MatchedRomDto(md5Match.Id, md5Match.Path, "MD5"));
					continue;
				}
			}

			// Try CRC32 + size
			if (!string.IsNullOrEmpty(romEntry.Crc32)) {
				var crcMatch = await db.RomFiles
					.FirstOrDefaultAsync(r => r.Crc32 == romEntry.Crc32 && r.Size == romEntry.Size, ct);
				if (crcMatch is not null) {
					results.Add(new MatchedRomDto(crcMatch.Id, crcMatch.Path, "CRC+Size"));
				}
			}
		}

		return Ok(results);
	}

	/// <summary>Gets unknown (unverified) ROM files.</summary>
	[HttpGet("unknown")]
	public async Task<ActionResult<PagedResult<UnknownRomDto>>> GetUnknownRoms(
		[FromQuery] int page = 1,
		[FromQuery] int pageSize = 50,
		CancellationToken ct = default
	) {
		var query = db.RomFiles
			.Where(r => !db.RomEntries.Any(e =>
				(e.Sha1 != null && e.Sha1 == r.Sha1) ||
				(e.Md5 != null && e.Md5 == r.Md5) ||
				(e.Crc32 != null && e.Crc32 == r.Crc32 && e.Size == r.Size)));

		var totalCount = await query.CountAsync(ct);

		var roms = await query
			.OrderBy(r => r.FileName)
			.Skip((page - 1) * pageSize)
			.Take(pageSize)
			.Select(r => new UnknownRomDto(r.Id, r.FileName, r.Path, r.Size, r.Crc32, r.Sha1))
			.ToListAsync(ct);

		return Ok(new PagedResult<UnknownRomDto>(roms, totalCount, page, pageSize));
	}

	/// <summary>Gets missing games (in DAT but not in collection).</summary>
	[HttpGet("missing")]
	public async Task<ActionResult<PagedResult<MissingGameDto>>> GetMissingGames(
		[FromQuery] Guid? datId = null,
		[FromQuery] int page = 1,
		[FromQuery] int pageSize = 50,
		CancellationToken ct = default
	) {
		var query = db.Games
			.Include(g => g.DatFile)
			.Where(g => g.DatFile.IsEnabled)
			.Where(g => !g.Roms.Any(romEntry =>
				db.RomFiles.Any(r =>
					(romEntry.Sha1 != null && romEntry.Sha1 == r.Sha1) ||
					(romEntry.Md5 != null && romEntry.Md5 == r.Md5) ||
					(romEntry.Crc32 != null && romEntry.Crc32 == r.Crc32 && romEntry.Size == r.Size))));

		if (datId.HasValue)
			query = query.Where(g => g.DatFileId == datId.Value);

		var totalCount = await query.CountAsync(ct);

		var games = await query
			.OrderBy(g => g.DatFile.Name)
			.ThenBy(g => g.Name)
			.Skip((page - 1) * pageSize)
			.Take(pageSize)
			.Select(g => new MissingGameDto(
				g.Id,
				g.Name,
				g.Description,
				g.DatFileId,
				g.DatFile.Name,
				g.Roms.Count
			))
			.ToListAsync(ct);

		return Ok(new PagedResult<MissingGameDto>(games, totalCount, page, pageSize));
	}

	private static string DetermineMatchType(
		string? romSha1, string? romMd5, string? romCrc32, long romSize,
		string? entrySha1, string? entryMd5, string? entryCrc32, long entrySize
	) {
		if (romSha1 != null && entrySha1 == romSha1) return "SHA1";
		if (romMd5 != null && entryMd5 == romMd5) return "MD5";
		if (romCrc32 != null && entryCrc32 == romCrc32 && romSize == entrySize) return "CRC+Size";
		return "Unknown";
	}
}

// DTOs
public sealed record VerificationSummaryDto(
	int TotalRomFiles,
	int VerifiedCount,
	int UnknownCount,
	int NeverScannedCount,
	int TotalDatGames,
	int MatchedGames,
	int MissingGames,
	double VerificationRate,
	double CompletionRate
);

public sealed record RomVerificationDto(
	Guid RomFileId,
	string FileName,
	string Status,
	IReadOnlyList<DatMatchDto> Matches
);

public sealed record DatMatchDto(
	Guid DatFileId,
	string DatFileName,
	Guid GameId,
	string GameName,
	Guid RomEntryId,
	string RomEntryName,
	string MatchType
);

public sealed record MatchedRomDto(Guid RomFileId, string FilePath, string MatchType);

public sealed record UnknownRomDto(Guid Id, string FileName, string FilePath, long Size, string? Crc32, string? Sha1);

public sealed record MissingGameDto(
	Guid Id,
	string Name,
	string? Description,
	Guid DatFileId,
	string DatFileName,
	int RomCount
);
