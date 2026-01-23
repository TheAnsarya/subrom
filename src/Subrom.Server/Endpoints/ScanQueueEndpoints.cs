using Subrom.Application.Interfaces;
using Subrom.Application.Services;

namespace Subrom.Server.Endpoints;

/// <summary>
/// Scan queue management endpoints.
/// </summary>
public static class ScanQueueEndpoints {
	public static IEndpointRouteBuilder MapScanQueueEndpoints(this IEndpointRouteBuilder endpoints) {
		var group = endpoints.MapGroup("/scan-queue")
			.WithTags("Scan Queue");

		// Get all scans in the queue
		group.MapGet("/", (ScanQueueService queueService) => {
			var scans = queueService.GetAllScans();
			return Results.Ok(scans);
		})
		.WithName("GetAllQueuedScans")
		.WithOpenApi(operation => {
			operation.Summary = "Get all queued scans";
			operation.Description = "Returns all scans in the queue with their status.";
			return operation;
		});

		// Get queue statistics
		group.MapGet("/stats", (ScanQueueService queueService) => {
			var stats = queueService.GetStats();
			return Results.Ok(stats);
		})
		.WithName("GetQueueStats")
		.WithOpenApi(operation => {
			operation.Summary = "Get queue statistics";
			operation.Description = "Returns statistics about the scan queue including counts by status.";
			return operation;
		});

		// Get scans by status
		group.MapGet("/by-status/{status}", (QueuedScanStatus status, ScanQueueService queueService) => {
			var scans = queueService.GetScansByStatus(status);
			return Results.Ok(scans);
		})
		.WithName("GetScansByStatus")
		.WithOpenApi(operation => {
			operation.Summary = "Get scans by status";
			operation.Description = "Returns all scans with the specified status.";
			return operation;
		});

		// Get a specific scan
		group.MapGet("/{id:guid}", (Guid id, ScanQueueService queueService) => {
			var scan = queueService.GetScan(id);
			return scan is null ? Results.NotFound() : Results.Ok(scan);
		})
		.WithName("GetQueuedScan")
		.WithOpenApi(operation => {
			operation.Summary = "Get a specific queued scan";
			operation.Description = "Returns a specific scan from the queue by ID.";
			return operation;
		});

		// Enqueue a new scan
		group.MapPost("/", async (
			EnqueueScanRequest request,
			ScanQueueService queueService,
			IDriveRepository driveRepository,
			CancellationToken ct) => {
				var drive = await driveRepository.GetByIdAsync(request.DriveId, ct);
				if (drive is null) {
					return Results.NotFound(new { Message = $"Drive {request.DriveId} not found." });
				}

				var scan = await queueService.EnqueueAsync(drive, request.Priority);
				return Results.Created($"/api/scan-queue/{scan.Id}", scan);
			})
		.WithName("EnqueueScan")
		.WithOpenApi(operation => {
			operation.Summary = "Enqueue a new scan";
			operation.Description = "Adds a new scan to the queue with the specified priority.";
			return operation;
		});

		// Cancel a scan
		group.MapPost("/{id:guid}/cancel", (Guid id, ScanQueueService queueService) => {
			var success = queueService.CancelScan(id);
			return success
				? Results.Ok(new { Message = "Scan cancelled." })
				: Results.BadRequest(new { Message = "Scan cannot be cancelled (already completed, failed, or not found)." });
		})
		.WithName("CancelQueuedScan")
		.WithOpenApi(operation => {
			operation.Summary = "Cancel a queued scan";
			operation.Description = "Cancels a pending or running scan.";
			return operation;
		});

		// Change scan priority
		group.MapPost("/{id:guid}/priority", async (
			Guid id,
			ChangePriorityRequest request,
			ScanQueueService queueService) => {
				var success = await queueService.ChangePriorityAsync(id, request.Priority);
				return success
					? Results.Ok(new { Message = "Priority changed." })
					: Results.BadRequest(new { Message = "Cannot change priority (scan not pending or not found)." });
			})
		.WithName("ChangeScanPriority")
		.WithOpenApi(operation => {
			operation.Summary = "Change scan priority";
			operation.Description = "Changes the priority of a pending scan.";
			return operation;
		});

		// Move scan to front of queue
		group.MapPost("/{id:guid}/move-to-front", async (Guid id, ScanQueueService queueService) => {
			var success = await queueService.MoveToFrontAsync(id);
			return success
				? Results.Ok(new { Message = "Scan moved to front." })
				: Results.BadRequest(new { Message = "Cannot move scan (not pending or not found)." });
		})
		.WithName("MoveScanToFront")
		.WithOpenApi(operation => {
			operation.Summary = "Move scan to front";
			operation.Description = "Moves a pending scan to the front of the queue (high priority).";
			return operation;
		});

		// Move scan to back of queue
		group.MapPost("/{id:guid}/move-to-back", async (Guid id, ScanQueueService queueService) => {
			var success = await queueService.MoveToBackAsync(id);
			return success
				? Results.Ok(new { Message = "Scan moved to back." })
				: Results.BadRequest(new { Message = "Cannot move scan (not pending or not found)." });
		})
		.WithName("MoveScanToBack")
		.WithOpenApi(operation => {
			operation.Summary = "Move scan to back";
			operation.Description = "Moves a pending scan to the back of the queue (low priority).";
			return operation;
		});

		// Pause the entire queue
		group.MapPost("/pause", (ScanQueueService queueService) => {
			queueService.PauseQueue();
			return Results.Ok(new { Message = "Queue paused.", IsPaused = true });
		})
		.WithName("PauseQueue")
		.WithOpenApi(operation => {
			operation.Summary = "Pause the queue";
			operation.Description = "Pauses processing of all queued scans. Running scans will complete but no new scans will start.";
			return operation;
		});

		// Resume the queue
		group.MapPost("/resume", (ScanQueueService queueService) => {
			queueService.ResumeQueue();
			return Results.Ok(new { Message = "Queue resumed.", IsPaused = false });
		})
		.WithName("ResumeQueue")
		.WithOpenApi(operation => {
			operation.Summary = "Resume the queue";
			operation.Description = "Resumes processing of queued scans after a pause.";
			return operation;
		});

		// Clear completed/failed scan history
		group.MapDelete("/history", (ScanQueueService queueService) => {
			queueService.ClearHistory();
			return Results.Ok(new { Message = "History cleared." });
		})
		.WithName("ClearQueueHistory")
		.WithOpenApi(operation => {
			operation.Summary = "Clear queue history";
			operation.Description = "Removes completed, failed, and cancelled scans from the queue history.";
			return operation;
		});

		return endpoints;
	}
}

/// <summary>
/// Request to enqueue a scan.
/// </summary>
public record EnqueueScanRequest {
	public required Guid DriveId { get; init; }
	public QueuedScanPriority Priority { get; init; } = QueuedScanPriority.Normal;
}

/// <summary>
/// Request to change scan priority.
/// </summary>
public record ChangePriorityRequest {
	public required QueuedScanPriority Priority { get; init; }
}
