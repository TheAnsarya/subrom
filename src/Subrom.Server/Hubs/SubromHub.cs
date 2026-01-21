using Microsoft.AspNetCore.SignalR;

namespace Subrom.Server.Hubs;

/// <summary>
/// SignalR hub for real-time updates.
/// </summary>
public class SubromHub : Hub {
	private readonly ILogger<SubromHub> _logger;

	public SubromHub(ILogger<SubromHub> logger) {
		_logger = logger;
	}

	public override async Task OnConnectedAsync() {
		_logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
		await base.OnConnectedAsync();
	}

	public override async Task OnDisconnectedAsync(Exception? exception) {
		_logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
		await base.OnDisconnectedAsync(exception);
	}

	/// <summary>
	/// Subscribes to a specific scan job's progress.
	/// </summary>
	public async Task SubscribeToScanJob(Guid jobId) {
		await Groups.AddToGroupAsync(Context.ConnectionId, $"scan-{jobId}");
		_logger.LogDebug("Client {ConnectionId} subscribed to scan job {JobId}", Context.ConnectionId, jobId);
	}

	/// <summary>
	/// Unsubscribes from a scan job's progress.
	/// </summary>
	public async Task UnsubscribeFromScanJob(Guid jobId) {
		await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"scan-{jobId}");
		_logger.LogDebug("Client {ConnectionId} unsubscribed from scan job {JobId}", Context.ConnectionId, jobId);
	}

	/// <summary>
	/// Subscribes to DAT import progress.
	/// </summary>
	public async Task SubscribeToDatImport(Guid importId) {
		await Groups.AddToGroupAsync(Context.ConnectionId, $"import-{importId}");
	}

	/// <summary>
	/// Subscribes to hash progress updates for a scan job.
	/// </summary>
	public async Task SubscribeToHashProgress(Guid jobId) {
		await Groups.AddToGroupAsync(Context.ConnectionId, $"hash-{jobId}");
		_logger.LogDebug("Client {ConnectionId} subscribed to hash progress for job {JobId}", Context.ConnectionId, jobId);
	}

	/// <summary>
	/// Subscribes to cache invalidation events.
	/// </summary>
	public async Task SubscribeToCacheInvalidation() {
		await Groups.AddToGroupAsync(Context.ConnectionId, "cache-invalidation");
		_logger.LogDebug("Client {ConnectionId} subscribed to cache invalidation", Context.ConnectionId);
	}

	/// <summary>
	/// Unsubscribes from cache invalidation events.
	/// </summary>
	public async Task UnsubscribeFromCacheInvalidation() {
		await Groups.RemoveFromGroupAsync(Context.ConnectionId, "cache-invalidation");
	}
}

/// <summary>
/// Hub client interface for type-safe client invocation.
/// </summary>
public interface ISubromHubClient {
	// Scan events
	Task ScanJobStarted(ScanJobStartedMessage message);
	Task ScanJobProgress(ScanJobProgressMessage message);
	Task ScanJobPhaseChanged(ScanJobPhaseChangedMessage message);
	Task ScanJobCompleted(ScanJobCompletedMessage message);
	Task ScanJobFailed(ScanJobFailedMessage message);

	// DAT import events
	Task DatImportProgress(DatImportProgressMessage message);
	Task DatImportCompleted(DatImportCompletedMessage message);

	// Drive events
	Task DriveOnline(DriveStatusMessage message);
	Task DriveOffline(DriveStatusMessage message);

	// Hash progress events (for large files)
	Task HashProgress(HashProgressMessage message);

	// Cache invalidation events
	Task CacheInvalidated(CacheInvalidationMessage message);

	// General notifications
	Task Notification(NotificationMessage message);
}

// Message types
public record ScanJobStartedMessage(Guid JobId, string Type, DateTime StartedAt);

public record ScanJobProgressMessage(
	Guid JobId,
	string CurrentItem,
	int ProcessedItems,
	int TotalItems,
	double Progress,
	long ProcessedBytes,
	long TotalBytes);

public record ScanJobPhaseChangedMessage(Guid JobId, string Phase, int TotalItems);

public record ScanJobCompletedMessage(
	Guid JobId,
	int ProcessedItems,
	int SkippedItems,
	int ErrorItems,
	TimeSpan Duration);

public record ScanJobFailedMessage(Guid JobId, string ErrorMessage);

public record DatImportProgressMessage(
	Guid ImportId,
	string FileName,
	int GamesParsed,
	int? TotalGames,
	double Progress);

public record DatImportCompletedMessage(
	Guid ImportId,
	Guid DatFileId,
	string Name,
	int GameCount,
	int RomCount);

public record DriveStatusMessage(Guid DriveId, string Label, string RootPath);

public record NotificationMessage(string Type, string Title, string Message, object? Data = null);

// Cache invalidation events
public record CacheInvalidationMessage(
	string CacheKey,
	InvalidationType Type,
	Guid? EntityId = null,
	string? EntityType = null);

/// <summary>
/// Hash progress events for large file hashing.
/// </summary>
public record HashProgressMessage(
	Guid JobId,
	string FileName,
	long ProcessedBytes,
	long TotalBytes,
	double Percentage,
	double BytesPerSecond);

/// <summary>
/// Types of cache invalidation.
/// </summary>
public enum InvalidationType {
	/// <summary>Single entity was created.</summary>
	Created,
	/// <summary>Single entity was updated.</summary>
	Updated,
	/// <summary>Single entity was deleted.</summary>
	Deleted,
	/// <summary>Multiple entities were modified (bulk operation).</summary>
	BulkModified,
	/// <summary>Entire cache should be cleared.</summary>
	ClearAll
}
