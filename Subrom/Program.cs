using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

using Subrom.Domain.Storage;
using Subrom.Infrastructure.Parsers;
using Subrom.Services;
using Subrom.Services.Interfaces;
using Subrom.SubromAPI.Data;
using Subrom.SubromAPI.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Database
var dbPath = Path.Combine(
	Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
	"Subrom",
	"subrom.db"
);
Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

builder.Services.AddDbContext<SubromDbContext>(options =>
	options.UseSqlite($"Data Source={dbPath}"));

// Services
builder.Services.AddScoped<IHashService, HashService>();
builder.Services.AddSingleton<IScanService, ScanService>();
builder.Services.AddHostedService(sp => (ScanService)sp.GetRequiredService<IScanService>());

// DAT file parsers
builder.Services.AddSingleton<IDatParser, XmlDatParser>();
builder.Services.AddSingleton<IDatParser, ClrMameProDatParser>();

// SignalR for real-time updates
builder.Services.AddSignalR();

// Controllers
builder.Services.AddControllers();

// OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS for React dev server
builder.Services.AddCors(options => {
	options.AddDefaultPolicy(policy => {
		policy.WithOrigins("http://localhost:3000")
			.AllowAnyHeader()
			.AllowAnyMethod()
			.AllowCredentials(); // Required for SignalR
	});
});

var app = builder.Build();

// Apply migrations on startup
using (var scope = app.Services.CreateScope()) {
	var db = scope.ServiceProvider.GetRequiredService<SubromDbContext>();
	await db.Database.EnsureCreatedAsync();
}

// Wire up SignalR broadcaster to scan service
var scanService = app.Services.GetRequiredService<IScanService>();
var hubContext = app.Services.GetRequiredService<IHubContext<ScanHub>>();
scanService.SetBroadcaster(async (job, eventName) => {
	var update = ScanProgressUpdate.FromJob(job);
	await hubContext.Clients.Group($"scan-{job.Id}").SendAsync(eventName, update);
	await hubContext.Clients.Group("all-scans").SendAsync(eventName, update);
});

// Configure pipeline
if (app.Environment.IsDevelopment()) {
	app.UseDeveloperExceptionPage();
	app.UseSwagger();
	app.UseSwaggerUI(options => {
		options.SwaggerEndpoint("/swagger/v1/swagger.json", "Subrom API v1");
		options.RoutePrefix = "swagger";
	});
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthorization();
app.MapControllers();
app.MapHub<ScanHub>("/hubs/scan");

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

await app.RunAsync();
