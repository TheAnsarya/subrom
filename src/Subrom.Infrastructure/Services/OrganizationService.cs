using Microsoft.Extensions.Logging;
using Subrom.Application.Interfaces;
using Subrom.Domain.Aggregates.Organization;

namespace Subrom.Infrastructure.Services;

/// <summary>
/// Service for organizing ROMs according to templates.
/// </summary>
public class OrganizationService : IOrganizationService {
	private readonly ILogger<OrganizationService> _logger;
	private readonly List<OrganizationOperation> _history = [];

	public OrganizationService(ILogger<OrganizationService> logger) {
		_logger = logger;
	}

	/// <inheritdoc />
	public async Task<OrganizationPlan> PlanAsync(OrganizationRequest request, CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(request);

		_logger.LogInformation("Planning organization from {Source} to {Destination} using template '{Template}'",
			request.SourcePath, request.DestinationPath, request.Template.Name);

		// Validate request
		if (!Directory.Exists(request.SourcePath)) {
			throw new DirectoryNotFoundException($"Source directory not found: {request.SourcePath}");
		}

		var operations = new List<FileOperation>();
		var warnings = new List<string>();
		long totalBytes = 0;

		// Get all matching files
		var files = GetMatchingFiles(request);

		foreach (var filePath in files) {
			cancellationToken.ThrowIfCancellationRequested();

			try {
				var fileInfo = new FileInfo(filePath);
				var context = BuildContext(filePath, request);

				// Parse the template to get destination path
				var folderPath = TemplateParser.Parse(request.Template.FolderTemplate, context);
				var fileName = TemplateParser.Parse(request.Template.FileNameTemplate, context);

				var destinationPath = Path.Combine(request.DestinationPath, folderPath, fileName);

				// Normalize path separators
				destinationPath = Path.GetFullPath(destinationPath);

				var destExists = File.Exists(destinationPath);
				var wouldOverwrite = destExists && !string.Equals(filePath, destinationPath, StringComparison.OrdinalIgnoreCase);

				var operation = new FileOperation {
					Type = request.MoveFiles ? FileOperationType.Move : FileOperationType.Copy,
					SourcePath = filePath,
					DestinationPath = destinationPath,
					Size = fileInfo.Length,
					Context = context,
					DestinationExists = destExists,
					WouldOverwrite = wouldOverwrite
				};

				// Check if we would overwrite
				if (wouldOverwrite) {
					warnings.Add($"File would be overwritten: {destinationPath}");
				}

				operations.Add(operation);
				totalBytes += fileInfo.Length;
			} catch (Exception ex) {
				_logger.LogWarning(ex, "Failed to plan operation for file: {Path}", filePath);
				warnings.Add($"Could not process file: {filePath} - {ex.Message}");
			}
		}

		_logger.LogInformation("Plan created with {Count} operations, {Bytes} bytes total",
			operations.Count, totalBytes);

		return new OrganizationPlan {
			Request = request,
			Operations = operations,
			TotalBytes = totalBytes,
			Warnings = warnings
		};
	}

	/// <inheritdoc />
	public async Task<OrganizationResult> ExecuteAsync(OrganizationPlan plan, CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(plan);

		var startTime = DateTime.UtcNow;
		var rollbackData = new List<RollbackEntry>();
		var errors = new List<FileOperationError>();
		var processed = 0;
		var skipped = 0;
		long bytesProcessed = 0;

		_logger.LogInformation("Executing organization plan {PlanId} with {Count} operations",
			plan.Id, plan.Operations.Count);

		foreach (var operation in plan.Operations) {
			cancellationToken.ThrowIfCancellationRequested();

			try {
				switch (operation.Type) {
					case FileOperationType.Move:
						await ExecuteMoveAsync(operation, rollbackData);
						processed++;
						bytesProcessed += operation.Size;
						break;

					case FileOperationType.Copy:
						await ExecuteCopyAsync(operation);
						processed++;
						bytesProcessed += operation.Size;
						break;

					case FileOperationType.Skip:
						skipped++;
						break;

					case FileOperationType.Extract:
						// TODO: Implement archive extraction
						skipped++;
						break;
				}
			} catch (Exception ex) {
				_logger.LogError(ex, "Failed to execute operation for {Source}", operation.SourcePath);
				errors.Add(new FileOperationError {
					Operation = operation,
					Message = ex.Message,
					ExceptionType = ex.GetType().Name
				});
			}
		}

		var endTime = DateTime.UtcNow;

		// Delete empty source folders if requested
		if (plan.Request.DeleteEmptyFolders && plan.Request.MoveFiles) {
			DeleteEmptyFolders(plan.Request.SourcePath);
		}

		// Record history
		var historyEntry = new OrganizationOperation {
			Id = Guid.NewGuid(),
			PerformedAt = startTime,
			SourcePath = plan.Request.SourcePath,
			DestinationPath = plan.Request.DestinationPath,
			TemplateName = plan.Request.Template.Name,
			WasMoveOperation = plan.Request.MoveFiles,
			FileCount = processed,
			TotalBytes = bytesProcessed,
			CanRollback = plan.Request.MoveFiles && rollbackData.Count > 0,
			RollbackData = rollbackData
		};
		_history.Insert(0, historyEntry);

		_logger.LogInformation("Organization complete: {Processed} processed, {Skipped} skipped, {Errors} errors",
			processed, skipped, errors.Count);

		return new OrganizationResult {
			OperationId = historyEntry.Id,
			Success = errors.Count == 0,
			FilesProcessed = processed,
			FilesSkipped = skipped,
			FilesFailed = errors.Count,
			BytesProcessed = bytesProcessed,
			Errors = errors,
			Duration = endTime - startTime,
			StartedAt = startTime,
			CompletedAt = endTime,
			CanRollback = historyEntry.CanRollback
		};
	}

	/// <inheritdoc />
	public async Task<OrganizationResult> OrganizeAsync(OrganizationRequest request, CancellationToken cancellationToken = default) {
		var plan = await PlanAsync(request, cancellationToken);
		return await ExecuteAsync(plan, cancellationToken);
	}

	/// <inheritdoc />
	public async Task<bool> RollbackAsync(Guid operationId, CancellationToken cancellationToken = default) {
		var operation = _history.FirstOrDefault(h => h.Id == operationId);

		if (operation == null) {
			_logger.LogWarning("Operation not found for rollback: {Id}", operationId);
			return false;
		}

		if (!operation.CanRollback || operation.RollbackData == null) {
			_logger.LogWarning("Operation cannot be rolled back: {Id}", operationId);
			return false;
		}

		_logger.LogInformation("Rolling back operation {Id} with {Count} files", operationId, operation.RollbackData.Count);

		var success = true;

		foreach (var entry in operation.RollbackData) {
			cancellationToken.ThrowIfCancellationRequested();

			try {
				if (entry.WasMoved && File.Exists(entry.CurrentPath)) {
					// Ensure original directory exists
					var dir = Path.GetDirectoryName(entry.OriginalPath);
					if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) {
						Directory.CreateDirectory(dir);
					}

					File.Move(entry.CurrentPath, entry.OriginalPath, overwrite: false);
					_logger.LogDebug("Restored file: {Original}", entry.OriginalPath);
				}
			} catch (Exception ex) {
				_logger.LogError(ex, "Failed to rollback file: {Path}", entry.CurrentPath);
				success = false;
			}
		}

		// Clean up empty folders in destination
		if (success) {
			DeleteEmptyFolders(operation.DestinationPath);
		}

		return success;
	}

	/// <inheritdoc />
	public Task<IReadOnlyList<OrganizationOperation>> GetHistoryAsync(int limit = 100, CancellationToken cancellationToken = default) {
		var result = _history.Take(limit).ToList();
		return Task.FromResult<IReadOnlyList<OrganizationOperation>>(result);
	}

	private IEnumerable<string> GetMatchingFiles(OrganizationRequest request) {
		var allFiles = Directory.EnumerateFiles(request.SourcePath, "*.*", SearchOption.AllDirectories);

		foreach (var file in allFiles) {
			var fileName = Path.GetFileName(file);

			// Check include patterns
			var included = request.IncludePatterns.Count == 0 ||
				request.IncludePatterns.Any(p => MatchesPattern(fileName, p));

			// Check exclude patterns
			var excluded = request.ExcludePatterns.Any(p => MatchesPattern(fileName, p));

			if (included && !excluded) {
				yield return file;
			}
		}
	}

	private static bool MatchesPattern(string fileName, string pattern) {
		// Simple wildcard matching
		if (pattern == "*.*" || pattern == "*") {
			return true;
		}

		var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
			.Replace(@"\*", ".*")
			.Replace(@"\?", ".") + "$";

		return System.Text.RegularExpressions.Regex.IsMatch(fileName, regexPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
	}

	private static TemplateContext BuildContext(string filePath, OrganizationRequest request) {
		var fileName = Path.GetFileNameWithoutExtension(filePath);
		var extension = Path.GetExtension(filePath);

		var region = TemplateContext.ExtractRegion(fileName);
		var languages = TemplateContext.ExtractLanguages(fileName);
		var cleanName = TemplateContext.ExtractCleanName(fileName);

		// Try to infer system from DAT or folder structure
		var system = InferSystem(filePath, request);
		var systemShort = GetShortSystemName(system);

		return new TemplateContext {
			Name = fileName,
			Extension = extension,
			System = system ?? "Unknown",
			SystemShort = systemShort ?? "UNK",
			Region = region,
			RegionShort = TemplateContext.GetShortRegion(region),
			Languages = languages,
			CleanName = cleanName,
			Category = "Games"
		};
	}

	private static string? InferSystem(string filePath, OrganizationRequest request) {
		// Try to get system from parent folder name
		var parentDir = Path.GetDirectoryName(filePath);
		if (!string.IsNullOrEmpty(parentDir)) {
			var parentName = new DirectoryInfo(parentDir).Name;

			// Check if parent folder looks like a system name
			if (parentName.Contains(" - ") || KnownSystems.ContainsKey(parentName.ToLowerInvariant())) {
				return parentName;
			}
		}

		// Try extension mapping
		var ext = Path.GetExtension(filePath).ToLowerInvariant();
		return ExtensionToSystem.TryGetValue(ext, out var system) ? system : null;
	}

	private static string? GetShortSystemName(string? system) {
		if (string.IsNullOrEmpty(system)) {
			return null;
		}

		var lower = system.ToLowerInvariant();
		return KnownSystems.TryGetValue(lower, out var shortName) ? shortName : system[..Math.Min(4, system.Length)].ToUpperInvariant();
	}

	private static readonly Dictionary<string, string> KnownSystems = new(StringComparer.OrdinalIgnoreCase) {
		["nintendo - nintendo entertainment system"] = "NES",
		["nintendo - super nintendo entertainment system"] = "SNES",
		["nintendo - game boy"] = "GB",
		["nintendo - game boy color"] = "GBC",
		["nintendo - game boy advance"] = "GBA",
		["nintendo - nintendo 64"] = "N64",
		["nintendo - nintendo ds"] = "NDS",
		["nintendo - nintendo 3ds"] = "3DS",
		["sega - master system - mark iii"] = "SMS",
		["sega - mega drive - genesis"] = "MD",
		["sega - game gear"] = "GG",
		["sega - saturn"] = "SS",
		["sega - dreamcast"] = "DC",
		["sony - playstation"] = "PS1",
		["sony - playstation 2"] = "PS2",
		["sony - playstation portable"] = "PSP",
		["atari - 2600"] = "2600",
		["atari - 7800"] = "7800",
		["atari - lynx"] = "LYNX",
		["nec - pc engine - turbografx-16"] = "PCE",
		["snk - neo geo pocket"] = "NGP",
		["snk - neo geo pocket color"] = "NGPC"
	};

	private static readonly Dictionary<string, string> ExtensionToSystem = new(StringComparer.OrdinalIgnoreCase) {
		[".nes"] = "Nintendo - Nintendo Entertainment System",
		[".sfc"] = "Nintendo - Super Nintendo Entertainment System",
		[".smc"] = "Nintendo - Super Nintendo Entertainment System",
		[".gb"] = "Nintendo - Game Boy",
		[".gbc"] = "Nintendo - Game Boy Color",
		[".gba"] = "Nintendo - Game Boy Advance",
		[".n64"] = "Nintendo - Nintendo 64",
		[".z64"] = "Nintendo - Nintendo 64",
		[".nds"] = "Nintendo - Nintendo DS",
		[".3ds"] = "Nintendo - Nintendo 3DS",
		[".sms"] = "Sega - Master System - Mark III",
		[".md"] = "Sega - Mega Drive - Genesis",
		[".gen"] = "Sega - Mega Drive - Genesis",
		[".gg"] = "Sega - Game Gear",
		[".pce"] = "NEC - PC Engine - TurboGrafx-16",
		[".a26"] = "Atari - 2600",
		[".a78"] = "Atari - 7800",
		[".lnx"] = "Atari - Lynx",
		[".ngp"] = "SNK - Neo Geo Pocket",
		[".ngc"] = "SNK - Neo Geo Pocket Color"
	};

	private async Task ExecuteMoveAsync(FileOperation operation, List<RollbackEntry> rollbackData) {
		// Ensure destination directory exists
		var destDir = Path.GetDirectoryName(operation.DestinationPath);
		if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir)) {
			Directory.CreateDirectory(destDir);
		}

		// Move file
		File.Move(operation.SourcePath, operation.DestinationPath, overwrite: false);

		// Record for rollback
		rollbackData.Add(new RollbackEntry {
			CurrentPath = operation.DestinationPath,
			OriginalPath = operation.SourcePath,
			WasMoved = true
		});

		_logger.LogDebug("Moved: {Source} -> {Dest}", operation.SourcePath, operation.DestinationPath);

		await Task.CompletedTask;
	}

	private async Task ExecuteCopyAsync(FileOperation operation) {
		// Ensure destination directory exists
		var destDir = Path.GetDirectoryName(operation.DestinationPath);
		if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir)) {
			Directory.CreateDirectory(destDir);
		}

		// Copy file
		File.Copy(operation.SourcePath, operation.DestinationPath, overwrite: false);

		_logger.LogDebug("Copied: {Source} -> {Dest}", operation.SourcePath, operation.DestinationPath);

		await Task.CompletedTask;
	}

	private void DeleteEmptyFolders(string path) {
		if (!Directory.Exists(path)) {
			return;
		}

		foreach (var dir in Directory.GetDirectories(path, "*", SearchOption.AllDirectories)
			.OrderByDescending(d => d.Length)) {
			try {
				if (!Directory.EnumerateFileSystemEntries(dir).Any()) {
					Directory.Delete(dir);
					_logger.LogDebug("Deleted empty folder: {Path}", dir);
				}
			} catch (Exception ex) {
				_logger.LogWarning(ex, "Failed to delete empty folder: {Path}", dir);
			}
		}
	}
}
