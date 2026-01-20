using Microsoft.EntityFrameworkCore;
using Subrom.Domain.Aggregates.Scanning;
using Subrom.Infrastructure.Persistence;

namespace Subrom.Server.Endpoints;

/// <summary>
/// Scan operation endpoints.
/// </summary>
public static class ScanEndpoints {
	public static IEndpointRouteBuilder MapScanEndpoints(this IEndpointRouteBuilder endpoints) {
		var group = endpoints.MapGroup("/scans")
			.WithTags("Scanning");

		// Get all scan jobs
		group.MapGet("/", async (SubromDbContext db, CancellationToken ct) => {
			var jobs = await db.ScanJobs
				.AsNoTracking()
				.OrderByDescending(j => j.QueuedAt)
				.Take(50)
				.ToListAsync(ct);

			return Results.Ok(jobs);
		});

		// Get active scan jobs
		group.MapGet("/active", async (SubromDbContext db, CancellationToken ct) => {
			var jobs = await db.ScanJobs
				.AsNoTracking()
				.Where(j => j.Status == ScanStatus.Running || j.Status == ScanStatus.Queued)
				.OrderByDescending(j => j.QueuedAt)
				.ToListAsync(ct);

			return Results.Ok(jobs);
		});

		// Get scan job by ID
		group.MapGet("/{id:guid}", async (Guid id, SubromDbContext db, CancellationToken ct) => {
			var job = await db.ScanJobs
				.AsNoTracking()
				.FirstOrDefaultAsync(j => j.Id == id, ct);

			return job is null
				? Results.NotFound()
				: Results.Ok(job);
		});

		// Start a new scan
		group.MapPost("/", async (StartScanRequest request, SubromDbContext db, CancellationToken ct) => {
			// Check if there's already an active scan for this drive
			var hasActive = await db.ScanJobs
				.AnyAsync(j =>
					j.DriveId == request.DriveId &&
					(j.Status == ScanStatus.Running || j.Status == ScanStatus.Queued), ct);

			if (hasActive) {
				return Results.Conflict(new { Message = "A scan is already running for this drive" });
			}

			var job = ScanJob.Create(
				request.Type,
				request.DriveId,
				request.TargetPath);

			db.ScanJobs.Add(job);
			await db.SaveChangesAsync(ct);

			// TODO: Queue the job for processing

			return Results.Created($"/api/scans/{job.Id}", job);
		});

		// Cancel a scan
		group.MapPost("/{id:guid}/cancel", async (Guid id, SubromDbContext db, CancellationToken ct) => {
			var job = await db.ScanJobs.FindAsync([id], ct);
			if (job is null) {
				return Results.NotFound();
			}

			if (job.Status != ScanStatus.Running && job.Status != ScanStatus.Queued) {
				return Results.BadRequest(new { Message = "Scan is not running" });
			}

			job.Cancel();
			await db.SaveChangesAsync(ct);

			// TODO: Signal cancellation to the processing task

			return Results.Ok(job);
		});

		// Delete old scan jobs
		group.MapDelete("/cleanup", async (SubromDbContext db, CancellationToken ct) => {
			var cutoff = DateTime.UtcNow.AddDays(-7);
			var oldJobs = await db.ScanJobs
				.Where(j => j.CompletedAt < cutoff)
				.ToListAsync(ct);

			db.ScanJobs.RemoveRange(oldJobs);
			var count = await db.SaveChangesAsync(ct);

			return Results.Ok(new { DeletedCount = count });
		});

		return endpoints;
	}
}

public record StartScanRequest(ScanType Type, Guid? DriveId = null, string? TargetPath = null);
