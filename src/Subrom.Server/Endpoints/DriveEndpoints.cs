using Microsoft.EntityFrameworkCore;
using Subrom.Domain.Aggregates.Storage;
using Subrom.Infrastructure.Persistence;

namespace Subrom.Server.Endpoints;

/// <summary>
/// Drive management endpoints.
/// </summary>
public static class DriveEndpoints {
	public static IEndpointRouteBuilder MapDriveEndpoints(this IEndpointRouteBuilder endpoints) {
		var group = endpoints.MapGroup("/drives")
			.WithTags("Drives");

		// Get all drives
		group.MapGet("/", async (SubromDbContext db, CancellationToken ct) => {
			var drives = await db.Drives
				.AsNoTracking()
				.OrderBy(d => d.Label)
				.ToListAsync(ct);

			return Results.Ok(drives);
		});

		// Get drive by ID
		group.MapGet("/{id:guid}", async (Guid id, SubromDbContext db, CancellationToken ct) => {
			var drive = await db.Drives
				.AsNoTracking()
				.FirstOrDefaultAsync(d => d.Id == id, ct);

			return drive is null
				? Results.NotFound()
				: Results.Ok(drive);
		});

		// Register new drive
		group.MapPost("/", async (RegisterDriveRequest request, SubromDbContext db, CancellationToken ct) => {
			// Check if path already registered
			var existing = await db.Drives
				.AnyAsync(d => d.RootPath == request.RootPath, ct);

			if (existing) {
				return Results.Conflict(new { Message = "Drive path already registered" });
			}

			// Validate path exists
			if (!Directory.Exists(request.RootPath)) {
				return Results.BadRequest(new { Message = "Path does not exist" });
			}

			var driveInfo = new DriveInfo(Path.GetPathRoot(request.RootPath) ?? request.RootPath);
			var drive = Drive.Create(
				request.Label,
				request.RootPath,
				ConvertDriveType(driveInfo.DriveType));

			drive.VolumeLabel = driveInfo.IsReady ? driveInfo.VolumeLabel : null;
			drive.TotalSize = driveInfo.IsReady ? driveInfo.TotalSize : null;
			drive.FreeSpace = driveInfo.IsReady ? driveInfo.AvailableFreeSpace : null;

			db.Drives.Add(drive);
			await db.SaveChangesAsync(ct);

			return Results.Created($"/api/drives/{drive.Id}", drive);
		});

		// Update drive
		group.MapPut("/{id:guid}", async (Guid id, UpdateDriveRequest request, SubromDbContext db, CancellationToken ct) => {
			var drive = await db.Drives.FindAsync([id], ct);
			if (drive is null) {
				return Results.NotFound();
			}

			drive.Label = request.Label;
			drive.AutoScan = request.AutoScan;

			await db.SaveChangesAsync(ct);
			return Results.Ok(drive);
		});

		// Delete drive
		group.MapDelete("/{id:guid}", async (Guid id, SubromDbContext db, CancellationToken ct) => {
			var drive = await db.Drives.FindAsync([id], ct);
			if (drive is null) {
				return Results.NotFound();
			}

			db.Drives.Remove(drive);
			await db.SaveChangesAsync(ct);

			return Results.NoContent();
		});

		// Refresh drive status
		group.MapPost("/{id:guid}/refresh", async (Guid id, SubromDbContext db, CancellationToken ct) => {
			var drive = await db.Drives.FindAsync([id], ct);
			if (drive is null) {
				return Results.NotFound();
			}

			if (Directory.Exists(drive.RootPath)) {
				var driveInfo = new DriveInfo(Path.GetPathRoot(drive.RootPath) ?? drive.RootPath);
				if (driveInfo.IsReady) {
					drive.MarkOnline(driveInfo.TotalSize, driveInfo.AvailableFreeSpace);
					drive.VolumeLabel = driveInfo.VolumeLabel;
				} else {
					drive.MarkOffline();
				}
			} else {
				drive.MarkOffline();
			}

			await db.SaveChangesAsync(ct);
			return Results.Ok(drive);
		});

		// Get drive statistics
		group.MapGet("/{id:guid}/stats", async (Guid id, SubromDbContext db, CancellationToken ct) => {
			var drive = await db.Drives
				.AsNoTracking()
				.FirstOrDefaultAsync(d => d.Id == id, ct);

			if (drive is null) {
				return Results.NotFound();
			}

			var fileCount = await db.RomFiles.CountAsync(f => f.DriveId == id, ct);
			var totalSize = await db.RomFiles
				.Where(f => f.DriveId == id)
				.SumAsync(f => f.Size, ct);
			var verifiedCount = await db.RomFiles
				.CountAsync(f => f.DriveId == id && f.VerificationStatus == VerificationStatus.Verified, ct);

			return Results.Ok(new {
				DriveId = id,
				FileCount = fileCount,
				TotalSize = totalSize,
				VerifiedCount = verifiedCount,
				UnverifiedCount = fileCount - verifiedCount,
				VerificationPercentage = fileCount > 0 ? (double)verifiedCount / fileCount * 100 : 0
			});
		});

		return endpoints;
	}

	private static Domain.Aggregates.Storage.DriveType ConvertDriveType(System.IO.DriveType driveType) {
		return driveType switch {
			System.IO.DriveType.Fixed => Domain.Aggregates.Storage.DriveType.Fixed,
			System.IO.DriveType.Removable => Domain.Aggregates.Storage.DriveType.Removable,
			System.IO.DriveType.Network => Domain.Aggregates.Storage.DriveType.Network,
			System.IO.DriveType.CDRom => Domain.Aggregates.Storage.DriveType.Optical,
			_ => Domain.Aggregates.Storage.DriveType.Unknown
		};
	}
}

public record RegisterDriveRequest(string Label, string RootPath);
public record UpdateDriveRequest(string Label, bool AutoScan);
