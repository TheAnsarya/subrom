using Subrom.Domain.Aggregates.Organization;

namespace Subrom.Application.Interfaces;

/// <summary>
/// Service for organizing ROMs according to templates.
/// </summary>
public interface IOrganizationService {
	/// <summary>
	/// Plans organization operations without executing them (dry run).
	/// </summary>
	/// <param name="request">The organization request.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The planned operations.</returns>
	Task<OrganizationPlan> PlanAsync(OrganizationRequest request, CancellationToken cancellationToken = default);

	/// <summary>
	/// Executes an organization plan.
	/// </summary>
	/// <param name="plan">The plan to execute.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The execution result.</returns>
	Task<OrganizationResult> ExecuteAsync(OrganizationPlan plan, CancellationToken cancellationToken = default);

	/// <summary>
	/// Plans and executes organization in one step.
	/// </summary>
	/// <param name="request">The organization request.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The execution result.</returns>
	Task<OrganizationResult> OrganizeAsync(OrganizationRequest request, CancellationToken cancellationToken = default);

	/// <summary>
	/// Rolls back a completed organization operation.
	/// </summary>
	/// <param name="operationId">The operation to roll back.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>True if rollback succeeded.</returns>
	Task<bool> RollbackAsync(Guid operationId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets the history of organization operations.
	/// </summary>
	/// <param name="limit">Maximum number of operations to return.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>List of past operations.</returns>
	Task<IReadOnlyList<OrganizationOperation>> GetHistoryAsync(int limit = 100, CancellationToken cancellationToken = default);
}

/// <summary>
/// Request to organize ROMs.
/// </summary>
public record OrganizationRequest {
	/// <summary>Source directory containing ROMs to organize.</summary>
	public required string SourcePath { get; init; }

	/// <summary>Destination directory for organized ROMs.</summary>
	public required string DestinationPath { get; init; }

	/// <summary>Template to use for organization.</summary>
	public required OrganizationTemplate Template { get; init; }

	/// <summary>Whether to move files (true) or copy them (false).</summary>
	public bool MoveFiles { get; init; } = true;

	/// <summary>DAT file ID for metadata lookup.</summary>
	public Guid? DatFileId { get; init; }

	/// <summary>Whether to process archives.</summary>
	public bool ProcessArchives { get; init; } = true;

	/// <summary>Whether to extract archives before organizing.</summary>
	public bool ExtractArchives { get; init; } = false;

	/// <summary>Whether to delete empty source folders after organization.</summary>
	public bool DeleteEmptyFolders { get; init; } = true;

	/// <summary>File patterns to include (e.g., "*.nes", "*.zip").</summary>
	public IReadOnlyList<string> IncludePatterns { get; init; } = ["*.*"];

	/// <summary>File patterns to exclude.</summary>
	public IReadOnlyList<string> ExcludePatterns { get; init; } = [];
}

/// <summary>
/// A planned organization operation.
/// </summary>
public record OrganizationPlan {
	/// <summary>Unique ID for this plan.</summary>
	public Guid Id { get; init; } = Guid.NewGuid();

	/// <summary>Original request.</summary>
	public required OrganizationRequest Request { get; init; }

	/// <summary>Planned file operations.</summary>
	public required IReadOnlyList<FileOperation> Operations { get; init; }

	/// <summary>Total size of files to be processed.</summary>
	public long TotalBytes { get; init; }

	/// <summary>Number of files to process.</summary>
	public int FileCount => Operations.Count;

	/// <summary>Any warnings generated during planning.</summary>
	public IReadOnlyList<string> Warnings { get; init; } = [];

	/// <summary>When the plan was created.</summary>
	public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// A single file operation within an organization plan.
/// </summary>
public record FileOperation {
	/// <summary>Type of operation.</summary>
	public required FileOperationType Type { get; init; }

	/// <summary>Source file path.</summary>
	public required string SourcePath { get; init; }

	/// <summary>Destination file path.</summary>
	public required string DestinationPath { get; init; }

	/// <summary>File size in bytes.</summary>
	public long Size { get; init; }

	/// <summary>Template context used for this file.</summary>
	public required TemplateContext Context { get; init; }

	/// <summary>Whether destination already exists.</summary>
	public bool DestinationExists { get; init; }

	/// <summary>Whether operation would overwrite existing file.</summary>
	public bool WouldOverwrite { get; init; }
}

/// <summary>
/// Type of file operation.
/// </summary>
public enum FileOperationType {
	/// <summary>Move file from source to destination.</summary>
	Move,

	/// <summary>Copy file from source to destination.</summary>
	Copy,

	/// <summary>Skip this file (already exists, excluded, etc.).</summary>
	Skip,

	/// <summary>Extract from archive and place at destination.</summary>
	Extract
}

/// <summary>
/// Result of executing an organization plan.
/// </summary>
public record OrganizationResult {
	/// <summary>The operation ID.</summary>
	public Guid OperationId { get; init; } = Guid.NewGuid();

	/// <summary>Whether the operation succeeded overall.</summary>
	public bool Success { get; init; }

	/// <summary>Number of files successfully processed.</summary>
	public int FilesProcessed { get; init; }

	/// <summary>Number of files skipped.</summary>
	public int FilesSkipped { get; init; }

	/// <summary>Number of files that failed.</summary>
	public int FilesFailed { get; init; }

	/// <summary>Total bytes processed.</summary>
	public long BytesProcessed { get; init; }

	/// <summary>Errors that occurred.</summary>
	public IReadOnlyList<FileOperationError> Errors { get; init; } = [];

	/// <summary>Duration of the operation.</summary>
	public TimeSpan Duration { get; init; }

	/// <summary>When operation started.</summary>
	public DateTime StartedAt { get; init; }

	/// <summary>When operation completed.</summary>
	public DateTime CompletedAt { get; init; }

	/// <summary>Whether this operation can be rolled back.</summary>
	public bool CanRollback { get; init; }
}

/// <summary>
/// An error that occurred during a file operation.
/// </summary>
public record FileOperationError {
	/// <summary>The operation that failed.</summary>
	public required FileOperation Operation { get; init; }

	/// <summary>Error message.</summary>
	public required string Message { get; init; }

	/// <summary>Exception type if available.</summary>
	public string? ExceptionType { get; init; }
}

/// <summary>
/// A completed organization operation for history tracking.
/// </summary>
public record OrganizationOperation {
	/// <summary>Operation ID.</summary>
	public required Guid Id { get; init; }

	/// <summary>When operation was performed.</summary>
	public required DateTime PerformedAt { get; init; }

	/// <summary>Source path.</summary>
	public required string SourcePath { get; init; }

	/// <summary>Destination path.</summary>
	public required string DestinationPath { get; init; }

	/// <summary>Template name used.</summary>
	public required string TemplateName { get; init; }

	/// <summary>Whether files were moved (true) or copied (false).</summary>
	public bool WasMoveOperation { get; init; }

	/// <summary>Number of files processed.</summary>
	public int FileCount { get; init; }

	/// <summary>Total bytes processed.</summary>
	public long TotalBytes { get; init; }

	/// <summary>Whether operation can still be rolled back.</summary>
	public bool CanRollback { get; init; }

	/// <summary>Rollback data if available.</summary>
	public IReadOnlyList<RollbackEntry>? RollbackData { get; init; }
}

/// <summary>
/// Entry for rolling back a file operation.
/// </summary>
public record RollbackEntry {
	/// <summary>Current path of the file.</summary>
	public required string CurrentPath { get; init; }

	/// <summary>Original path before organization.</summary>
	public required string OriginalPath { get; init; }

	/// <summary>Whether file was moved (true) or copied (false).</summary>
	public bool WasMoved { get; init; }
}
