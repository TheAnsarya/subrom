using Microsoft.EntityFrameworkCore;
using Subrom.Domain.Aggregates.DatFiles;
using Subrom.Domain.Aggregates.Organization;
using Subrom.Domain.Aggregates.Scanning;
using Subrom.Domain.Aggregates.Settings;
using Subrom.Domain.Aggregates.Storage;

namespace Subrom.Infrastructure.Persistence;

/// <summary>
/// EF Core database context for Subrom.
/// </summary>
public class SubromDbContext : DbContext {
	public SubromDbContext(DbContextOptions<SubromDbContext> options) : base(options) { }

	public DbSet<DatFile> DatFiles => Set<DatFile>();
	public DbSet<GameEntry> Games => Set<GameEntry>();
	public DbSet<RomEntry> Roms => Set<RomEntry>();
	public DbSet<Drive> Drives => Set<Drive>();
	public DbSet<RomFile> RomFiles => Set<RomFile>();
	public DbSet<ScanJob> ScanJobs => Set<ScanJob>();
	public DbSet<OrganizationOperationLog> OrganizationOperationLogs => Set<OrganizationOperationLog>();
	public DbSet<OrganizationOperationEntry> OrganizationOperationEntries => Set<OrganizationOperationEntry>();
	public DbSet<AppSettings> AppSettings => Set<AppSettings>();

	protected override void OnModelCreating(ModelBuilder modelBuilder) {
		base.OnModelCreating(modelBuilder);

		// Apply all configurations from this assembly
		modelBuilder.ApplyConfigurationsFromAssembly(typeof(SubromDbContext).Assembly);
	}

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
		base.OnConfiguring(optionsBuilder);

		// SQLite optimizations
		if (optionsBuilder.IsConfigured) {
			// These are applied in the connection string or via PRAGMA
		}
	}
}
