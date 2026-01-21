using Subrom.Domain.Aggregates.Scanning;

namespace Subrom.Application.Interfaces;

/// <summary>
/// Service for managing scan job resumability.
/// Handles pausing, resuming, and checkpoint management for scan jobs.
/// </summary>
public interface IScanResumeService {
	/// <summary>
	/// Pauses an active scan job.
	/// </summary>
	/// <param name="jobId">The ID of the scan job to pause</param>
	/// <param name="lastProcessedPath">Optional path of the last fully processed item</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>The paused scan job, or null if not found or not running</returns>
	Task<ScanJob?> PauseAsync(Guid jobId, string? lastProcessedPath = null, CancellationToken cancellationToken = default);

	/// <summary>
	/// Resumes a paused or failed scan job.
	/// </summary>
	/// <param name="jobId">The ID of the scan job to resume</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>The resumed scan job, or null if not found or cannot be resumed</returns>
	Task<ScanJob?> ResumeAsync(Guid jobId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Updates the checkpoint for an active scan job.
	/// </summary>
	/// <param name="jobId">The ID of the scan job</param>
	/// <param name="lastProcessedPath">Path of the last fully processed item</param>
	/// <param name="processedItems">Number of items processed</param>
	/// <param name="processedBytes">Number of bytes processed</param>
	/// <param name="cancellationToken">Cancellation token</param>
	Task SetCheckpointAsync(
		Guid jobId,
		string lastProcessedPath,
		int processedItems = 0,
		long processedBytes = 0,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets all scan jobs that can be resumed.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>List of resumable scan jobs</returns>
	Task<IReadOnlyList<ScanJob>> GetResumableJobsAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Checks if a path has a resumable scan job.
	/// </summary>
	/// <param name="targetPath">The path to check</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>The resumable job if found, otherwise null</returns>
	Task<ScanJob?> GetResumableJobForPathAsync(string targetPath, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets the resume information for a scan job.
	/// </summary>
	/// <param name="jobId">The ID of the scan job</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Resume information including the path to start from</returns>
	Task<ScanResumeInfo?> GetResumeInfoAsync(Guid jobId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Contains information needed to resume a scan job.
/// </summary>
/// <param name="JobId">The scan job ID</param>
/// <param name="TargetPath">The original target path being scanned</param>
/// <param name="LastProcessedPath">The last fully processed path (resume from here)</param>
/// <param name="ProcessedItems">Number of items already processed</param>
/// <param name="TotalItems">Total items to process</param>
/// <param name="ProcessedBytes">Bytes already processed</param>
/// <param name="TotalBytes">Total bytes to process</param>
/// <param name="ResumeCount">Number of times this job has been resumed</param>
/// <param name="CurrentPhase">The phase the job was in when paused</param>
public sealed record ScanResumeInfo(
	Guid JobId,
	string? TargetPath,
	string? LastProcessedPath,
	int ProcessedItems,
	int TotalItems,
	long ProcessedBytes,
	long TotalBytes,
	int ResumeCount,
	string? CurrentPhase) {

	/// <summary>
	/// Progress percentage already completed.
	/// </summary>
	public double CompletedProgress =>
		TotalItems > 0 ? (double)ProcessedItems / TotalItems * 100 : 0;

	/// <summary>
	/// Whether this is the first run (no previous resume).
	/// </summary>
	public bool IsFirstRun => ResumeCount == 0;

	/// <summary>
	/// Whether there is checkpoint data to resume from.
	/// </summary>
	public bool HasCheckpoint => !string.IsNullOrEmpty(LastProcessedPath);
}
