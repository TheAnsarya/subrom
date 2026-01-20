namespace Subrom.Domain.Storage;

/// <summary>
/// Represents a scanning job with its status and results.
/// </summary>
public sealed record ScanJob {
	/// <summary>Unique identifier for this scan job.</summary>
	public required Guid Id { get; init; }

	/// <summary>The drive being scanned, if specific to a drive.</summary>
	public Guid? DriveId { get; init; }

	/// <summary>Root path being scanned.</summary>
	public required string RootPath { get; init; }

	/// <summary>Current status of the scan.</summary>
	public ScanJobStatus Status { get; set; } = ScanJobStatus.Pending;

	/// <summary>When the scan was started.</summary>
	public DateTime? StartedAt { get; set; }

	/// <summary>When the scan completed (success or failure).</summary>
	public DateTime? CompletedAt { get; set; }

	/// <summary>Total files found to scan.</summary>
	public int TotalFiles { get; set; }

	/// <summary>Files processed so far.</summary>
	public int ProcessedFiles { get; set; }

	/// <summary>Files that matched a DAT entry.</summary>
	public int VerifiedFiles { get; set; }

	/// <summary>Files that didn't match any DAT entry.</summary>
	public int UnknownFiles { get; set; }

	/// <summary>Files that had errors during scanning.</summary>
	public int ErrorFiles { get; set; }

	/// <summary>Current file being processed.</summary>
	public string? CurrentFile { get; set; }

	/// <summary>Error message if scan failed.</summary>
	public string? ErrorMessage { get; set; }

	/// <summary>When this record was created.</summary>
	public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

	/// <summary>Scan options used.</summary>
	public bool Recursive { get; init; } = true;

	/// <summary>Whether to verify hashes against DAT files.</summary>
	public bool VerifyHashes { get; init; } = true;

	/// <summary>Progress percentage (0-100).</summary>
	public double Progress => TotalFiles > 0 ? (double)ProcessedFiles / TotalFiles * 100 : 0;

	/// <summary>Creates a new ScanJob with a generated ID.</summary>
	public static ScanJob Create(string rootPath, Guid? driveId = null) => new() {
		Id = Guid.CreateVersion7(),
		RootPath = rootPath,
		DriveId = driveId,
	};
}

/// <summary>
/// Status of a scan job.
/// </summary>
public enum ScanJobStatus {
	Pending,
	Running,
	Completed,
	Failed,
	Cancelled
}
