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
	public ScanJobStatus Status { get; init; } = ScanJobStatus.Pending;

	/// <summary>When the scan was started.</summary>
	public DateTime? StartedAt { get; init; }

	/// <summary>When the scan completed (success or failure).</summary>
	public DateTime? CompletedAt { get; init; }

	/// <summary>Total files found to scan.</summary>
	public int TotalFiles { get; init; }

	/// <summary>Files processed so far.</summary>
	public int ProcessedFiles { get; init; }

	/// <summary>Files that matched a DAT entry.</summary>
	public int VerifiedFiles { get; init; }

	/// <summary>Files that didn't match any DAT entry.</summary>
	public int UnknownFiles { get; init; }

	/// <summary>Files that had errors during scanning.</summary>
	public int ErrorFiles { get; init; }

	/// <summary>Current file being processed.</summary>
	public string? CurrentFile { get; init; }

	/// <summary>Error message if scan failed.</summary>
	public string? ErrorMessage { get; init; }

	/// <summary>When this record was created.</summary>
	public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

	/// <summary>Scan options used.</summary>
	public bool Recursive { get; init; } = true;

	/// <summary>Whether to verify hashes against DAT files.</summary>
	public bool VerifyHashes { get; init; } = true;

	/// <summary>Scanned ROM files with their metadata.</summary>
	public IReadOnlyList<ScannedRomInfo> ScannedRoms { get; init; } = [];

	/// <summary>Progress percentage (0-100).</summary>
	public double Progress => TotalFiles > 0 ? (double)ProcessedFiles / TotalFiles * 100 : 0;

	/// <summary>Creates a new ScanJob with a generated ID.</summary>
	public static ScanJob Create(string rootPath, Guid? driveId = null, bool recursive = true, bool verifyHashes = true) => new() {
		Id = Guid.CreateVersion7(),
		RootPath = rootPath,
		DriveId = driveId,
		Recursive = recursive,
		VerifyHashes = verifyHashes,
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

/// <summary>
/// Represents a scanned ROM file with its metadata and hashes.
/// </summary>
public sealed record ScannedRomInfo(
	string Path,
	string FileName,
	long Size,
	string? Crc32,
	string? Md5,
	string? Sha1,
	DateTime ModifiedAt
);
