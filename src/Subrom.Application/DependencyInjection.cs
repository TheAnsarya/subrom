using Microsoft.Extensions.DependencyInjection;
using Subrom.Application.Interfaces;
using Subrom.Application.Services;

namespace Subrom.Application;

/// <summary>
/// Extension methods for registering Application services with DI.
/// </summary>
public static class DependencyInjection {
	/// <summary>
	/// Adds Application layer services to the DI container.
	/// </summary>
	/// <param name="services">The service collection.</param>
	/// <returns>The service collection for chaining.</returns>
	public static IServiceCollection AddApplication(this IServiceCollection services) {
		// Register Application Services
		services.AddScoped<DatFileService>();
		services.AddScoped<IDatCollectionService, DatCollectionService>();
		services.AddScoped<DriveService>();
		services.AddScoped<ScanService>();
		services.AddScoped<VerificationService>();

		return services;
	}
}
