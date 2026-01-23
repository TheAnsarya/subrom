using Subrom.Application.Interfaces;
using Subrom.Domain.Aggregates.DatFiles;

namespace Subrom.Server.Endpoints;

/// <summary>
/// DAT provider endpoints for discovering and downloading DATs from external sources.
/// </summary>
public static class DatProviderEndpoints {
	public static IEndpointRouteBuilder MapDatProviderEndpoints(this IEndpointRouteBuilder endpoints) {
		var group = endpoints.MapGroup("/dat-providers")
			.WithTags("DAT Providers");

		// Get all registered providers
		group.MapGet("/", async (IEnumerable<IDatProvider> providers) => {
			var providerInfo = providers.Select(p => new {
				Provider = p.ProviderType.ToString(),
				Type = p.ProviderType
			}).ToList();

			return Results.Ok(providerInfo);
		})
		.WithName("GetProviders")
		.WithDescription("Lists all registered DAT providers (No-Intro, TOSEC, etc.)");

		// List available DATs from a provider
		group.MapGet("/{provider}/available", async (
			DatProvider provider,
			IEnumerable<IDatProvider> providers,
			CancellationToken ct) => {
				var datProvider = providers.FirstOrDefault(p => p.ProviderType == provider);
				if (datProvider is null) {
					return Results.NotFound(new {
						error = $"Provider '{provider}' not found",
						availableProviders = providers.Select(p => p.ProviderType.ToString()).ToArray()
					});
				}

				try {
					var available = await datProvider.ListAvailableAsync(ct);
					return Results.Ok(new {
						provider = provider.ToString(),
						count = available.Count,
						dats = available
					});
				} catch (NotSupportedException ex) {
					return Results.Problem(
						detail: ex.Message,
						statusCode: 503,
						title: "Provider download disabled"
					);
				} catch (Exception ex) {
					return Results.Problem(
						detail: ex.Message,
						title: $"Failed to list DATs from {provider}"
					);
				}
			})
		.WithName("ListAvailableDats")
		.WithDescription("Lists all available DATs from a specific provider");

		// Download a DAT from a provider
		group.MapGet("/{provider}/download/{identifier}", async (
			DatProvider provider,
			string identifier,
			IEnumerable<IDatProvider> providers,
			CancellationToken ct) => {
				var datProvider = providers.FirstOrDefault(p => p.ProviderType == provider);
				if (datProvider is null) {
					return Results.NotFound(new {
						error = $"Provider '{provider}' not found"
					});
				}

				if (!datProvider.SupportsIdentifier(identifier)) {
					return Results.BadRequest(new {
						error = "Identifier not supported by provider",
						identifier,
						provider = provider.ToString()
					});
				}

				try {
					var stream = await datProvider.DownloadDatAsync(identifier, ct);
					return Results.Stream(stream, "application/xml", $"{identifier}.dat");
				} catch (NotSupportedException ex) {
					return Results.Problem(
						detail: ex.Message,
						statusCode: 503,
						title: "Provider download disabled"
					);
				} catch (FileNotFoundException ex) {
					return Results.NotFound(new {
						error = "DAT file not found",
						message = ex.Message,
						identifier
					});
				} catch (Exception ex) {
					return Results.Problem(
						detail: ex.Message,
						title: $"Failed to download DAT {identifier} from {provider}"
					);
				}
			})
		.WithName("DownloadDat")
		.WithDescription("Downloads a specific DAT file from a provider");

		// Sync DATs from a specific provider
		group.MapPost("/{provider}/sync", async (
			DatProvider provider,
			bool forceRefresh,
			IDatCollectionService datCollectionService,
			CancellationToken ct) => {
				try {
					var updated = await datCollectionService.SyncProviderAsync(
						provider,
						forceRefresh,
						progress: null,
						ct);

					return Results.Ok(new {
						provider = provider.ToString(),
						updated,
						message = $"Synchronized {updated} DATs from {provider}"
					});
				} catch (NotSupportedException ex) {
					return Results.Problem(
						detail: ex.Message,
						statusCode: 503,
						title: "Provider sync disabled"
					);
				} catch (Exception ex) {
					return Results.Problem(
						detail: ex.Message,
						title: $"Failed to sync DATs from {provider}"
					);
				}
			})
		.WithName("SyncProvider")
		.WithDescription("Synchronizes DATs from a specific provider");

		// Sync all providers
		group.MapPost("/sync-all", async (
			IDatCollectionService datCollectionService,
			CancellationToken ct) => {
				try {
					var report = await datCollectionService.SyncAllAsync(progress: null, ct);

					return Results.Ok(new {
						startedAt = report.StartedAt,
						completedAt = report.CompletedAt,
						duration = report.Duration.TotalSeconds,
						providersProcessed = report.ProvidersProcessed,
						datsUpdated = report.DatsUpdated,
						datsAdded = report.DatsAdded,
						datsSkipped = report.DatsSkipped,
						errors = report.Errors,
						errorMessages = report.ErrorMessages
					});
				} catch (Exception ex) {
					return Results.Problem(
						detail: ex.Message,
						title: "Failed to sync DATs from all providers"
					);
				}
			})
		.WithName("SyncAllProviders")
		.WithDescription("Synchronizes DATs from all registered providers");

		return endpoints;
	}
}
