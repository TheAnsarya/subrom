using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subrom.Domain.Aggregates.Storage;

namespace Subrom.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for Drive entity.
/// </summary>
public class DriveConfiguration : IEntityTypeConfiguration<Drive> {
	public void Configure(EntityTypeBuilder<Drive> builder) {
		builder.ToTable("Drives");

		builder.HasKey(d => d.Id);

		builder.Property(d => d.Label)
			.IsRequired()
			.HasMaxLength(200);

		builder.Property(d => d.RootPath)
			.IsRequired()
			.HasMaxLength(1000);

		builder.Property(d => d.VolumeSerial)
			.HasMaxLength(50);

		builder.Property(d => d.VolumeLabel)
			.HasMaxLength(200);

		builder.Property(d => d.DriveType)
			.HasConversion<string>()
			.HasMaxLength(20);

		// ScanPaths stored as pipe-delimited string with proper value comparer
		var scanPathsComparer = new ValueComparer<List<string>>(
			(c1, c2) => c1!.SequenceEqual(c2!),
			c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
			c => c.ToList());

		builder.Property(d => d.ScanPaths)
			.HasConversion(
				v => string.Join("|", v),
				v => v.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList())
			.Metadata.SetValueComparer(scanPathsComparer);

		// Indexes
		builder.HasIndex(d => d.RootPath).IsUnique();
		builder.HasIndex(d => d.VolumeSerial);
		builder.HasIndex(d => d.IsOnline);

		// Ignore domain events
		builder.Ignore(d => d.DomainEvents);
	}
}

/// <summary>
/// EF Core configuration for RomFile entity.
/// </summary>
public class RomFileConfiguration : IEntityTypeConfiguration<RomFile> {
	public void Configure(EntityTypeBuilder<RomFile> builder) {
		builder.ToTable("RomFiles");

		builder.HasKey(r => r.Id);

		builder.Property(r => r.RelativePath)
			.IsRequired()
			.HasMaxLength(2000);

		builder.Property(r => r.FileName)
			.IsRequired()
			.HasMaxLength(500);

		builder.Property(r => r.ArchivePath)
			.HasMaxLength(2000);

		builder.Property(r => r.PathInArchive)
			.HasMaxLength(500);

		builder.Property(r => r.VerificationStatus)
			.HasConversion<string>()
			.HasMaxLength(20);

		// Hash columns (nullable until computed)
		builder.Property(r => r.Crc)
			.HasMaxLength(8);

		builder.Property(r => r.Md5)
			.HasMaxLength(32);

		builder.Property(r => r.Sha1)
			.HasMaxLength(40);

		// Indexes for fast lookups
		builder.HasIndex(r => r.DriveId);
		builder.HasIndex(r => new { r.DriveId, r.RelativePath }).IsUnique();
		builder.HasIndex(r => r.Size);
		builder.HasIndex(r => r.VerificationStatus);
		builder.HasIndex(r => r.MatchedDatFileId);
		builder.HasIndex(r => r.Crc);
		builder.HasIndex(r => r.Sha1);

		// Composite indexes for verification matching (Epic #8)
		builder.HasIndex(r => new { r.Size, r.Crc }).HasDatabaseName("IX_RomFiles_Size_Crc");
		builder.HasIndex(r => new { r.Size, r.Sha1 }).HasDatabaseName("IX_RomFiles_Size_Sha1");
		builder.HasIndex(r => new { r.DriveId, r.VerificationStatus }).HasDatabaseName("IX_RomFiles_Drive_Status");

		// Ignore computed property
		builder.Ignore(r => r.HasHashes);

		// Relationship with Drive
		builder.HasOne<Drive>()
			.WithMany()
			.HasForeignKey(r => r.DriveId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}
