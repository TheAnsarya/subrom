namespace Subrom.Server.Endpoints;

/// <summary>
/// Extension methods for mapping API endpoints.
/// </summary>
public static class EndpointExtensions {
	/// <summary>
	/// Maps all Subrom API endpoints.
	/// </summary>
	public static IEndpointRouteBuilder MapSubromEndpoints(this IEndpointRouteBuilder endpoints) {
		var api = endpoints.MapGroup("/api");

		// Version endpoint
		api.MapGet("/version", () => new {
			Version = "1.0.0",
			DotNetVersion = Environment.Version.ToString(),
			Os = Environment.OSVersion.ToString()
		}).WithTags("System");

		// DAT files endpoints
		api.MapDatFileEndpoints();

		// DAT provider endpoints
		api.MapDatProviderEndpoints();

		// Drives endpoints
		api.MapDriveEndpoints();

		// Scan endpoints
		api.MapScanEndpoints();

		// ROM files endpoints
		api.MapRomFileEndpoints();

		// Verification endpoints
		api.MapVerificationEndpoints();

		// Organization endpoints
		api.MapOrganizationEndpoints();

		// Storage monitor endpoints
		api.MapStorageEndpoints();

		// Settings endpoints
		api.MapSettingsEndpoints();

		// Streaming/cursor-based pagination endpoints
		endpoints.MapStreamingEndpoints();

		return endpoints;
	}
}
