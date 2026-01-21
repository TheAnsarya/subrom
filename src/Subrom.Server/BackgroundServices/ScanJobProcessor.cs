using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR;
using Subrom.Application.Interfaces;
using Subrom.Application.Services;
using Subrom.Domain.Aggregates.Scanning;
using Subrom.Server.Hubs;

namespace Subrom.Server.BackgroundServices;

/// <summary>
/// Background service that processes queued scan jobs.
/// </summary>
public sealed class ScanJobProcessor : BackgroundService {
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly IHubContext<SubromHub> _hubContext;
	private readonly ILogger<ScanJobProcessor> _logger;
	private readonly Channel<Guid> _jobQueue;

	public ScanJobProcessor(
		IServiceScopeFactory scopeFactory,
		IHubContext<SubromHub> hubContext,
		ILogger<ScanJobProcessor> logger) {
		_scopeFactory = scopeFactory;
		_hubContext = hubContext;
		_logger = logger;
		_jobQueue = Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions {
			SingleReader = true,
			SingleWriter = false
		});
	}

	/// <summary>
	/// Enqueues a scan job for background processing.
	/// </summary>
	public async ValueTask EnqueueJobAsync(Guid jobId) {
		await _jobQueue.Writer.WriteAsync(jobId);
		_logger.LogInformation("Enqueued scan job {JobId} for processing", jobId);
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		_logger.LogInformation("Scan job processor started");

		await foreach (var jobId in _jobQueue.Reader.ReadAllAsync(stoppingToken)) {
			try {
				await ProcessJobAsync(jobId, stoppingToken);
			} catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
				break;
			} catch (Exception ex) {
				_logger.LogError(ex, "Error processing scan job {JobId}", jobId);
			}
		}

		_logger.LogInformation("Scan job processor stopped");
	}

	private async Task ProcessJobAsync(Guid jobId, CancellationToken ct) {
		_logger.LogInformation("Processing scan job {JobId}", jobId);

		using var scope = _scopeFactory.CreateScope();
		var scanService = scope.ServiceProvider.GetRequiredService<ScanService>();

		// Create a progress reporter that sends updates via SignalR
		var progress = new Progress<ScanProgress>(async p => {
			await _hubContext.Clients
				.Group($"scan-{jobId}")
				.SendAsync("ScanProgress", new {
					JobId = p.JobId,
					Phase = p.Phase.ToString(),
					ProcessedFiles = p.ProcessedFiles,
					TotalFiles = p.TotalFiles,
					CurrentFile = p.CurrentFile,
					Percentage = p.TotalFiles > 0 ? (double)p.ProcessedFiles / p.TotalFiles * 100 : 0,
					ErrorMessage = p.ErrorMessage,
					Timestamp = DateTime.UtcNow
				}, ct);
		});

		try {
			await scanService.ExecuteScanAsync(jobId, progress, ct);
			_logger.LogInformation("Completed scan job {JobId}", jobId);

			// Notify completion
			await _hubContext.Clients
				.Group($"scan-{jobId}")
				.SendAsync("ScanComplete", new { JobId = jobId }, ct);

			// Invalidate caches
			await _hubContext.Clients
				.Group("cache-invalidation")
				.SendAsync("CacheInvalidation", new {
					Type = "RomFiles",
					Reason = "ScanComplete",
					JobId = jobId
				}, ct);
		} catch (OperationCanceledException) {
			_logger.LogInformation("Scan job {JobId} was cancelled", jobId);

			await _hubContext.Clients
				.Group($"scan-{jobId}")
				.SendAsync("ScanCancelled", new { JobId = jobId }, ct);
		} catch (Exception ex) {
			_logger.LogError(ex, "Scan job {JobId} failed", jobId);

			await _hubContext.Clients
				.Group($"scan-{jobId}")
				.SendAsync("ScanFailed", new { JobId = jobId, Error = ex.Message }, ct);
		}
	}
}
