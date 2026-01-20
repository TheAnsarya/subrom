using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Subrom.SubromAPI.Data;

namespace Subrom.SubromAPI.Controllers;

/// <summary>
/// API controller for managing storage drives.
/// CRITICAL: ROMs are NEVER deleted when drives go offline.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class DrivesController(SubromDbContext db, ILogger<DrivesController> logger) : ControllerBase {
	/// <summary>Gets all registered drives.</summary>
	[HttpGet]
	public async Task<ActionResult<IEnumerable<DriveDto>>> GetDrives(CancellationToken ct) {
		var drives = await db.Drives
			.Select(d => new DriveDto(
				d.Id,
				d.Label,
				d.Path,
				d.IsOnline,
				d.LastSeen,
				d.LastScanned,
				d.TotalCapacity,
				d.FreeSpace,
				d.RomCount,
				d.IsEnabled
			))
			.ToListAsync(ct);

		return Ok(drives);
	}

	/// <summary>Gets a drive by ID.</summary>
	[HttpGet("{id:guid}")]
	public async Task<ActionResult<DriveDto>> GetDrive(Guid id, CancellationToken ct) {
		var drive = await db.Drives.FindAsync([id], ct);
		if (drive is null) return NotFound();

		return Ok(new DriveDto(
			drive.Id,
			drive.Label,
			drive.Path,
			drive.IsOnline,
			drive.LastSeen,
			drive.LastScanned,
			drive.TotalCapacity,
			drive.FreeSpace,
			drive.RomCount,
			drive.IsEnabled
		));
	}

	/// <summary>Registers a new drive.</summary>
	[HttpPost]
	public async Task<ActionResult<DriveDto>> CreateDrive(CreateDriveRequest request, CancellationToken ct) {
		var drive = new DriveEntity {
			Id = Guid.CreateVersion7(),
			Label = request.Label,
			Path = request.Path,
			VolumeId = request.VolumeId ?? "",
			RegisteredAt = DateTime.UtcNow,
			LastSeen = DateTime.UtcNow,
		};

		db.Drives.Add(drive);
		await db.SaveChangesAsync(ct);

		logger.LogInformation("Registered new drive {Label} at {Path}", drive.Label, drive.Path);

		return CreatedAtAction(
			nameof(GetDrive),
			new { id = drive.Id },
			new DriveDto(drive.Id, drive.Label, drive.Path, drive.IsOnline, drive.LastSeen, drive.LastScanned, drive.TotalCapacity, drive.FreeSpace, drive.RomCount, drive.IsEnabled)
		);
	}

	/// <summary>Updates a drive's settings.</summary>
	[HttpPut("{id:guid}")]
	public async Task<ActionResult> UpdateDrive(Guid id, UpdateDriveRequest request, CancellationToken ct) {
		var drive = await db.Drives.FindAsync([id], ct);
		if (drive is null) return NotFound();

		if (request.Label is not null) drive.Label = request.Label;
		if (request.IsEnabled.HasValue) drive.IsEnabled = request.IsEnabled.Value;

		await db.SaveChangesAsync(ct);
		return NoContent();
	}

	/// <summary>Refreshes drive status (online/offline check).</summary>
	[HttpPost("{id:guid}/refresh")]
	public async Task<ActionResult<DriveDto>> RefreshDrive(Guid id, CancellationToken ct) {
		var drive = await db.Drives.FindAsync([id], ct);
		if (drive is null) return NotFound();

		var isOnline = Directory.Exists(drive.Path);
		drive.IsOnline = isOnline;

		if (isOnline) {
			drive.LastSeen = DateTime.UtcNow;
			try {
				var driveInfo = new DriveInfo(Path.GetPathRoot(drive.Path)!);
				drive.TotalCapacity = driveInfo.TotalSize;
				drive.FreeSpace = driveInfo.AvailableFreeSpace;
			}
			catch {
				// Ignore drive info errors
			}
		}

		await db.SaveChangesAsync(ct);

		return Ok(new DriveDto(drive.Id, drive.Label, drive.Path, drive.IsOnline, drive.LastSeen, drive.LastScanned, drive.TotalCapacity, drive.FreeSpace, drive.RomCount, drive.IsEnabled));
	}

	/// <summary>Deletes a drive registration. ROMs are NOT deleted, only marked offline.</summary>
	[HttpDelete("{id:guid}")]
	public async Task<ActionResult> DeleteDrive(Guid id, CancellationToken ct) {
		var drive = await db.Drives.FindAsync([id], ct);
		if (drive is null) return NotFound();

		// Mark all ROMs as offline instead of deleting
		await db.RomFiles
			.Where(r => r.DriveId == id)
			.ExecuteUpdateAsync(s => s.SetProperty(r => r.IsOnline, false), ct);

		db.Drives.Remove(drive);
		await db.SaveChangesAsync(ct);

		logger.LogInformation("Removed drive {Label}, ROMs marked offline", drive.Label);

		return NoContent();
	}
}

public sealed record DriveDto(
	Guid Id,
	string Label,
	string Path,
	bool IsOnline,
	DateTime LastSeen,
	DateTime? LastScanned,
	long TotalCapacity,
	long FreeSpace,
	int RomCount,
	bool IsEnabled
);

public sealed record CreateDriveRequest(string Label, string Path, string? VolumeId = null);
public sealed record UpdateDriveRequest(string? Label = null, bool? IsEnabled = null);
