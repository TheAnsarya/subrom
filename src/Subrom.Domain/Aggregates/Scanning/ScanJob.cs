using Subrom.Domain.Common;

namespace Subrom.Domain.Aggregates.Scanning;

/// <summary>
/// Aggregate root for scan jobs.
/// Tracks the progress and results of file/hash scanning operations.
/// </summary>
public class ScanJob : AggregateRoot {
	/// <summary>
	/// Type of scan operation.
	/// </summary>
	public required ScanType Type { get; init; }

	/// <summary>
	/// Current status of the scan.
	/// </summary>
	public ScanStatus Status { get; private set; } = ScanStatus.Queued;

	/// <summary>
	/// Drive being scanned.
	/// </summary>
	public Guid? DriveId { get; init; }

	/// <summary>
	/// Specific path being scanned (if not whole drive).
	/// </summary>
	public string? TargetPath { get; init; }

	/// <summary>
	/// When the scan was requested.
	/// </summary>
	public DateTime QueuedAt { get; init; } = DateTime.UtcNow;

	/// <summary>
	/// When the scan started executing.
	/// </summary>
	public DateTime? StartedAt { get; private set; }

	/// <summary>
	/// When the scan completed (success or failure).
	/// </summary>
	public DateTime? CompletedAt { get; private set; }

	/// <summary>
	/// Current phase of the scan.
	/// </summary>
	public string? CurrentPhase { get; private set; }

	/// <summary>
	/// Current item being processed.
	/// </summary>
	public string? CurrentItem { get; private set; }

	/// <summary>
	/// Total items to process.
	/// </summary>
	public int TotalItems { get; private set; }

	/// <summary>
	/// Items processed so far.
	/// </summary>
	public int ProcessedItems { get; private set; }

	/// <summary>
	/// Items skipped (cached, unchanged).
	/// </summary>
	public int SkippedItems { get; private set; }

	/// <summary>
	/// Items with errors.
	/// </summary>
	public int ErrorItems { get; private set; }

	/// <summary>
	/// Total bytes to process.
	/// </summary>
	public long TotalBytes { get; private set; }

	/// <summary>
	/// Bytes processed so far.
	/// </summary>
	public long ProcessedBytes { get; private set; }

	/// <summary>
	/// Error message if scan failed.
	/// </summary>
	public string? ErrorMessage { get; private set; }

	/// <summary>
	/// User who initiated the scan.
	/// </summary>
	public string? InitiatedBy { get; init; }

	/// <summary>
	/// Progress percentage (0-100).
	/// </summary>
	public double Progress =>
		TotalItems > 0 ? (double)ProcessedItems / TotalItems * 100 : 0;

	/// <summary>
	/// Elapsed time since scan started.
	/// </summary>
	public TimeSpan? Elapsed =>
		StartedAt.HasValue
			? (CompletedAt ?? DateTime.UtcNow) - StartedAt.Value
			: null;

	/// <summary>
	/// Creates a new scan job.
	/// </summary>
	public static ScanJob Create(ScanType type, Guid? driveId = null, string? targetPath = null) {
		var job = new ScanJob {
			Type = type,
			DriveId = driveId,
			TargetPath = targetPath
		};

		job.AddDomainEvent(new ScanJobCreatedEvent(job.Id, type));
		return job;
	}

	/// <summary>
	/// Starts the scan.
	/// </summary>
	public void Start() {
		Status = ScanStatus.Running;
		StartedAt = DateTime.UtcNow;
		AddDomainEvent(new ScanJobStartedEvent(Id));
	}

	/// <summary>
	/// Updates the scan phase.
	/// </summary>
	public void SetPhase(string phase, int totalItems = 0, long totalBytes = 0) {
		CurrentPhase = phase;
		if (totalItems > 0) TotalItems = totalItems;
		if (totalBytes > 0) TotalBytes = totalBytes;
		AddDomainEvent(new ScanJobPhaseChangedEvent(Id, phase, totalItems));
	}

	/// <summary>
	/// Reports progress on current item.
	/// </summary>
	public void ReportProgress(string currentItem, int processedItems, long processedBytes = 0) {
		CurrentItem = currentItem;
		ProcessedItems = processedItems;
		if (processedBytes > 0) ProcessedBytes = processedBytes;
		AddDomainEvent(new ScanJobProgressEvent(Id, currentItem, processedItems, TotalItems, Progress));
	}

	/// <summary>
	/// Reports an item was skipped.
	/// </summary>
	public void ReportSkipped() {
		SkippedItems++;
	}

	/// <summary>
	/// Reports an error on an item.
	/// </summary>
	public void ReportError(string item, string error) {
		ErrorItems++;
		AddDomainEvent(new ScanJobItemErrorEvent(Id, item, error));
	}

	/// <summary>
	/// Completes the scan successfully.
	/// </summary>
	public void Complete() {
		Status = ScanStatus.Completed;
		CompletedAt = DateTime.UtcNow;
		AddDomainEvent(new ScanJobCompletedEvent(Id, ProcessedItems, SkippedItems, ErrorItems));
	}

	/// <summary>
	/// Fails the scan with an error.
	/// </summary>
	public void Fail(string errorMessage) {
		Status = ScanStatus.Failed;
		CompletedAt = DateTime.UtcNow;
		ErrorMessage = errorMessage;
		AddDomainEvent(new ScanJobFailedEvent(Id, errorMessage));
	}

	/// <summary>
	/// Cancels the scan.
	/// </summary>
	public void Cancel() {
		Status = ScanStatus.Cancelled;
		CompletedAt = DateTime.UtcNow;
		AddDomainEvent(new ScanJobCancelledEvent(Id));
	}
}

/// <summary>
/// Type of scan operation.
/// </summary>
public enum ScanType {
	FileDiscovery,    // Find files on disk
	Hashing,          // Compute file hashes
	Verification,     // Verify against DATs
	Full              // All of the above
}

/// <summary>
/// Scan job status.
/// </summary>
public enum ScanStatus {
	Queued,
	Running,
	Completed,
	Failed,
	Cancelled
}

// Domain events
public sealed record ScanJobCreatedEvent(Guid JobId, ScanType Type) : DomainEvent;
public sealed record ScanJobStartedEvent(Guid JobId) : DomainEvent;
public sealed record ScanJobPhaseChangedEvent(Guid JobId, string Phase, int TotalItems) : DomainEvent;
public sealed record ScanJobProgressEvent(Guid JobId, string CurrentItem, int ProcessedItems, int TotalItems, double Progress) : DomainEvent;
public sealed record ScanJobItemErrorEvent(Guid JobId, string Item, string Error) : DomainEvent;
public sealed record ScanJobCompletedEvent(Guid JobId, int ProcessedItems, int SkippedItems, int ErrorItems) : DomainEvent;
public sealed record ScanJobFailedEvent(Guid JobId, string ErrorMessage) : DomainEvent;
public sealed record ScanJobCancelledEvent(Guid JobId) : DomainEvent;
