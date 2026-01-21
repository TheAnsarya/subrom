using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subrom.Domain.Aggregates.Organization;

namespace Subrom.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for OrganizationOperationLog.
/// </summary>
public class OrganizationOperationLogConfiguration : IEntityTypeConfiguration<OrganizationOperationLog> {
	public void Configure(EntityTypeBuilder<OrganizationOperationLog> builder) {
		builder.ToTable("OrganizationOperationLogs");

		builder.HasKey(x => x.Id);

		builder.Property(x => x.SourcePath)
			.IsRequired()
			.HasMaxLength(1024);

		builder.Property(x => x.DestinationPath)
			.IsRequired()
			.HasMaxLength(1024);

		builder.Property(x => x.TemplateName)
			.IsRequired()
			.HasMaxLength(256);

		builder.Property(x => x.FolderTemplate)
			.IsRequired()
			.HasMaxLength(512);

		builder.Property(x => x.FileNameTemplate)
			.IsRequired()
			.HasMaxLength(512);

		builder.Property(x => x.InitiatedBy)
			.HasMaxLength(256);

		builder.HasIndex(x => x.PerformedAt);
		builder.HasIndex(x => x.Success);

		builder.HasMany(x => x.Entries)
			.WithOne(x => x.Operation)
			.HasForeignKey(x => x.OperationId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}

/// <summary>
/// EF Core configuration for OrganizationOperationEntry.
/// </summary>
public class OrganizationOperationEntryConfiguration : IEntityTypeConfiguration<OrganizationOperationEntry> {
	public void Configure(EntityTypeBuilder<OrganizationOperationEntry> builder) {
		builder.ToTable("OrganizationOperationEntries");

		builder.HasKey(x => x.Id);

		builder.Property(x => x.OperationType)
			.IsRequired()
			.HasMaxLength(50);

		builder.Property(x => x.SourcePath)
			.IsRequired()
			.HasMaxLength(1024);

		builder.Property(x => x.DestinationPath)
			.IsRequired()
			.HasMaxLength(1024);

		builder.Property(x => x.ErrorMessage)
			.HasMaxLength(1024);

		builder.Property(x => x.Crc)
			.HasMaxLength(8);

		builder.HasIndex(x => x.OperationId);
	}
}
