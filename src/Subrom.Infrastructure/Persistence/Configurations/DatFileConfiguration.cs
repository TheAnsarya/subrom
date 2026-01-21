using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subrom.Domain.Aggregates.DatFiles;

namespace Subrom.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for DatFile entity.
/// </summary>
public class DatFileConfiguration : IEntityTypeConfiguration<DatFile> {
	public void Configure(EntityTypeBuilder<DatFile> builder) {
		builder.ToTable("DatFiles");

		builder.HasKey(d => d.Id);

		builder.Property(d => d.FileName)
			.IsRequired()
			.HasMaxLength(500);

		builder.Property(d => d.Name)
			.IsRequired()
			.HasMaxLength(500);

		builder.Property(d => d.Description)
			.HasMaxLength(2000);

		builder.Property(d => d.Version)
			.HasMaxLength(100);

		builder.Property(d => d.Author)
			.HasMaxLength(200);

		builder.Property(d => d.Homepage)
			.HasMaxLength(500);

		builder.Property(d => d.System)
			.HasMaxLength(200);

		builder.Property(d => d.CategoryPath)
			.HasMaxLength(1000);

		builder.Property(d => d.Format)
			.HasConversion<string>()
			.HasMaxLength(50);

		builder.Property(d => d.Provider)
			.HasConversion<string>()
			.HasMaxLength(50);

		// Index for fast lookups
		builder.HasIndex(d => d.Name);
		builder.HasIndex(d => d.CategoryPath);
		builder.HasIndex(d => d.Provider);
		builder.HasIndex(d => d.IsEnabled);

		// Ignore domain events
		builder.Ignore(d => d.DomainEvents);

		// Games relationship is configured in GameEntryConfiguration
	}
}

/// <summary>
/// EF Core configuration for GameEntry entity.
/// </summary>
public class GameEntryConfiguration : IEntityTypeConfiguration<GameEntry> {
	public void Configure(EntityTypeBuilder<GameEntry> builder) {
		builder.ToTable("Games");

		builder.HasKey(g => g.Id);

		builder.Property(g => g.Name)
			.IsRequired()
			.HasMaxLength(500);

		builder.Property(g => g.Description)
			.HasMaxLength(1000);

		builder.Property(g => g.Region)
			.HasMaxLength(100);

		builder.Property(g => g.Languages)
			.HasMaxLength(100);

		builder.Property(g => g.Year)
			.HasMaxLength(20);

		builder.Property(g => g.Publisher)
			.HasMaxLength(200);

		builder.Property(g => g.CloneOf)
			.HasMaxLength(500);

		builder.Property(g => g.RomOf)
			.HasMaxLength(500);

		builder.Property(g => g.SampleOf)
			.HasMaxLength(500);

		builder.Property(g => g.Category)
			.HasMaxLength(200);

		// Indexes
		builder.HasIndex(g => g.DatFileId);
		builder.HasIndex(g => g.Name);
		builder.HasIndex(g => g.Region);

		// Relationship with DatFile
		builder.HasOne<DatFile>()
			.WithMany(d => d.Games)
			.HasForeignKey(g => g.DatFileId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}

/// <summary>
/// EF Core configuration for RomEntry entity.
/// </summary>
public class RomEntryConfiguration : IEntityTypeConfiguration<RomEntry> {
	public void Configure(EntityTypeBuilder<RomEntry> builder) {
		builder.ToTable("RomEntries");

		builder.HasKey(r => r.Id);

		builder.Property(r => r.Name)
			.IsRequired()
			.HasMaxLength(500);

		builder.Property(r => r.Serial)
			.HasMaxLength(50);

		builder.Property(r => r.Merge)
			.HasMaxLength(500);

		builder.Property(r => r.Status)
			.HasConversion<string>()
			.HasMaxLength(20);

		// Hash columns as simple strings
		builder.Property(r => r.Crc)
			.HasMaxLength(8);

		builder.Property(r => r.Md5)
			.HasMaxLength(32);

		builder.Property(r => r.Sha1)
			.HasMaxLength(40);

		// Indexes for hash-based verification lookups
		builder.HasIndex(r => r.GameId);
		builder.HasIndex(r => r.Size);
		builder.HasIndex(r => r.Crc);
		builder.HasIndex(r => r.Sha1);
		builder.HasIndex(r => new { r.Size, r.Crc }).HasDatabaseName("IX_RomEntries_Size_Crc");
		builder.HasIndex(r => new { r.Size, r.Sha1 }).HasDatabaseName("IX_RomEntries_Size_Sha1");

		// Relationship with GameEntry
		builder.HasOne<GameEntry>()
			.WithMany(g => g.Roms)
			.HasForeignKey(r => r.GameId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}
