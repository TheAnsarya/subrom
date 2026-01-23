using Subrom.Application.Interfaces;
using Subrom.Domain.Aggregates.Organization;

namespace Subrom.Server.Endpoints;

/// <summary>
/// ROM organization endpoints.
/// </summary>
public static class OrganizationEndpoints {
	public static IEndpointRouteBuilder MapOrganizationEndpoints(this IEndpointRouteBuilder endpoints) {
		var group = endpoints.MapGroup("/organization")
			.WithTags("Organization");

		// Get built-in templates
		group.MapGet("/templates", () => Results.Ok(OrganizationTemplate.BuiltInTemplates));

		// Validate a template string
		group.MapPost("/templates/validate", (ValidateTemplateRequest request) => {
			var folderErrors = TemplateParser.Validate(request.FolderTemplate ?? "");
			var fileErrors = TemplateParser.Validate(request.FileNameTemplate ?? "");

			var allErrors = folderErrors.Concat(fileErrors.Select(e => $"Filename: {e}")).ToList();

			return Results.Ok(new ValidateTemplateResponse {
				IsValid = allErrors.Count == 0,
				Errors = allErrors
			});
		});

		// Preview template parsing with sample data
		group.MapPost("/templates/preview", (PreviewTemplateRequest request) => {
			try {
				var context = new TemplateContext {
					Name = request.SampleName ?? "Super Mario Bros (USA)",
					Extension = request.SampleExtension ?? ".nes",
					System = request.SampleSystem ?? "Nintendo - Nintendo Entertainment System",
					SystemShort = "NES",
					Region = "USA",
					RegionShort = "U",
					Languages = "En",
					CleanName = "Super Mario Bros",
					Category = "Games"
				};

				var folderPath = TemplateParser.Parse(request.FolderTemplate, context);
				var fileName = TemplateParser.Parse(request.FileNameTemplate, context);

				return Results.Ok(new PreviewTemplateResponse {
					FolderPath = folderPath,
					FileName = fileName,
					FullPath = Path.Combine(folderPath, fileName)
				});
			} catch (Exception ex) {
				return Results.BadRequest(new { Message = ex.Message });
			}
		});

		// Plan an organization operation (dry run)
		group.MapPost("/plan", async (PlanOrganizationRequest request, IOrganizationService orgService, CancellationToken ct) => {
			try {
				var template = GetTemplate(request.TemplateId, request.CustomTemplate);
				if (template == null) {
					return Results.BadRequest(new { Message = "Invalid template" });
				}

				var orgRequest = new OrganizationRequest {
					SourcePath = request.SourcePath,
					DestinationPath = request.DestinationPath,
					Template = template,
					MoveFiles = request.MoveFiles,
					ProcessArchives = request.ProcessArchives,
					ExtractArchives = request.ExtractArchives,
					DeleteEmptyFolders = request.DeleteEmptyFolders,
					IncludePatterns = request.IncludePatterns ?? ["*.*"],
					ExcludePatterns = request.ExcludePatterns ?? []
				};

				var plan = await orgService.PlanAsync(orgRequest, ct);

				return Results.Ok(new OrganizationPlanResponse {
					PlanId = plan.Id,
					FileCount = plan.FileCount,
					TotalBytes = plan.TotalBytes,
					Warnings = plan.Warnings,
					Operations = plan.Operations.Take(100).Select(op => new FileOperationDto {
						Type = op.Type.ToString(),
						SourcePath = op.SourcePath,
						DestinationPath = op.DestinationPath,
						Size = op.Size,
						WouldOverwrite = op.WouldOverwrite
					}).ToList()
				});
			} catch (DirectoryNotFoundException ex) {
				return Results.NotFound(new { Message = ex.Message });
			} catch (Exception ex) {
				return Results.BadRequest(new { Message = ex.Message });
			}
		});

		// Execute an organization operation
		group.MapPost("/execute", async (ExecuteOrganizationRequest request, IOrganizationService orgService, CancellationToken ct) => {
			try {
				var template = GetTemplate(request.TemplateId, request.CustomTemplate);
				if (template == null) {
					return Results.BadRequest(new { Message = "Invalid template" });
				}

				var orgRequest = new OrganizationRequest {
					SourcePath = request.SourcePath,
					DestinationPath = request.DestinationPath,
					Template = template,
					MoveFiles = request.MoveFiles,
					ProcessArchives = request.ProcessArchives,
					ExtractArchives = request.ExtractArchives,
					DeleteEmptyFolders = request.DeleteEmptyFolders,
					IncludePatterns = request.IncludePatterns ?? ["*.*"],
					ExcludePatterns = request.ExcludePatterns ?? []
				};

				var result = await orgService.OrganizeAsync(orgRequest, ct);

				return Results.Ok(new OrganizationResultResponse {
					OperationId = result.OperationId,
					Success = result.Success,
					FilesProcessed = result.FilesProcessed,
					FilesSkipped = result.FilesSkipped,
					FilesFailed = result.FilesFailed,
					BytesProcessed = result.BytesProcessed,
					DurationMs = (long)result.Duration.TotalMilliseconds,
					CanRollback = result.CanRollback,
					Errors = result.Errors.Select(e => new FileOperationErrorDto {
						SourcePath = e.Operation.SourcePath,
						Message = e.Message
					}).ToList()
				});
			} catch (DirectoryNotFoundException ex) {
				return Results.NotFound(new { Message = ex.Message });
			} catch (Exception ex) {
				return Results.BadRequest(new { Message = ex.Message });
			}
		});

		// Rollback an organization operation
		group.MapPost("/{operationId:guid}/rollback", async (Guid operationId, IOrganizationService orgService, CancellationToken ct) => {
			var success = await orgService.RollbackAsync(operationId, ct);
			return success
				? Results.Ok(new { Message = "Rollback completed successfully" })
				: Results.BadRequest(new { Message = "Rollback failed or operation not found" });
		});

		// Get organization history
		group.MapGet("/history", async (int? limit, IOrganizationService orgService, CancellationToken ct) => {
			var history = await orgService.GetHistoryAsync(limit ?? 50, ct);
			return Results.Ok(history.Select(h => new OrganizationHistoryDto {
				Id = h.Id,
				PerformedAt = h.PerformedAt,
				SourcePath = h.SourcePath,
				DestinationPath = h.DestinationPath,
				TemplateName = h.TemplateName,
				WasMoveOperation = h.WasMoveOperation,
				FileCount = h.FileCount,
				TotalBytes = h.TotalBytes,
				CanRollback = h.CanRollback
			}));
		});

		// Get organization statistics
		group.MapGet("/stats", async (IOrganizationLogRepository logRepo, CancellationToken ct) => {
			var stats = await logRepo.GetStatisticsAsync(ct);
			return Results.Ok(stats);
		});

		// Get detailed operation log
		group.MapGet("/{operationId:guid}", async (Guid operationId, IOrganizationLogRepository logRepo, CancellationToken ct) => {
			var log = await logRepo.GetWithEntriesAsync(operationId, ct);
			if (log is null) {
				return Results.NotFound(new { Message = $"Operation {operationId} not found" });
			}

			return Results.Ok(new {
				log.Id,
				log.PerformedAt,
				log.SourcePath,
				log.DestinationPath,
				log.TemplateName,
				log.WasMoveOperation,
				log.IsRolledBack,
				log.RolledBackAt,
				log.Success,
				log.ErrorsJson,
				log.FilesProcessed,
				log.FilesFailed,
				log.FilesSkipped,
				EntryCount = log.Entries.Count,
				TotalBytes = log.Entries.Sum(e => e.Size),
				Entries = log.Entries.Take(200).Select(e => new {
					e.Id,
					e.SourcePath,
					e.DestinationPath,
					e.OperationType,
					e.Size,
					e.Success,
					e.ErrorMessage,
					e.Crc
				})
			});
		});

		// Get rollbackable operations
		group.MapGet("/rollbackable", async (IOrganizationLogRepository logRepo, CancellationToken ct) => {
			var logs = await logRepo.GetRollbackableAsync(ct);
			return Results.Ok(logs.Select(h => new OrganizationHistoryDto {
				Id = h.Id,
				PerformedAt = h.PerformedAt,
				SourcePath = h.SourcePath,
				DestinationPath = h.DestinationPath,
				TemplateName = h.TemplateName,
				WasMoveOperation = h.WasMoveOperation,
				FileCount = h.Entries.Count,
				TotalBytes = h.Entries.Sum(e => e.Size),
				CanRollback = true
			}));
		});

		return endpoints;
	}

	private static OrganizationTemplate? GetTemplate(Guid? templateId, CustomTemplateDto? customTemplate) {
		if (customTemplate != null) {
			return new OrganizationTemplate {
				Name = customTemplate.Name ?? "Custom",
				FolderTemplate = customTemplate.FolderTemplate,
				FileNameTemplate = customTemplate.FileNameTemplate,
				Use1G1R = customTemplate.Use1G1R,
				RegionPriority = customTemplate.RegionPriority ?? [],
				LanguagePriority = customTemplate.LanguagePriority ?? []
			};
		}

		if (templateId.HasValue) {
			return OrganizationTemplate.BuiltInTemplates.FirstOrDefault(t => t.Id == templateId.Value);
		}

		// Default to No-Intro Style
		return OrganizationTemplate.NoIntroStyle;
	}
}

// Request/Response DTOs

public record ValidateTemplateRequest {
	public string? FolderTemplate { get; init; }
	public string? FileNameTemplate { get; init; }
}

public record ValidateTemplateResponse {
	public bool IsValid { get; init; }
	public IReadOnlyList<string> Errors { get; init; } = [];
}

public record PreviewTemplateRequest {
	public required string FolderTemplate { get; init; }
	public required string FileNameTemplate { get; init; }
	public string? SampleName { get; init; }
	public string? SampleExtension { get; init; }
	public string? SampleSystem { get; init; }
}

public record PreviewTemplateResponse {
	public required string FolderPath { get; init; }
	public required string FileName { get; init; }
	public required string FullPath { get; init; }
}

public record PlanOrganizationRequest {
	public required string SourcePath { get; init; }
	public required string DestinationPath { get; init; }
	public Guid? TemplateId { get; init; }
	public CustomTemplateDto? CustomTemplate { get; init; }
	public bool MoveFiles { get; init; } = true;
	public bool ProcessArchives { get; init; } = true;
	public bool ExtractArchives { get; init; } = false;
	public bool DeleteEmptyFolders { get; init; } = true;
	public IReadOnlyList<string>? IncludePatterns { get; init; }
	public IReadOnlyList<string>? ExcludePatterns { get; init; }
}

public record ExecuteOrganizationRequest {
	public required string SourcePath { get; init; }
	public required string DestinationPath { get; init; }
	public Guid? TemplateId { get; init; }
	public CustomTemplateDto? CustomTemplate { get; init; }
	public bool MoveFiles { get; init; } = true;
	public bool ProcessArchives { get; init; } = true;
	public bool ExtractArchives { get; init; } = false;
	public bool DeleteEmptyFolders { get; init; } = true;
	public IReadOnlyList<string>? IncludePatterns { get; init; }
	public IReadOnlyList<string>? ExcludePatterns { get; init; }
}

public record CustomTemplateDto {
	public string? Name { get; init; }
	public required string FolderTemplate { get; init; }
	public required string FileNameTemplate { get; init; }
	public bool Use1G1R { get; init; }
	public IReadOnlyList<string>? RegionPriority { get; init; }
	public IReadOnlyList<string>? LanguagePriority { get; init; }
}

public record OrganizationPlanResponse {
	public Guid PlanId { get; init; }
	public int FileCount { get; init; }
	public long TotalBytes { get; init; }
	public IReadOnlyList<string> Warnings { get; init; } = [];
	public IReadOnlyList<FileOperationDto> Operations { get; init; } = [];
}

public record FileOperationDto {
	public required string Type { get; init; }
	public required string SourcePath { get; init; }
	public required string DestinationPath { get; init; }
	public long Size { get; init; }
	public bool WouldOverwrite { get; init; }
}

public record OrganizationResultResponse {
	public Guid OperationId { get; init; }
	public bool Success { get; init; }
	public int FilesProcessed { get; init; }
	public int FilesSkipped { get; init; }
	public int FilesFailed { get; init; }
	public long BytesProcessed { get; init; }
	public long DurationMs { get; init; }
	public bool CanRollback { get; init; }
	public IReadOnlyList<FileOperationErrorDto> Errors { get; init; } = [];
}

public record FileOperationErrorDto {
	public required string SourcePath { get; init; }
	public required string Message { get; init; }
}

public record OrganizationHistoryDto {
	public Guid Id { get; init; }
	public DateTime PerformedAt { get; init; }
	public required string SourcePath { get; init; }
	public required string DestinationPath { get; init; }
	public required string TemplateName { get; init; }
	public bool WasMoveOperation { get; init; }
	public int FileCount { get; init; }
	public long TotalBytes { get; init; }
	public bool CanRollback { get; init; }
}
