using Microsoft.EntityFrameworkCore;
using Serilog;
using Subrom.Application;
using Subrom.Infrastructure;
using Subrom.Infrastructure.Persistence;
using Subrom.Infrastructure.Platform;
using Subrom.Server.BackgroundServices;
using Subrom.Server.Endpoints;
using Subrom.Server.Hubs;

// Ensure data directories exist
PlatformHelper.EnsureDataDirectoryExists();
PlatformHelper.EnsureLogDirectoryExists();

// Configure Serilog early for startup logging
Log.Logger = new LoggerConfiguration()
	.WriteTo.Console()
	.CreateBootstrapLogger();

try {
	Log.Information("Starting Subrom server on {Platform}...", PlatformHelper.PlatformName);

	var builder = WebApplication.CreateBuilder(args);

	// Configure Serilog
	builder.Host.UseSerilog((context, services, configuration) => configuration
		.ReadFrom.Configuration(context.Configuration)
		.ReadFrom.Services(services)
		.Enrich.FromLogContext()
		.WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
		.WriteTo.File(
			Path.Combine(PlatformHelper.GetLogDirectory(), "subrom-.log"),
			rollingInterval: RollingInterval.Day,
			retainedFileCountLimit: 30));

	// Configure services
	ConfigureServices(builder.Services, builder.Configuration);

	var app = builder.Build();

	// Configure HTTP pipeline
	ConfigureApp(app);

	// Initialize database
	await InitializeDatabaseAsync(app);

	Log.Information("Subrom server started on port 52100");
	await app.RunAsync();
}
catch (Exception ex) {
	Log.Fatal(ex, "Subrom server terminated unexpectedly");
	throw;
}
finally {
	await Log.CloseAndFlushAsync();
}

static void ConfigureServices(IServiceCollection services, IConfiguration configuration) {
	// Database path - cross-platform
	var dbPath = PlatformHelper.GetDefaultDatabasePath();
	var connectionString = $"Data Source={dbPath};Cache=Shared";

	// Add Infrastructure services (DbContext, repositories, services)
	services.AddInfrastructure(connectionString);

	// Add Application services
	services.AddApplication();

	// SignalR
	services.AddSignalR(options => {
		options.EnableDetailedErrors = true;
		options.MaximumReceiveMessageSize = 1024 * 1024; // 1MB
	});

	// CORS for development
	services.AddCors(options => {
		options.AddPolicy("Development", policy => {
			policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
				.AllowAnyHeader()
				.AllowAnyMethod()
				.AllowCredentials();
		});
	});

	// OpenAPI
	services.AddOpenApi();

	// Health checks
	services.AddHealthChecks()
		.AddDbContextCheck<SubromDbContext>();

	// Background services
	services.AddSingleton<ScanJobProcessor>();
	services.AddHostedService(sp => sp.GetRequiredService<ScanJobProcessor>());
}

static void ConfigureApp(WebApplication app) {
	// Exception handling - both dev and prod
	app.UseExceptionHandler("/error");

	// Error endpoint for ProblemDetails response
	app.Map("/error", (HttpContext context) => {
		var exceptionHandler = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
		var exception = exceptionHandler?.Error;

		var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails {
			Title = "An error occurred",
			Status = StatusCodes.Status500InternalServerError,
			Detail = app.Environment.IsDevelopment() ? exception?.Message : "An unexpected error occurred.",
			Instance = context.Request.Path
		};

		if (exception is KeyNotFoundException) {
			problem.Status = StatusCodes.Status404NotFound;
			problem.Title = "Not Found";
		} else if (exception is InvalidOperationException) {
			problem.Status = StatusCodes.Status400BadRequest;
			problem.Title = "Bad Request";
		} else if (exception is UnauthorizedAccessException) {
			problem.Status = StatusCodes.Status403Forbidden;
			problem.Title = "Forbidden";
		} else if (exception is ArgumentException or ArgumentNullException) {
			problem.Status = StatusCodes.Status400BadRequest;
			problem.Title = "Invalid Argument";
		}

		context.Response.StatusCode = problem.Status ?? 500;
		return Results.Problem(problem);
	});

	// CORS
	app.UseCors("Development");

	// Serilog request logging
	app.UseSerilogRequestLogging();

	// Static files (React UI)
	app.UseDefaultFiles();
	app.UseStaticFiles();

	// OpenAPI/Scalar
	if (app.Environment.IsDevelopment()) {
		app.MapOpenApi();
		app.MapScalarApiReference(options => {
			options.Title = "Subrom API";
			options.Theme = ScalarTheme.BluePlanet;
		});
	}

	// Health check endpoint
	app.MapHealthChecks("/api/health");

	// API endpoints
	app.MapSubromEndpoints();

	// SignalR hub
	app.MapHub<SubromHub>("/hub");

	// SPA fallback
	app.MapFallbackToFile("index.html");
}

static async Task InitializeDatabaseAsync(WebApplication app) {
	using var scope = app.Services.CreateScope();
	var db = scope.ServiceProvider.GetRequiredService<SubromDbContext>();

	// Ensure database exists and apply migrations
	await db.Database.EnsureCreatedAsync();

	// Apply SQLite optimizations
	await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode = WAL;");
	await db.Database.ExecuteSqlRawAsync("PRAGMA synchronous = NORMAL;");
	await db.Database.ExecuteSqlRawAsync("PRAGMA cache_size = -64000;"); // 64MB
	await db.Database.ExecuteSqlRawAsync("PRAGMA temp_store = MEMORY;");
	await db.Database.ExecuteSqlRawAsync("PRAGMA mmap_size = 268435456;"); // 256MB

	Log.Information("Database initialized at {Path}", db.Database.GetDbConnection().DataSource);
}

/// <summary>
/// Scalar theme configuration.
/// </summary>
public enum ScalarTheme {
	Default,
	BluePlanet,
	Saturn,
	Kepler,
	Mars,
	DeepSpace
}

/// <summary>
/// Extension methods for Scalar API reference.
/// </summary>
public static class ScalarExtensions {
	public static IEndpointRouteBuilder MapScalarApiReference(
		this IEndpointRouteBuilder endpoints,
		Action<ScalarOptions>? configure = null) {
		var options = new ScalarOptions();
		configure?.Invoke(options);

		endpoints.MapGet("/scalar/{documentName}", (string documentName) =>
			Results.Content($$"""
				<!DOCTYPE html>
				<html>
				<head>
					<title>{{options.Title}}</title>
					<meta charset="utf-8" />
					<meta name="viewport" content="width=device-width, initial-scale=1" />
				</head>
				<body>
					<script id="api-reference" data-url="/openapi/v1.json"></script>
					<script src="https://cdn.jsdelivr.net/npm/@scalar/api-reference"></script>
				</body>
				</html>
				""", "text/html"))
			.ExcludeFromDescription();

		return endpoints;
	}
}

public class ScalarOptions {
	public string Title { get; set; } = "API Reference";
	public ScalarTheme Theme { get; set; } = ScalarTheme.Default;
}
