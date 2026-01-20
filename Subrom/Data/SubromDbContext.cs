using Microsoft.EntityFrameworkCore;

using Subrom.Domain.Datfiles;
using Subrom.Domain.Storage;

namespace Subrom.SubromAPI.Data;

/// <summary>
/// Entity Framework Core database context for Subrom.
/// Uses SQLite for local-first storage.
/// </summary>
public sealed class SubromDbContext(DbContextOptions<SubromDbContext> options) : DbContext(options) {
	/// <summary>Registered storage drives.</summary>
	public DbSet<DriveEntity> Drives => Set<DriveEntity>();

	/// <summary>ROM files tracked across all drives.</summary>
	public DbSet<RomFileEntity> RomFiles => Set<RomFileEntity>();

	/// <summary>Scan jobs and their results.</summary>
	public DbSet<ScanJobEntity> ScanJobs => Set<ScanJobEntity>();

	/// <summary>Imported DAT files.</summary>
	public DbSet<DatFileEntity> DatFiles => Set<DatFileEntity>();

	/// <summary>Games from DAT files.</summary>
	public DbSet<GameEntity> Games => Set<GameEntity>();

	/// <summary>ROM entries from DAT files.</summary>
	public DbSet<RomEntryEntity> RomEntries => Set<RomEntryEntity>();

	protected override void OnModelCreating(ModelBuilder modelBuilder) {
		base.OnModelCreating(modelBuilder);

		// Drive configuration
		modelBuilder.Entity<DriveEntity>(entity => {
			entity.HasKey(e => e.Id);
			entity.Property(e => e.Label).HasMaxLength(256).IsRequired();
			entity.Property(e => e.Path).HasMaxLength(1024).IsRequired();
			entity.Property(e => e.VolumeId).HasMaxLength(128);
			entity.HasIndex(e => e.VolumeId);
			entity.HasMany(e => e.RomFiles).WithOne(r => r.Drive).HasForeignKey(r => r.DriveId);
		});

		// RomFile configuration
		modelBuilder.Entity<RomFileEntity>(entity => {
			entity.HasKey(e => e.Id);
			entity.Property(e => e.Path).HasMaxLength(2048).IsRequired();
			entity.Property(e => e.FileName).HasMaxLength(512).IsRequired();
			entity.Property(e => e.Crc32).HasMaxLength(8);
			entity.Property(e => e.Md5).HasMaxLength(32);
			entity.Property(e => e.Sha1).HasMaxLength(40);
			entity.HasIndex(e => e.Sha1);
			entity.HasIndex(e => e.Md5);
			entity.HasIndex(e => e.Crc32);
			entity.HasIndex(e => new { e.DriveId, e.Path }).IsUnique();
		});

		// ScanJob configuration
		modelBuilder.Entity<ScanJobEntity>(entity => {
			entity.HasKey(e => e.Id);
			entity.Property(e => e.RootPath).HasMaxLength(1024).IsRequired();
			entity.Property(e => e.CurrentFile).HasMaxLength(2048);
			entity.Property(e => e.ErrorMessage).HasMaxLength(4096);
		});

		// DatFile configuration
		modelBuilder.Entity<DatFileEntity>(entity => {
			entity.HasKey(e => e.Id);
			entity.Property(e => e.Name).HasMaxLength(256).IsRequired();
			entity.Property(e => e.Description).HasMaxLength(1024);
			entity.Property(e => e.Provider).HasMaxLength(64);
			entity.Property(e => e.Version).HasMaxLength(64);
			entity.Property(e => e.FilePath).HasMaxLength(1024);
			entity.HasMany(e => e.Games).WithOne(g => g.DatFile).HasForeignKey(g => g.DatFileId);
		});

		// Game configuration
		modelBuilder.Entity<GameEntity>(entity => {
			entity.HasKey(e => e.Id);
			entity.Property(e => e.Name).HasMaxLength(512).IsRequired();
			entity.Property(e => e.Description).HasMaxLength(1024);
			entity.Property(e => e.CloneOf).HasMaxLength(256);
			entity.Property(e => e.RomOf).HasMaxLength(256);
			entity.HasMany(e => e.Roms).WithOne(r => r.Game).HasForeignKey(r => r.GameId);
			entity.HasIndex(e => new { e.DatFileId, e.Name });
		});

		// RomEntry configuration
		modelBuilder.Entity<RomEntryEntity>(entity => {
			entity.HasKey(e => e.Id);
			entity.Property(e => e.Name).HasMaxLength(512).IsRequired();
			entity.Property(e => e.Crc32).HasMaxLength(8);
			entity.Property(e => e.Md5).HasMaxLength(32);
			entity.Property(e => e.Sha1).HasMaxLength(40);
			entity.HasIndex(e => e.Sha1);
			entity.HasIndex(e => e.Crc32);
		});
	}
}

// Entity classes for EF Core (separate from domain records for mutability)

public class DriveEntity {
	public Guid Id { get; set; }
	public string Label { get; set; } = "";
	public string Path { get; set; } = "";
	public string VolumeId { get; set; } = "";
	public bool IsOnline { get; set; } = true;
	public DateTime LastSeen { get; set; }
	public DateTime? LastScanned { get; set; }
	public long TotalCapacity { get; set; }
	public long FreeSpace { get; set; }
	public DateTime RegisteredAt { get; set; }
	public int RomCount { get; set; }
	public bool IsEnabled { get; set; } = true;
	public ICollection<RomFileEntity> RomFiles { get; set; } = [];
}

public class RomFileEntity {
	public Guid Id { get; set; }
	public Guid DriveId { get; set; }
	public DriveEntity Drive { get; set; } = null!;
	public string Path { get; set; } = "";
	public string FileName { get; set; } = "";
	public long Size { get; set; }
	public DateTime ModifiedAt { get; set; }
	public string? Crc32 { get; set; }
	public string? Md5 { get; set; }
	public string? Sha1 { get; set; }
	public DateTime? VerifiedAt { get; set; }
	public bool IsOnline { get; set; } = true;
	public bool IsInArchive { get; set; }
	public string? ArchivePath { get; set; }
	public string? PathInArchive { get; set; }
	public DateTime CreatedAt { get; set; }
	public DateTime UpdatedAt { get; set; }
}

public class ScanJobEntity {
	public Guid Id { get; set; }
	public Guid? DriveId { get; set; }
	public string RootPath { get; set; } = "";
	public ScanJobStatus Status { get; set; }
	public DateTime? StartedAt { get; set; }
	public DateTime? CompletedAt { get; set; }
	public int TotalFiles { get; set; }
	public int ProcessedFiles { get; set; }
	public int VerifiedFiles { get; set; }
	public int UnknownFiles { get; set; }
	public int ErrorFiles { get; set; }
	public string? CurrentFile { get; set; }
	public string? ErrorMessage { get; set; }
	public DateTime CreatedAt { get; set; }
	public bool Recursive { get; set; } = true;
	public bool VerifyHashes { get; set; } = true;
}

public class DatFileEntity {
	public Guid Id { get; set; }
	public string Name { get; set; } = "";
	public string? Description { get; set; }
	public string? Provider { get; set; }
	public string? Version { get; set; }
	public string? Author { get; set; }
	public string? FilePath { get; set; }
	public DateTime ImportedAt { get; set; }
	public DateTime? UpdatedAt { get; set; }
	public int GameCount { get; set; }
	public int RomCount { get; set; }
	public bool IsEnabled { get; set; } = true;
	public ICollection<GameEntity> Games { get; set; } = [];
}

public class GameEntity {
	public Guid Id { get; set; }
	public Guid DatFileId { get; set; }
	public DatFileEntity DatFile { get; set; } = null!;
	public string Name { get; set; } = "";
	public string? Description { get; set; }
	public string? CloneOf { get; set; }
	public string? RomOf { get; set; }
	public string? Year { get; set; }
	public string? Manufacturer { get; set; }
	public ICollection<RomEntryEntity> Roms { get; set; } = [];
}

public class RomEntryEntity {
	public Guid Id { get; set; }
	public Guid GameId { get; set; }
	public GameEntity Game { get; set; } = null!;
	public string Name { get; set; } = "";
	public long Size { get; set; }
	public string? Crc32 { get; set; }
	public string? Md5 { get; set; }
	public string? Sha1 { get; set; }
	public string? Status { get; set; }
}
