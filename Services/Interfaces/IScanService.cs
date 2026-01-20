using Subrom.Domain.Storage;

namespace Subrom.Services.Interfaces;

/// <summary>
/// Delegate for sending scan progress updates via SignalR.
/// </summary>
public delegate Task ScanProgressBroadcaster(ScanJob job, string eventName);

/// <summary>
/// Service for scanning directories for ROM files.
/// </summary>
public interface IScanService {
	/// <summary>
	/// Sets the SignalR broadcaster for progress updates.
	/// </summary>
	void SetBroadcaster(ScanProgressBroadcaster broadcaster);

	/// <summary>
	/// Enqueues a new scan job for processing.
	/// </summary>
	ValueTask<ScanJob> EnqueueScanAsync(string rootPath, Guid? driveId = null, bool recursive = true, bool verifyHashes = true);

	/// <summary>
	/// Gets a scan job by its ID.
	/// </summary>
	ScanJob? GetJob(Guid jobId);

	/// <summary>
	/// Gets all active scan jobs.
	/// </summary>
	IEnumerable<ScanJob> GetActiveJobs();

	/// <summary>
	/// Cancels a running scan job.
	/// </summary>
	bool CancelJob(Guid jobId);
}
