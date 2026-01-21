using Subrom.Domain.ValueObjects;

namespace Subrom.Application.Interfaces;

/// <summary>
/// Service interface for managing hash job queues.
/// Supports priority-based queuing, cancellation, and caching for large file operations.
/// </summary>
public interface IHashJobService {
	/// <summary>
	/// Queues a file for hashing with optional priority.
	/// </summary>
	/// <param name="filePath">Path to the file to hash.</param>
	/// <param name="priority">Job priority (higher = processed sooner).</param>
	/// <param name="skipBytes">Bytes to skip (header removal).</param>
	/// <returns>Job ID for tracking.</returns>
	Guid QueueHashJob(string filePath, HashJobPriority priority = HashJobPriority.Normal, int skipBytes = 0);

	/// <summary>
	/// Queues multiple files for batch hashing.
	/// </summary>
	/// <param name="filePaths">Files to hash.</param>
	/// <param name="priority">Priority for all jobs in batch.</param>
	/// <returns>Batch ID for tracking all jobs.</returns>
	Guid QueueBatch(IEnumerable<string> filePaths, HashJobPriority priority = HashJobPriority.Normal);

	/// <summary>
	/// Gets the current status of a hash job.
	/// </summary>
	Task<HashJobStatus?> GetJobStatusAsync(Guid jobId, CancellationToken ct = default);

	/// <summary>
	/// Gets the result if the job is complete.
	/// </summary>
	Task<RomHashes?> GetJobResultAsync(Guid jobId, CancellationToken ct = default);

	/// <summary>
	/// Cancels a queued or in-progress hash job.
	/// </summary>
	Task<bool> CancelJobAsync(Guid jobId);

	/// <summary>
	/// Cancels all jobs in a batch.
	/// </summary>
	Task CancelBatchAsync(Guid batchId);

	/// <summary>
	/// Gets cached hashes for a file if available and valid.
	/// </summary>
	Task<RomHashes?> GetCachedHashesAsync(string filePath, CancellationToken ct = default);

	/// <summary>
	/// Invalidates cached hashes for a file.
	/// </summary>
	Task InvalidateCacheAsync(string filePath);

	/// <summary>
	/// Gets overall queue statistics.
	/// </summary>
	HashQueueStats GetQueueStats();

	/// <summary>
	/// Event fired when a job completes (success or failure).
	/// </summary>
	event EventHandler<HashJobCompletedEventArgs>? JobCompleted;

	/// <summary>
	/// Event fired when job progress updates.
	/// </summary>
	event EventHandler<HashJobProgressEventArgs>? JobProgress;
}

/// <summary>
/// Priority levels for hash jobs.
/// </summary>
public enum HashJobPriority {
	/// <summary>Background processing (lowest priority).</summary>
	Background = 0,
	/// <summary>Normal user-initiated operation.</summary>
	Normal = 1,
	/// <summary>High priority (user waiting for result).</summary>
	High = 2,
	/// <summary>Critical (verification in progress).</summary>
	Critical = 3
}

/// <summary>
/// Status of a hash job.
/// </summary>
public enum HashJobState {
	/// <summary>Job is queued waiting to start.</summary>
	Queued,
	/// <summary>Job is currently being processed.</summary>
	InProgress,
	/// <summary>Job completed successfully.</summary>
	Completed,
	/// <summary>Job failed with error.</summary>
	Failed,
	/// <summary>Job was cancelled.</summary>
	Cancelled
}

/// <summary>
/// Status information for a hash job.
/// </summary>
public sealed record HashJobStatus {
	public required Guid JobId { get; init; }
	public required string FilePath { get; init; }
	public required HashJobState State { get; init; }
	public required HashJobPriority Priority { get; init; }
	public Guid? BatchId { get; init; }
	public long BytesProcessed { get; init; }
	public long TotalBytes { get; init; }
	public double ProgressPercent => TotalBytes > 0 ? (double)BytesProcessed / TotalBytes * 100 : 0;
	public DateTime QueuedAt { get; init; }
	public DateTime? StartedAt { get; init; }
	public DateTime? CompletedAt { get; init; }
	public string? ErrorMessage { get; init; }
	public RomHashes? Result { get; init; }
}

/// <summary>
/// Queue statistics.
/// </summary>
public sealed record HashQueueStats {
	public required int QueuedCount { get; init; }
	public required int InProgressCount { get; init; }
	public required int CompletedCount { get; init; }
	public required int FailedCount { get; init; }
	public required long TotalBytesQueued { get; init; }
	public required long TotalBytesProcessed { get; init; }
	public required int MaxConcurrency { get; init; }
}

/// <summary>
/// Event args for job completion.
/// </summary>
public sealed class HashJobCompletedEventArgs : EventArgs {
	public required Guid JobId { get; init; }
	public required string FilePath { get; init; }
	public required HashJobState State { get; init; }
	public RomHashes? Result { get; init; }
	public string? ErrorMessage { get; init; }
	public TimeSpan Duration { get; init; }
}

/// <summary>
/// Event args for job progress.
/// </summary>
public sealed class HashJobProgressEventArgs : EventArgs {
	public required Guid JobId { get; init; }
	public required string FilePath { get; init; }
	public required long BytesProcessed { get; init; }
	public required long TotalBytes { get; init; }
	public double ProgressPercent => TotalBytes > 0 ? (double)BytesProcessed / TotalBytes * 100 : 0;
}
