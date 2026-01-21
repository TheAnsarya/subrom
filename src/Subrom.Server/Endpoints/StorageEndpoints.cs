using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Subrom.Application.Interfaces;

namespace Subrom.Server.Endpoints;

/// <summary>
/// API endpoints for storage monitoring and analysis.
/// Extends the base DriveEndpoints with monitoring capabilities.
/// </summary>
public static class StorageEndpoints {
	public static void MapStorageEndpoints(this IEndpointRouteBuilder app) {
		var group = app.MapGroup("/api/storage").WithTags("Storage");

		// Summary and monitoring
		group.MapGet("/summary", GetStorageSummaryAsync)
			.WithName("GetStorageSummary")
			.WithDescription("Gets a summary of all storage");

		group.MapGet("/drives/{id:guid}/status", GetDriveStatusAsync)
			.WithName("GetDriveStatus")
			.WithDescription("Gets detailed status of a specific drive");

		group.MapPost("/refresh", RefreshAllDrivesAsync)
			.WithName("RefreshAllDrives")
			.WithDescription("Refreshes status of all drives");

		// ROM analysis
		group.MapGet("/roms/offline", GetOfflineRomsAsync)
			.WithName("GetOfflineRoms")
			.WithDescription("Gets all ROMs on offline drives");

		group.MapGet("/duplicates", FindDuplicatesAsync)
			.WithName("FindDuplicates")
			.WithDescription("Finds duplicate ROMs across drives");

		group.MapGet("/drives/{id:guid}/relocations", GetRelocationSuggestionsAsync)
			.WithName("GetRelocationSuggestions")
			.WithDescription("Gets relocation suggestions for a drive's ROMs");
	}

	private static async Task<IResult> GetStorageSummaryAsync(IStorageMonitorService monitorService) {
		var summary = await monitorService.GetSummaryAsync();
		return Results.Ok(summary);
	}

	private static async Task<IResult> GetDriveStatusAsync(
		Guid id,
		IStorageMonitorService monitorService) {
		try {
			var status = await monitorService.GetDriveStatusAsync(id);
			return Results.Ok(status);
		} catch (KeyNotFoundException) {
			return Results.NotFound();
		}
	}

	private static async Task<IResult> RefreshAllDrivesAsync(IStorageMonitorService monitorService) {
		var changes = await monitorService.RefreshAllDrivesAsync();
		return Results.Ok(changes);
	}

	private static async Task<IResult> GetOfflineRomsAsync(IStorageMonitorService monitorService) {
		var roms = await monitorService.GetOfflineRomsAsync();
		return Results.Ok(roms);
	}

	private static async Task<IResult> FindDuplicatesAsync(IStorageMonitorService monitorService) {
		var duplicates = await monitorService.FindDuplicatesAsync();
		return Results.Ok(new {
			groups = duplicates,
			totalGroups = duplicates.Count,
			wastedSpace = duplicates.Sum(g => g.WastedSpace)
		});
	}

	private static async Task<IResult> GetRelocationSuggestionsAsync(
		Guid id,
		Guid? targetDriveId,
		IStorageMonitorService monitorService) {
		try {
			var suggestions = await monitorService.GetRelocationSuggestionsAsync(id, targetDriveId);
			return Results.Ok(suggestions);
		} catch (KeyNotFoundException) {
			return Results.NotFound();
		}
	}
}
