using Serilog;
using Serilog.Events;
using Subrom.Service;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
	.MinimumLevel.Information()
	.MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
	.MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
	.Enrich.FromLogContext()
	.WriteTo.File(
		Path.Combine(AppContext.BaseDirectory, "logs", "subrom-service-.log"),
		rollingInterval: RollingInterval.Day,
		retainedFileCountLimit: 30)
	.WriteTo.EventLog("Subrom Service", manageEventSource: true)
	.CreateLogger();

try {
	Log.Information("Starting Subrom Windows Service");

	var builder = Host.CreateApplicationBuilder(args);

	// Configure Windows Service hosting
	builder.Services.AddWindowsService(options => options.ServiceName = "SubromService");

	builder.Services.AddSerilog();
	builder.Services.AddHostedService<SubromServiceWorker>();

	var host = builder.Build();
	await host.RunAsync();
} catch (Exception ex) {
	Log.Fatal(ex, "Service terminated unexpectedly");
	throw;
} finally {
	await Log.CloseAndFlushAsync();
}
