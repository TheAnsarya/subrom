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
			Version = "1.0.0-alpha",
			DotNetVersion = Environment.Version.ToString(),
			Os = Environment.OSVersion.ToString()
		}).WithTags("System");

		// DAT files endpoints
		api.MapDatFileEndpoints();

		// Drives endpoints
		api.MapDriveEndpoints();

		// Scan endpoints
		api.MapScanEndpoints();

		// ROM files endpoints
		api.MapRomFileEndpoints();

		return endpoints;
	}
}
