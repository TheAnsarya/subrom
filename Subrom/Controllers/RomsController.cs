using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Subrom.SubromAPI.Data;

namespace Subrom.SubromAPI.Controllers;

/// <summary>
/// API controller for managing ROM files.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class RomsController(SubromDbContext db) : ControllerBase {
	/// <summary>Gets ROM files with optional filtering.</summary>
	[HttpGet]
	public async Task<ActionResult<PagedResult<RomFileDto>>> GetRoms(
		[FromQuery] Guid? driveId = null,
		[FromQuery] string? search = null,
		[FromQuery] bool? online = null,
		[FromQuery] bool? verified = null,
		[FromQuery] int page = 1,
		[FromQuery] int pageSize = 50,
		CancellationToken ct = default
	) {
		var query = db.RomFiles.AsQueryable();

		if (driveId.HasValue)
			query = query.Where(r => r.DriveId == driveId.Value);

		if (!string.IsNullOrEmpty(search))
			query = query.Where(r => r.FileName.Contains(search) || r.Path.Contains(search));

		if (online.HasValue)
			query = query.Where(r => r.IsOnline == online.Value);

		if (verified.HasValue)
			query = query.Where(r => verified.Value ? r.VerifiedAt != null : r.VerifiedAt == null);

		var totalCount = await query.CountAsync(ct);

		var roms = await query
			.OrderBy(r => r.FileName)
			.Skip((page - 1) * pageSize)
			.Take(pageSize)
			.Select(r => new RomFileDto(
				r.Id,
				r.DriveId,
				r.FileName,
				r.Path,
				r.Size,
				r.Crc32,
				r.Md5,
				r.Sha1,
				r.IsOnline,
				r.VerifiedAt,
				r.ModifiedAt
			))
			.ToListAsync(ct);

		return Ok(new PagedResult<RomFileDto>(roms, totalCount, page, pageSize));
	}

	/// <summary>Gets a ROM file by ID.</summary>
	[HttpGet("{id:guid}")]
	public async Task<ActionResult<RomFileDetailDto>> GetRom(Guid id, CancellationToken ct) {
		var rom = await db.RomFiles
			.Include(r => r.Drive)
			.FirstOrDefaultAsync(r => r.Id == id, ct);

		if (rom is null) return NotFound();

		return Ok(new RomFileDetailDto(
			rom.Id,
			rom.DriveId,
			rom.Drive.Label,
			rom.FileName,
			rom.Path,
			rom.Size,
			rom.Crc32,
			rom.Md5,
			rom.Sha1,
			rom.IsOnline,
			rom.IsInArchive,
			rom.ArchivePath,
			rom.PathInArchive,
			rom.VerifiedAt,
			rom.ModifiedAt,
			rom.CreatedAt,
			rom.UpdatedAt
		));
	}

	/// <summary>Gets ROM statistics.</summary>
	[HttpGet("stats")]
	public async Task<ActionResult<RomStatsDto>> GetStats(CancellationToken ct) {
		var totalRoms = await db.RomFiles.CountAsync(ct);
		var onlineRoms = await db.RomFiles.CountAsync(r => r.IsOnline, ct);
		var verifiedRoms = await db.RomFiles.CountAsync(r => r.VerifiedAt != null, ct);
		var totalSize = await db.RomFiles.SumAsync(r => r.Size, ct);

		return Ok(new RomStatsDto(totalRoms, onlineRoms, verifiedRoms, totalSize));
	}

	/// <summary>Gets ROMs by hash (SHA1, MD5, or CRC32).</summary>
	[HttpGet("by-hash")]
	public async Task<ActionResult<IEnumerable<RomFileDto>>> GetByHash(
		[FromQuery] string? sha1 = null,
		[FromQuery] string? md5 = null,
		[FromQuery] string? crc32 = null,
		CancellationToken ct = default
	) {
		if (string.IsNullOrEmpty(sha1) && string.IsNullOrEmpty(md5) && string.IsNullOrEmpty(crc32))
			return BadRequest("At least one hash must be provided");

		var query = db.RomFiles.AsQueryable();

		if (!string.IsNullOrEmpty(sha1))
			query = query.Where(r => r.Sha1 == sha1.ToLowerInvariant());
		else if (!string.IsNullOrEmpty(md5))
			query = query.Where(r => r.Md5 == md5.ToLowerInvariant());
		else if (!string.IsNullOrEmpty(crc32))
			query = query.Where(r => r.Crc32 == crc32.ToLowerInvariant());

		var roms = await query
			.Select(r => new RomFileDto(
				r.Id,
				r.DriveId,
				r.FileName,
				r.Path,
				r.Size,
				r.Crc32,
				r.Md5,
				r.Sha1,
				r.IsOnline,
				r.VerifiedAt,
				r.ModifiedAt
			))
			.ToListAsync(ct);

		return Ok(roms);
	}

	/// <summary>Deletes a ROM file record. File is NOT deleted from disk.</summary>
	[HttpDelete("{id:guid}")]
	public async Task<ActionResult> DeleteRom(Guid id, CancellationToken ct) {
		var rom = await db.RomFiles.FindAsync([id], ct);
		if (rom is null) return NotFound();

		db.RomFiles.Remove(rom);
		await db.SaveChangesAsync(ct);

		return NoContent();
	}
}

public sealed record RomFileDto(
	Guid Id,
	Guid DriveId,
	string FileName,
	string Path,
	long Size,
	string? Crc32,
	string? Md5,
	string? Sha1,
	bool IsOnline,
	DateTime? VerifiedAt,
	DateTime ModifiedAt
);

public sealed record RomFileDetailDto(
	Guid Id,
	Guid DriveId,
	string DriveLabel,
	string FileName,
	string Path,
	long Size,
	string? Crc32,
	string? Md5,
	string? Sha1,
	bool IsOnline,
	bool IsInArchive,
	string? ArchivePath,
	string? PathInArchive,
	DateTime? VerifiedAt,
	DateTime ModifiedAt,
	DateTime CreatedAt,
	DateTime UpdatedAt
);

public sealed record RomStatsDto(int TotalRoms, int OnlineRoms, int VerifiedRoms, long TotalSize);
