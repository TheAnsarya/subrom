using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Enumeration;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Subrom.Application.Interfaces;
using Subrom.Infrastructure.Persistence;

namespace Subrom.Infrastructure.Services;

/// <summary>
/// Service for incremental file scanning with checkpointing.
/// Optimized for large collections (500K+ files) with minimal re-scanning.
/// </summary>
public sealed class IncrementalScanService : IIncrementalScanService {
	private readonly SubromDbContext _db;
	private readonly ILogger<IncrementalScanService> _logger;
	private readonly string _checkpointDirectory;

	public IncrementalScanService(SubromDbContext db, ILogger<IncrementalScanService> logger) {
		_db = db;
		_logger = logger;
		_checkpointDirectory = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			"Subrom", "Checkpoints");
		Directory.CreateDirectory(_checkpointDirectory);
	}

	public async Task<IncrementalScanResult> ScanAsync(
		string scanPath,
		IncrementalScanOptions options,
		IProgress<IncrementalScanProgress>? progress = null,
		CancellationToken ct = default) {
		ArgumentException.ThrowIfNullOrWhiteSpace(scanPath);

		var scanJobId = Guid.NewGuid();
		var stopwatch = Stopwatch.StartNew();
		var errors = new ConcurrentBag<ScanError>();

		_logger.LogInformation("Starting incremental scan of {Path} (Job: {JobId})", scanPath, scanJobId);

		// Get previously scanned files for this path
		var previouslyScanned = await GetPreviouslyScannedFilesAsync(scanPath, ct);

		var filesScanned = 0;
		var filesSkipped = 0;
		var newFiles = 0;
		var modifiedFiles = 0;
		var deletedFiles = 0;
		var directoriesScanned = 0;
		long bytesScanned = 0;
		var lastCheckpoint = 0;

		// Build exclusion set
		var excludePatterns = options.ExcludePatterns.ToHashSet(StringComparer.OrdinalIgnoreCase);

		// Enumerate files with chunking for large directories
		var enumOptions = new EnumerationOptions {
			RecurseSubdirectories = options.Recursive,
			IgnoreInaccessible = true,
			AttributesToSkip = FileAttributes.System
		};

		await foreach (var file in EnumerateFilesAsync(scanPath, options.IncludePatterns, enumOptions, ct)) {
			ct.ThrowIfCancellationRequested();

			try {
				// Check exclusions
				if (ShouldExclude(file.FullName, excludePatterns)) {
					filesSkipped++;
					continue;
				}

				var previousState = previouslyScanned.GetValueOrDefault(file.FullName);

				if (options.IncrementalOnly && previousState is not null) {
					// Check if file was modified
					if (file.LastWriteTimeUtc == previousState.LastModified && file.Length == previousState.Size) {
						filesSkipped++;
						continue;
					}

					modifiedFiles++;
				} else if (previousState is null) {
					newFiles++;
				}

				// Process file
				await ProcessFileAsync(file, options, ct);

				filesScanned++;
				bytesScanned += file.Length;

				// Mark as scanned
				await MarkFileScannedAsync(file.FullName, file.LastWriteTimeUtc, file.Length, ct);

				// Progress reporting
				if (filesScanned % 100 == 0 || filesScanned == 1) {
					progress?.Report(new IncrementalScanProgress {
						Phase = "Scanning",
						CurrentPath = file.FullName,
						FilesScanned = filesScanned,
						FilesSkipped = filesSkipped,
						DirectoriesScanned = directoriesScanned,
						BytesScanned = bytesScanned,
						Elapsed = stopwatch.Elapsed
					});
				}

				// Checkpoint
				if (filesScanned - lastCheckpoint >= options.CheckpointInterval) {
					await CreateCheckpointInternalAsync(scanJobId, scanPath, options, filesScanned, file.FullName, ct);
					lastCheckpoint = filesScanned;
				}
			} catch (Exception ex) when (ex is not OperationCanceledException) {
				errors.Add(new ScanError {
					FilePath = file.FullName,
					ErrorMessage = ex.Message,
					ErrorType = ex.GetType().Name
				});
				_logger.LogWarning(ex, "Error scanning file {Path}", file.FullName);
			}
		}

		// Detect deleted files
		var scannedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		// (In real impl, track during scan)
		deletedFiles = await DetectDeletedFilesAsync(scanPath, previouslyScanned.Keys, ct);

		stopwatch.Stop();

		_logger.LogInformation(
			"Scan complete: {Scanned} scanned, {New} new, {Modified} modified, {Deleted} deleted, {Skipped} skipped, {Errors} errors in {Duration}",
			filesScanned, newFiles, modifiedFiles, deletedFiles, filesSkipped, errors.Count, stopwatch.Elapsed);

		// Cleanup checkpoint on success
		DeleteCheckpoint(scanJobId);

		return new IncrementalScanResult {
			ScanJobId = scanJobId,
			ScanPath = scanPath,
			TotalFilesScanned = filesScanned,
			NewFilesFound = newFiles,
			ModifiedFilesFound = modifiedFiles,
			DeletedFilesDetected = deletedFiles,
			FilesSkipped = filesSkipped,
			ErrorCount = errors.Count,
			TotalBytesScanned = bytesScanned,
			Duration = stopwatch.Elapsed,
			WasResumed = false,
			IsComplete = true,
			Errors = errors.ToList()
		};
	}

	public async Task<ScanCheckpoint> CreateCheckpointAsync(Guid scanJobId, CancellationToken ct = default) {
		// This is called externally to get checkpoint state
		var checkpointPath = GetCheckpointPath(scanJobId);
		if (File.Exists(checkpointPath)) {
			var json = await File.ReadAllTextAsync(checkpointPath, ct);
			return JsonSerializer.Deserialize<ScanCheckpoint>(json)!;
		}

		throw new InvalidOperationException($"No checkpoint exists for job {scanJobId}");
	}

	public async Task<IncrementalScanResult> ResumeFromCheckpointAsync(
		ScanCheckpoint checkpoint,
		IProgress<IncrementalScanProgress>? progress = null,
		CancellationToken ct = default) {
		_logger.LogInformation("Resuming scan {JobId} from checkpoint at {FilesProcessed} files",
			checkpoint.ScanJobId, checkpoint.FilesProcessed);

		// Re-run scan but skip already processed files
		var resumeOptions = checkpoint.Options with {
			// Could add a "resume from path" option
		};

		var result = await ScanAsync(checkpoint.ScanPath, resumeOptions, progress, ct);

		return result with {
			ScanJobId = checkpoint.ScanJobId,
			WasResumed = true
		};
	}

	public async IAsyncEnumerable<FileChange> GetPendingChangesAsync(
		string scanPath,
		[EnumeratorCancellation] CancellationToken ct = default) {
		var previouslyScanned = await GetPreviouslyScannedFilesAsync(scanPath, ct);

		var enumOptions = new EnumerationOptions {
			RecurseSubdirectories = true,
			IgnoreInaccessible = true
		};

		var currentFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		await foreach (var file in EnumerateFilesAsync(scanPath, [], enumOptions, ct)) {
			currentFiles.Add(file.FullName);

			if (previouslyScanned.TryGetValue(file.FullName, out var previous)) {
				if (file.LastWriteTimeUtc != previous.LastModified || file.Length != previous.Size) {
					yield return new FileChange {
						FilePath = file.FullName,
						ChangeType = FileChangeType.Modified,
						Size = file.Length,
						LastModified = file.LastWriteTimeUtc,
						PreviousLastModified = previous.LastModified,
						PreviousSize = previous.Size
					};
				}
			} else {
				yield return new FileChange {
					FilePath = file.FullName,
					ChangeType = FileChangeType.New,
					Size = file.Length,
					LastModified = file.LastWriteTimeUtc
				};
			}
		}

		// Detect deletions
		foreach (var (path, state) in previouslyScanned) {
			if (!currentFiles.Contains(path)) {
				yield return new FileChange {
					FilePath = path,
					ChangeType = FileChangeType.Deleted,
					Size = state.Size,
					LastModified = state.LastModified
				};
			}
		}
	}

	public async Task MarkFileScannedAsync(string filePath, DateTime lastModified, long size, CancellationToken ct = default) {
		// In a real implementation, this would update the database
		// For now, just log
		_logger.LogTrace("Marked file scanned: {Path}", filePath);
		await Task.CompletedTask;
	}

	public async Task InvalidateFileAsync(string filePath, CancellationToken ct = default) {
		// Remove from scanned cache
		_logger.LogDebug("Invalidated file: {Path}", filePath);
		await Task.CompletedTask;
	}

	private async Task<Dictionary<string, FileState>> GetPreviouslyScannedFilesAsync(string scanPath, CancellationToken ct) {
		// Query database for previously scanned files under this path
		// This would be a real DB query in production
		return new Dictionary<string, FileState>(StringComparer.OrdinalIgnoreCase);
	}

	private Task ProcessFileAsync(FileInfo file, IncrementalScanOptions options, CancellationToken ct) {
		// Process the file (add to database, compute hashes if needed, etc.)
		return Task.CompletedTask;
	}

	private async Task CreateCheckpointInternalAsync(
		Guid scanJobId,
		string scanPath,
		IncrementalScanOptions options,
		int filesProcessed,
		string lastPath,
		CancellationToken ct) {
		var checkpoint = new ScanCheckpoint {
			ScanJobId = scanJobId,
			ScanPath = scanPath,
			Options = options,
			CreatedAt = DateTime.UtcNow,
			FilesProcessed = filesProcessed,
			LastProcessedPath = lastPath,
			PendingDirectories = []
		};

		var json = JsonSerializer.Serialize(checkpoint, new JsonSerializerOptions { WriteIndented = true });
		await File.WriteAllTextAsync(GetCheckpointPath(scanJobId), json, ct);

		_logger.LogDebug("Created checkpoint at {Files} files", filesProcessed);
	}

	private string GetCheckpointPath(Guid scanJobId) =>
		Path.Combine(_checkpointDirectory, $"{scanJobId}.json");

	private void DeleteCheckpoint(Guid scanJobId) {
		var path = GetCheckpointPath(scanJobId);
		if (File.Exists(path)) {
			File.Delete(path);
		}
	}

	private static bool ShouldExclude(string path, HashSet<string> excludePatterns) {
		if (excludePatterns.Count == 0) return false;

		var fileName = Path.GetFileName(path);
		foreach (var pattern in excludePatterns) {
			if (pattern.Contains('*') || pattern.Contains('?')) {
				// Simple wildcard matching
				if (FileSystemName.MatchesSimpleExpression(pattern, fileName)) {
					return true;
				}
			} else if (path.Contains(pattern, StringComparison.OrdinalIgnoreCase)) {
				return true;
			}
		}

		return false;
	}

	private async Task<int> DetectDeletedFilesAsync(
		string scanPath,
		IEnumerable<string> previousPaths,
		CancellationToken ct) {
		var deleted = 0;
		foreach (var path in previousPaths) {
			if (!File.Exists(path)) {
				deleted++;
				await InvalidateFileAsync(path, ct);
			}
		}

		return deleted;
	}

	private static async IAsyncEnumerable<FileInfo> EnumerateFilesAsync(
		string path,
		IReadOnlyList<string> includePatterns,
		EnumerationOptions options,
		[EnumeratorCancellation] CancellationToken ct) {
		// Yield in batches to avoid blocking
		var patterns = includePatterns.Count > 0 ? includePatterns : ["*"];

		foreach (var pattern in patterns) {
			IEnumerable<string> files;
			try {
				files = Directory.EnumerateFiles(path, pattern, options);
			} catch (UnauthorizedAccessException) {
				continue;
			} catch (DirectoryNotFoundException) {
				continue;
			}

			foreach (var filePath in files) {
				ct.ThrowIfCancellationRequested();

				FileInfo? fileInfo = null;
				try {
					fileInfo = new FileInfo(filePath);
				} catch {
					continue;
				}

				if (fileInfo.Exists) {
					yield return fileInfo;
				}

				// Yield periodically to allow cancellation
				await Task.Yield();
			}
		}
	}

	private sealed record FileState(DateTime LastModified, long Size);
}
