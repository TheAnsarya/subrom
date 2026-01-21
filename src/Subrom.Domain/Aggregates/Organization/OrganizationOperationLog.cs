namespace Subrom.Domain.Aggregates.Organization;

/// <summary>
/// Entity for persisting organization operation history.
/// </summary>
public class OrganizationOperationLog {
	/// <summary>Operation ID.</summary>
	public Guid Id { get; init; } = Guid.NewGuid();

	/// <summary>When operation was performed.</summary>
	public DateTime PerformedAt { get; init; } = DateTime.UtcNow;

	/// <summary>Source directory path.</summary>
	public required string SourcePath { get; init; }

	/// <summary>Destination directory path.</summary>
	public required string DestinationPath { get; init; }

	/// <summary>Template name used.</summary>
	public required string TemplateName { get; init; }

	/// <summary>Template folder pattern.</summary>
	public required string FolderTemplate { get; init; }

	/// <summary>Template filename pattern.</summary>
	public required string FileNameTemplate { get; init; }

	/// <summary>Whether files were moved (true) or copied (false).</summary>
	public bool WasMoveOperation { get; init; }

	/// <summary>Number of files processed successfully.</summary>
	public int FilesProcessed { get; init; }

	/// <summary>Number of files skipped.</summary>
	public int FilesSkipped { get; init; }

	/// <summary>Number of files that failed.</summary>
	public int FilesFailed { get; init; }

	/// <summary>Total bytes processed.</summary>
	public long BytesProcessed { get; init; }

	/// <summary>Duration of operation in milliseconds.</summary>
	public long DurationMs { get; init; }

	/// <summary>Whether operation completed successfully.</summary>
	public bool Success { get; init; }

	/// <summary>Whether operation has been rolled back.</summary>
	public bool IsRolledBack { get; set; }

	/// <summary>When rollback was performed (if any).</summary>
	public DateTime? RolledBackAt { get; set; }

	/// <summary>Error messages (JSON array).</summary>
	public string? ErrorsJson { get; init; }

	/// <summary>Rollback data (JSON array of file moves).</summary>
	public string? RollbackDataJson { get; init; }

	/// <summary>User who initiated the operation (for future multi-user support).</summary>
	public string? InitiatedBy { get; init; }

	/// <summary>Additional notes.</summary>
	public string? Notes { get; set; }

	/// <summary>
	/// Navigation property for operation entries (individual file operations).
	/// </summary>
	public ICollection<OrganizationOperationEntry> Entries { get; init; } = [];
}

/// <summary>
/// Individual file operation entry within an organization operation.
/// </summary>
public class OrganizationOperationEntry {
	/// <summary>Entry ID.</summary>
	public Guid Id { get; init; } = Guid.NewGuid();

	/// <summary>Parent operation ID.</summary>
	public Guid OperationId { get; init; }

	/// <summary>Parent operation.</summary>
	public OrganizationOperationLog? Operation { get; init; }

	/// <summary>Operation type (Move, Copy, Skip).</summary>
	public required string OperationType { get; init; }

	/// <summary>Original source path.</summary>
	public required string SourcePath { get; init; }

	/// <summary>Destination path.</summary>
	public required string DestinationPath { get; init; }

	/// <summary>File size in bytes.</summary>
	public long Size { get; init; }

	/// <summary>Whether operation succeeded.</summary>
	public bool Success { get; init; }

	/// <summary>Error message if failed.</summary>
	public string? ErrorMessage { get; init; }

	/// <summary>Processing timestamp.</summary>
	public DateTime ProcessedAt { get; init; } = DateTime.UtcNow;

	/// <summary>CRC32 hash (for verification).</summary>
	public string? Crc { get; init; }
}
