using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subrom.Domain.Aggregates.Scanning;

namespace Subrom.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for ScanJob entity.
/// </summary>
public class ScanJobConfiguration : IEntityTypeConfiguration<ScanJob> {
	public void Configure(EntityTypeBuilder<ScanJob> builder) {
		builder.ToTable("ScanJobs");

		builder.HasKey(s => s.Id);

		builder.Property(s => s.Type)
			.HasConversion<string>()
			.HasMaxLength(20);

		builder.Property(s => s.Status)
			.HasConversion<string>()
			.HasMaxLength(20);

		builder.Property(s => s.TargetPath)
			.HasMaxLength(2000);

		builder.Property(s => s.CurrentPhase)
			.HasMaxLength(100);

		builder.Property(s => s.CurrentItem)
			.HasMaxLength(500);

		builder.Property(s => s.ErrorMessage)
			.HasMaxLength(2000);

		builder.Property(s => s.InitiatedBy)
			.HasMaxLength(100);

		// Indexes
		builder.HasIndex(s => s.Status);
		builder.HasIndex(s => s.DriveId);
		builder.HasIndex(s => s.QueuedAt);

		// Ignore domain events and computed properties
		builder.Ignore(s => s.DomainEvents);
		builder.Ignore(s => s.Progress);
		builder.Ignore(s => s.Elapsed);
	}
}
