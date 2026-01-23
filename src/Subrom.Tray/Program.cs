using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace Subrom.Tray;

internal static class Program {
	[STAThread]
	static void Main() {
		// Single instance check
		if (!TrayApplicationContext.EnsureSingleInstance()) {
			MessageBox.Show(
				"Subrom Tray is already running.",
				"Subrom",
				MessageBoxButtons.OK,
				MessageBoxIcon.Information);
			return;
		}

		// Build configuration
		var configuration = new ConfigurationBuilder()
			.SetBasePath(AppContext.BaseDirectory)
			.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
			.Build();

		var settings = configuration.Get<TraySettings>() ?? new TraySettings();

		// Configure Serilog
		var logPath = Path.Combine(AppContext.BaseDirectory, settings.Logging.LogPath, "subrom-tray-.log");
		var rollingInterval = settings.Logging.RollingInterval.ToLowerInvariant() switch {
			"hour" => RollingInterval.Hour,
			"minute" => RollingInterval.Minute,
			_ => RollingInterval.Day
		};

		Log.Logger = new LoggerConfiguration()
			.MinimumLevel.Debug()
			.MinimumLevel.Override("Microsoft", LogEventLevel.Information)
			.Enrich.FromLogContext()
			.WriteTo.File(
				logPath,
				rollingInterval: rollingInterval,
				retainedFileCountLimit: settings.Logging.RetainedFileCountLimit,
				fileSizeLimitBytes: settings.Logging.FileSizeLimitMb * 1024 * 1024)
			.CreateLogger();

		try {
			Log.Information("Starting Subrom Tray application");

			// Build service provider
			var services = new ServiceCollection();
			services.AddSingleton(settings);
			services.AddLogging(builder => builder.AddSerilog(dispose: true));
			services.AddSingleton<ServerManager>();
			services.AddSingleton<TrayApplicationContext>();

			using var serviceProvider = services.BuildServiceProvider();

			ApplicationConfiguration.Initialize();
			Application.SetHighDpiMode(HighDpiMode.SystemAware);

			var context = serviceProvider.GetRequiredService<TrayApplicationContext>();
			Application.Run(context);
		} catch (Exception ex) {
			Log.Fatal(ex, "Application terminated unexpectedly");
			MessageBox.Show(
				$"Fatal error: {ex.Message}",
				"Subrom Error",
				MessageBoxButtons.OK,
				MessageBoxIcon.Error);
		} finally {
			Log.CloseAndFlush();
		}
	}
}
