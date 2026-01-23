using Microsoft.EntityFrameworkCore;
using Subrom.Application.Services;
using Subrom.Domain.Aggregates.Scanning;
using Subrom.Infrastructure.Persistence;
using Subrom.Server.BackgroundServices;

namespace Subrom.Server.Endpoints;

/// <summary>
/// Scan operation endpoints.
/// </summary>
public static class ScanEndpoints {
	public static IEndpointRouteBuilder MapScanEndpoints(this IEndpointRouteBuilder endpoints) {
		var group = endpoints.MapGroup("/scans")
			.WithTags("Scanning");

		// Get all scan jobs
		group.MapGet("/", async (ScanService scanService, CancellationToken ct) => {
			var jobs = await scanService.GetJobsAsync(cancellationToken: ct);
			return Results.Ok(jobs.Take(50));
		});

		// Get active scan jobs
		group.MapGet("/active", async (ScanService scanService, CancellationToken ct) => {
			var jobs = await scanService.GetActiveJobsAsync(ct);
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
		group.MapPost("/", async (
			StartScanRequest request,
			ScanService scanService,
			ScanJobProcessor scanJobProcessor,
			CancellationToken ct) => {
				try {
					var job = await scanService.StartScanAsync(
						request.DriveId ?? Guid.Empty,
						request.Type,
						request.TargetPath,
						ct);

					// Queue the job for background execution
					await scanJobProcessor.EnqueueJobAsync(job.Id);

					return Results.Created($"/api/scans/{job.Id}", job);
				} catch (KeyNotFoundException ex) {
					return Results.NotFound(new { Message = ex.Message });
				} catch (InvalidOperationException ex) {
					return Results.Conflict(new { Message = ex.Message });
				}
			});

		// Cancel a scan
		group.MapPost("/{id:guid}/cancel", async (Guid id, ScanService scanService, CancellationToken ct) => {
			try {
				await scanService.CancelScanAsync(id, ct);
				return Results.Ok(new { Message = "Scan cancellation requested" });
			} catch (KeyNotFoundException) {
				return Results.NotFound();
			}
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
