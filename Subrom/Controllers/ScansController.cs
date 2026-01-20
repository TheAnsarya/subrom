using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Subrom.Domain.Storage;
using Subrom.SubromAPI.Data;

namespace Subrom.SubromAPI.Controllers;

/// <summary>
/// API controller for managing scan jobs.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class ScansController(SubromDbContext db, ILogger<ScansController> logger) : ControllerBase {
	/// <summary>Gets all scan jobs.</summary>
	[HttpGet]
	public async Task<ActionResult<IEnumerable<ScanJobDto>>> GetScans(
		[FromQuery] ScanJobStatus? status = null,
		[FromQuery] int limit = 20,
		CancellationToken ct = default
	) {
		var query = db.ScanJobs.AsQueryable();

		if (status.HasValue)
			query = query.Where(s => s.Status == status.Value);

		var scans = await query
			.OrderByDescending(s => s.CreatedAt)
			.Take(limit)
			.Select(s => new ScanJobDto(
				s.Id,
				s.DriveId,
				s.RootPath,
				s.Status,
				s.StartedAt,
				s.CompletedAt,
				s.TotalFiles,
				s.ProcessedFiles,
				s.VerifiedFiles,
				s.UnknownFiles,
				s.ErrorFiles,
				s.CurrentFile
			))
			.ToListAsync(ct);

		return Ok(scans);
	}

	/// <summary>Gets a scan job by ID.</summary>
	[HttpGet("{id:guid}")]
	public async Task<ActionResult<ScanJobDetailDto>> GetScan(Guid id, CancellationToken ct) {
		var scan = await db.ScanJobs.FindAsync([id], ct);
		if (scan is null) return NotFound();

		return Ok(new ScanJobDetailDto(
			scan.Id,
			scan.DriveId,
			scan.RootPath,
			scan.Status,
			scan.StartedAt,
			scan.CompletedAt,
			scan.TotalFiles,
			scan.ProcessedFiles,
			scan.VerifiedFiles,
			scan.UnknownFiles,
			scan.ErrorFiles,
			scan.CurrentFile,
			scan.ErrorMessage,
			scan.CreatedAt,
			scan.Recursive,
			scan.VerifyHashes
		));
	}

	/// <summary>Creates a new scan job.</summary>
	[HttpPost]
	public async Task<ActionResult<ScanJobDto>> CreateScan(CreateScanRequest request, CancellationToken ct) {
		// Validate path exists
		if (!Directory.Exists(request.RootPath))
			return BadRequest($"Path does not exist: {request.RootPath}");

		var scan = new ScanJobEntity {
			Id = Guid.CreateVersion7(),
			DriveId = request.DriveId,
			RootPath = request.RootPath,
			Status = ScanJobStatus.Pending,
			CreatedAt = DateTime.UtcNow,
			Recursive = request.Recursive ?? true,
			VerifyHashes = request.VerifyHashes ?? true,
		};

		db.ScanJobs.Add(scan);
		await db.SaveChangesAsync(ct);

		logger.LogInformation("Created scan job {Id} for path {Path}", scan.Id, scan.RootPath);

		return CreatedAtAction(
			nameof(GetScan),
			new { id = scan.Id },
			new ScanJobDto(scan.Id, scan.DriveId, scan.RootPath, scan.Status, scan.StartedAt, scan.CompletedAt, scan.TotalFiles, scan.ProcessedFiles, scan.VerifiedFiles, scan.UnknownFiles, scan.ErrorFiles, scan.CurrentFile)
		);
	}

	/// <summary>Cancels a running scan job.</summary>
	[HttpPost("{id:guid}/cancel")]
	public async Task<ActionResult> CancelScan(Guid id, CancellationToken ct) {
		var scan = await db.ScanJobs.FindAsync([id], ct);
		if (scan is null) return NotFound();

		if (scan.Status is not (ScanJobStatus.Pending or ScanJobStatus.Running))
			return BadRequest("Scan cannot be cancelled");

		scan.Status = ScanJobStatus.Cancelled;
		scan.CompletedAt = DateTime.UtcNow;
		await db.SaveChangesAsync(ct);

		logger.LogInformation("Cancelled scan job {Id}", scan.Id);

		return NoContent();
	}

	/// <summary>Deletes a scan job record.</summary>
	[HttpDelete("{id:guid}")]
	public async Task<ActionResult> DeleteScan(Guid id, CancellationToken ct) {
		var scan = await db.ScanJobs.FindAsync([id], ct);
		if (scan is null) return NotFound();

		if (scan.Status == ScanJobStatus.Running)
			return BadRequest("Cannot delete a running scan");

		db.ScanJobs.Remove(scan);
		await db.SaveChangesAsync(ct);

		return NoContent();
	}
}

public sealed record ScanJobDto(
	Guid Id,
	Guid? DriveId,
	string RootPath,
	ScanJobStatus Status,
	DateTime? StartedAt,
	DateTime? CompletedAt,
	int TotalFiles,
	int ProcessedFiles,
	int VerifiedFiles,
	int UnknownFiles,
	int ErrorFiles,
	string? CurrentFile
) {
	public double Progress => TotalFiles > 0 ? (double)ProcessedFiles / TotalFiles * 100 : 0;
}

public sealed record ScanJobDetailDto(
	Guid Id,
	Guid? DriveId,
	string RootPath,
	ScanJobStatus Status,
	DateTime? StartedAt,
	DateTime? CompletedAt,
	int TotalFiles,
	int ProcessedFiles,
	int VerifiedFiles,
	int UnknownFiles,
	int ErrorFiles,
	string? CurrentFile,
	string? ErrorMessage,
	DateTime CreatedAt,
	bool Recursive,
	bool VerifyHashes
);

public sealed record CreateScanRequest(
	string RootPath,
	Guid? DriveId = null,
	bool? Recursive = true,
	bool? VerifyHashes = true
);
