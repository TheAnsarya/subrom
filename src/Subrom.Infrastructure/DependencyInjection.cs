using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Subrom.Application.Interfaces;
using Subrom.Infrastructure.Parsing;
using Subrom.Infrastructure.Persistence;
using Subrom.Infrastructure.Persistence.Repositories;
using Subrom.Infrastructure.Services;

namespace Subrom.Infrastructure;

/// <summary>
/// Extension methods for registering Infrastructure services with DI.
/// </summary>
public static class DependencyInjection {
	/// <summary>
	/// Adds Infrastructure layer services to the DI container.
	/// </summary>
	/// <param name="services">The service collection.</param>
	/// <param name="connectionString">SQLite connection string.</param>
	/// <returns>The service collection for chaining.</returns>
	public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString) {
		// Register DbContext
		services.AddDbContext<SubromDbContext>(options => {
			options.UseSqlite(connectionString, sqliteOptions => {
				sqliteOptions.CommandTimeout(30);
			});
		});

		// Register UnitOfWork
		services.AddScoped<IUnitOfWork, UnitOfWork>();

		// Register Repositories
		services.AddScoped<IDatFileRepository, DatFileRepository>();
		services.AddScoped<IDriveRepository, DriveRepository>();
		services.AddScoped<IRomFileRepository, RomFileRepository>();
		services.AddScoped<IScanJobRepository, ScanJobRepository>();

		// Register Services
		services.AddSingleton<IHashService, HashService>();

		// Register Parsers
		services.AddSingleton<IDatParser, LogiqxDatParser>();
		services.AddSingleton<IDatParserFactory, DatParserFactory>();

		return services;
	}

	/// <summary>
	/// Ensures the database is created and migrated.
	/// </summary>
	public static async Task InitializeDatabaseAsync(this IServiceProvider services) {
		using var scope = services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<SubromDbContext>();
		await context.Database.EnsureCreatedAsync();
	}
}
