using Microsoft.EntityFrameworkCore;

using Subrom.Services;
using Subrom.Services.Interfaces;
using Subrom.SubromAPI.Data;

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

// Controllers
builder.Services.AddControllers();

// OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => {
	options.SwaggerDoc("v1", new() {
		Title = "Subrom API",
		Version = "v1",
		Description = "ROM management and verification API",
	});
});

// CORS for React dev server
builder.Services.AddCors(options => {
	options.AddDefaultPolicy(policy => {
		policy.WithOrigins("http://localhost:3000")
			.AllowAnyHeader()
			.AllowAnyMethod();
	});
});

var app = builder.Build();

// Apply migrations on startup
using (var scope = app.Services.CreateScope()) {
	var db = scope.ServiceProvider.GetRequiredService<SubromDbContext>();
	await db.Database.EnsureCreatedAsync();
}

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

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

await app.RunAsync();
