using Microsoft.EntityFrameworkCore;
using Subrom.Application.Services;
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
		group.MapGet("/", async (DriveService driveService, CancellationToken ct) => {
			var drives = await driveService.GetAllAsync(ct);
			return Results.Ok(drives);
		});

		// Get drive by ID
		group.MapGet("/{id:guid}", async (Guid id, DriveService driveService, CancellationToken ct) => {
			var drive = await driveService.GetByIdAsync(id, ct);
			return drive is null
				? Results.NotFound()
				: Results.Ok(drive);
		});

		// Register new drive
		group.MapPost("/", async (RegisterDriveRequest request, DriveService driveService, CancellationToken ct) => {
			try {
				var driveInfo = new DriveInfo(Path.GetPathRoot(request.RootPath) ?? request.RootPath);
				var driveType = ConvertDriveType(driveInfo.DriveType);

				var drive = await driveService.RegisterAsync(
					request.Label,
					request.RootPath,
					driveType,
					ct);

				return Results.Created($"/api/drives/{drive.Id}", drive);
			} catch (DirectoryNotFoundException ex) {
				return Results.BadRequest(new { Message = ex.Message });
			}
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
		group.MapDelete("/{id:guid}", async (Guid id, DriveService driveService, CancellationToken ct) => {
			try {
				await driveService.DeleteAsync(id, ct);
				return Results.NoContent();
			} catch (KeyNotFoundException) {
				return Results.NotFound();
			}
		});

		// Refresh drive status
		group.MapPost("/{id:guid}/refresh", async (Guid id, DriveService driveService, CancellationToken ct) => {
			try {
				var drive = await driveService.RefreshStatusAsync(id, ct);
				return Results.Ok(drive);
			} catch (KeyNotFoundException) {
				return Results.NotFound();
			}
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
