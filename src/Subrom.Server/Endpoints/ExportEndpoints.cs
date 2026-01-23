using Subrom.Application.Services;
using Subrom.Domain.Aggregates.Storage;

namespace Subrom.Server.Endpoints;

/// <summary>
/// Export endpoints for CSV/JSON data export.
/// </summary>
public static class ExportEndpoints {
	public static IEndpointRouteBuilder MapExportEndpoints(this IEndpointRouteBuilder endpoints) {
		var group = endpoints.MapGroup("/export")
			.WithTags("Export");

		// Export all ROMs as CSV
		group.MapGet("/roms/csv", async (
			ExportService exportService,
			Guid? driveId,
			bool? includeHashes,
			CancellationToken ct) => {
				var options = new ExportOptions {
					Format = ExportFormat.Csv,
					IncludeHashes = includeHashes ?? true
				};

				var csv = await exportService.ExportRomFilesAsync(options, driveId, ct);

				return Results.Content(csv, "text/csv", System.Text.Encoding.UTF8);
			})
		.WithName("ExportRomsCsv")
		.WithOpenApi(operation => {
			operation.Summary = "Export ROMs as CSV";
			operation.Description = "Exports all ROM files to CSV format with optional filtering by drive.";
			return operation;
		})
		.Produces<string>(200, "text/csv");

		// Export all ROMs as JSON
		group.MapGet("/roms/json", async (
			ExportService exportService,
			Guid? driveId,
			bool? includeHashes,
			bool? pretty,
			CancellationToken ct) => {
				var options = new ExportOptions {
					Format = pretty == true ? ExportFormat.JsonPretty : ExportFormat.Json,
					IncludeHashes = includeHashes ?? true
				};

				var json = await exportService.ExportRomFilesAsync(options, driveId, ct);

				return Results.Content(json, "application/json", System.Text.Encoding.UTF8);
			})
		.WithName("ExportRomsJson")
		.WithOpenApi(operation => {
			operation.Summary = "Export ROMs as JSON";
			operation.Description = "Exports all ROM files to JSON format with optional filtering by drive.";
			return operation;
		})
		.Produces<string>(200, "application/json");

		// Export by verification status
		group.MapGet("/roms/by-status/{status}", async (
			VerificationStatus status,
			ExportService exportService,
			string? format,
			bool? includeHashes,
			CancellationToken ct) => {
				var exportFormat = format?.ToLowerInvariant() switch {
					"json" => ExportFormat.Json,
					"json-pretty" => ExportFormat.JsonPretty,
					_ => ExportFormat.Csv
				};

				var options = new ExportOptions {
					Format = exportFormat,
					IncludeHashes = includeHashes ?? true
				};

				var content = await exportService.ExportByVerificationStatusAsync(status, options, ct);
				var contentType = exportFormat == ExportFormat.Csv ? "text/csv" : "application/json";

				return Results.Content(content, contentType, System.Text.Encoding.UTF8);
			})
		.WithName("ExportRomsByStatus")
		.WithOpenApi(operation => {
			operation.Summary = "Export ROMs by verification status";
			operation.Description = "Exports ROM files filtered by verification status (Verified, Unverified, Unknown, BadDump).";
			return operation;
		});

		// Export collection summary
		group.MapGet("/summary", async (
			ExportService exportService,
			bool? pretty,
			CancellationToken ct) => {
				var options = new ExportOptions {
					Format = pretty == true ? ExportFormat.JsonPretty : ExportFormat.Json
				};

				var json = await exportService.ExportCollectionSummaryAsync(options, ct);

				return Results.Content(json, "application/json", System.Text.Encoding.UTF8);
			})
		.WithName("ExportCollectionSummary")
		.WithOpenApi(operation => {
			operation.Summary = "Export collection summary";
			operation.Description = "Exports a statistical summary of the entire ROM collection including verification stats and drive breakdown.";
			return operation;
		})
		.Produces<string>(200, "application/json");

		// Download export as file
		group.MapGet("/download/roms", async (
			ExportService exportService,
			string? format,
			Guid? driveId,
			bool? includeHashes,
			CancellationToken ct) => {
				var exportFormat = format?.ToLowerInvariant() switch {
					"json" => ExportFormat.Json,
					"json-pretty" => ExportFormat.JsonPretty,
					_ => ExportFormat.Csv
				};

				var options = new ExportOptions {
					Format = exportFormat,
					IncludeHashes = includeHashes ?? true
				};

				var content = await exportService.ExportRomFilesAsync(options, driveId, ct);
				var extension = exportFormat == ExportFormat.Csv ? "csv" : "json";
				var contentType = exportFormat == ExportFormat.Csv ? "text/csv" : "application/json";
				var fileName = $"subrom-export-{DateTime.UtcNow:yyyy-MM-dd-HHmmss}.{extension}";

				return Results.File(
					System.Text.Encoding.UTF8.GetBytes(content),
					contentType,
					fileName);
			})
		.WithName("DownloadRomsExport")
		.WithOpenApi(operation => {
			operation.Summary = "Download ROMs export as file";
			operation.Description = "Downloads the ROM export as a file with Content-Disposition header.";
			return operation;
		});

		return endpoints;
	}
}
