using Subrom.Domain.Aggregates.Scanning;

namespace Subrom.Application.Interfaces;

/// <summary>
/// Service interface for incremental file scanning with checkpointing.
/// Designed for collections with 500K+ files across multiple drives.
/// </summary>
public interface IIncrementalScanService {
	/// <summary>
	/// Starts or resumes an incremental scan for a path.
	/// </summary>
	/// <param name="scanPath">Root path to scan.</param>
	/// <param name="options">Scan options.</param>
	/// <param name="progress">Progress callback.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>Scan result summary.</returns>
	Task<IncrementalScanResult> ScanAsync(
		string scanPath,
		IncrementalScanOptions options,
		IProgress<IncrementalScanProgress>? progress = null,
		CancellationToken ct = default);

	/// <summary>
	/// Creates a checkpoint for the current scan state.
	/// </summary>
	Task<ScanCheckpoint> CreateCheckpointAsync(Guid scanJobId, CancellationToken ct = default);

	/// <summary>
	/// Resumes a scan from a checkpoint.
	/// </summary>
	Task<IncrementalScanResult> ResumeFromCheckpointAsync(
		ScanCheckpoint checkpoint,
		IProgress<IncrementalScanProgress>? progress = null,
		CancellationToken ct = default);

	/// <summary>
	/// Gets pending files that need rescanning (new or modified).
	/// </summary>
	IAsyncEnumerable<FileChange> GetPendingChangesAsync(string scanPath, CancellationToken ct = default);

	/// <summary>
	/// Marks a file as scanned with the current state.
	/// </summary>
	Task MarkFileScannedAsync(string filePath, DateTime lastModified, long size, CancellationToken ct = default);

	/// <summary>
	/// Invalidates scan state for a path (file deleted or modified).
	/// </summary>
	Task InvalidateFileAsync(string filePath, CancellationToken ct = default);
}

/// <summary>
/// Options for incremental scanning.
/// </summary>
public sealed record IncrementalScanOptions {
	/// <summary>
	/// Whether to scan recursively into subdirectories.
	/// </summary>
	public bool Recursive { get; init; } = true;

	/// <summary>
	/// File patterns to include (e.g., "*.zip", "*.7z"). Empty = all files.
	/// </summary>
	public IReadOnlyList<string> IncludePatterns { get; init; } = [];

	/// <summary>
	/// File/folder patterns to exclude (e.g., "*.txt", "temp/").
	/// </summary>
	public IReadOnlyList<string> ExcludePatterns { get; init; } = [];

	/// <summary>
	/// Whether to scan archive contents.
	/// </summary>
	public bool ScanArchiveContents { get; init; } = true;

	/// <summary>
	/// Maximum parallel I/O operations.
	/// </summary>
	public int MaxParallelism { get; init; } = 4;

	/// <summary>
	/// Whether to only scan changed files (new or modified since last scan).
	/// </summary>
	public bool IncrementalOnly { get; init; } = true;

	/// <summary>
	/// Interval for checkpointing progress (number of files).
	/// </summary>
	public int CheckpointInterval { get; init; } = 1000;

	/// <summary>
	/// Whether to compute hashes during scan (slower but complete).
	/// </summary>
	public bool ComputeHashes { get; init; } = false;
}

/// <summary>
/// Progress information for incremental scan.
/// </summary>
public sealed record IncrementalScanProgress {
	public required string Phase { get; init; }
	public required string CurrentPath { get; init; }
	public required int FilesScanned { get; init; }
	public required int FilesSkipped { get; init; }
	public required int DirectoriesScanned { get; init; }
	public int? TotalFiles { get; init; }
	public long BytesScanned { get; init; }
	public TimeSpan Elapsed { get; init; }
	public double FilesPerSecond => Elapsed.TotalSeconds > 0 ? FilesScanned / Elapsed.TotalSeconds : 0;

	/// <summary>
	/// Estimated time remaining based on current progress and rate.
	/// </summary>
	public TimeSpan? EstimatedTimeRemaining {
		get {
			if (TotalFiles is null || TotalFiles == 0 || FilesPerSecond <= 0) return null;
			var remaining = TotalFiles.Value - FilesScanned;
			if (remaining <= 0) return TimeSpan.Zero;
			return TimeSpan.FromSeconds(remaining / FilesPerSecond);
		}
	}

	/// <summary>
	/// Estimated completion time.
	/// </summary>
	public DateTime? EstimatedCompletionTime {
		get {
			var remaining = EstimatedTimeRemaining;
			if (remaining is null) return null;
			return DateTime.UtcNow.Add(remaining.Value);
		}
	}

	/// <summary>
	/// Progress percentage (0-100).
	/// </summary>
	public double? ProgressPercentage {
		get {
			if (TotalFiles is null || TotalFiles == 0) return null;
			return (double)FilesScanned / TotalFiles.Value * 100.0;
		}
	}
}

/// <summary>
/// Result of an incremental scan.
/// </summary>
public sealed record IncrementalScanResult {
	public required Guid ScanJobId { get; init; }
	public required string ScanPath { get; init; }
	public required int TotalFilesScanned { get; init; }
	public required int NewFilesFound { get; init; }
	public required int ModifiedFilesFound { get; init; }
	public required int DeletedFilesDetected { get; init; }
	public required int FilesSkipped { get; init; }
	public required int ErrorCount { get; init; }
	public required long TotalBytesScanned { get; init; }
	public required TimeSpan Duration { get; init; }
	public required bool WasResumed { get; init; }
	public required bool IsComplete { get; init; }
	public IReadOnlyList<ScanError> Errors { get; init; } = [];
}

/// <summary>
/// Checkpoint for resuming interrupted scans.
/// </summary>
public sealed record ScanCheckpoint {
	public required Guid ScanJobId { get; init; }
	public required string ScanPath { get; init; }
	public required IncrementalScanOptions Options { get; init; }
	public required DateTime CreatedAt { get; init; }
	public required int FilesProcessed { get; init; }
	public required string? LastProcessedPath { get; init; }
	public required IReadOnlyList<string> PendingDirectories { get; init; }
}

/// <summary>
/// Represents a file change detected during scan.
/// </summary>
public sealed record FileChange {
	public required string FilePath { get; init; }
	public required FileChangeType ChangeType { get; init; }
	public required long Size { get; init; }
	public required DateTime LastModified { get; init; }
	public DateTime? PreviousLastModified { get; init; }
	public long? PreviousSize { get; init; }
}

/// <summary>
/// Type of file change.
/// </summary>
public enum FileChangeType {
	/// <summary>New file not previously scanned.</summary>
	New,
	/// <summary>File modified since last scan.</summary>
	Modified,
	/// <summary>File deleted since last scan.</summary>
	Deleted
}

/// <summary>
/// Error encountered during scan.
/// </summary>
public sealed record ScanError {
	public required string FilePath { get; init; }
	public required string ErrorMessage { get; init; }
	public required string ErrorType { get; init; }
}
