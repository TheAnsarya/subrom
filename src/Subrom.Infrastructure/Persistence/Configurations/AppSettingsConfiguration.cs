using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subrom.Domain.Aggregates.Settings;

namespace Subrom.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for AppSettings entity.
/// </summary>
public class AppSettingsConfiguration : IEntityTypeConfiguration<AppSettings> {
	public void Configure(EntityTypeBuilder<AppSettings> builder) {
		builder.ToTable("AppSettings", t => {
			// Ensure only one row can exist (singleton pattern)
			t.HasCheckConstraint("CK_AppSettings_SingletonId", $"Id = {AppSettings.SingletonId}");
		});

		builder.HasKey(s => s.Id);

		// Scanning settings
		builder.Property(s => s.ScanningParallelThreads)
			.HasDefaultValue(4);

		builder.Property(s => s.ScanningSkipHiddenFiles)
			.HasDefaultValue(true);

		builder.Property(s => s.ScanningScanArchives)
			.HasDefaultValue(true);

		builder.Property(s => s.ScanningCalculateMd5)
			.HasDefaultValue(true);

		builder.Property(s => s.ScanningCalculateSha1)
			.HasDefaultValue(true);

		builder.Property(s => s.ScanningDetectHeaders)
			.HasDefaultValue(true);

		// Organization settings
		builder.Property(s => s.OrganizationDefaultTemplate)
			.HasMaxLength(100)
			.HasDefaultValue("system-game");

		builder.Property(s => s.OrganizationRegionPriority)
			.HasMaxLength(500)
			.HasDefaultValue("USA,Europe,Japan,World");

		builder.Property(s => s.OrganizationLanguagePriority)
			.HasMaxLength(500)
			.HasDefaultValue("En,Ja,De,Fr,Es,It");

		builder.Property(s => s.OrganizationUse1G1R)
			.HasDefaultValue(false);

		builder.Property(s => s.OrganizationPreferParent)
			.HasDefaultValue(true);

		// Verification settings
		builder.Property(s => s.VerificationAutoVerify)
			.HasDefaultValue(true);

		builder.Property(s => s.VerificationMarkUnknown)
			.HasDefaultValue(true);

		// UI settings
		builder.Property(s => s.UiTheme)
			.HasMaxLength(20)
			.HasDefaultValue("dark");

		builder.Property(s => s.UiPageSize)
			.HasDefaultValue(100);

		builder.Property(s => s.UiShowHumanSizes)
			.HasDefaultValue(true);

		// Storage settings
		builder.Property(s => s.StorageLowSpaceWarningMb)
			.HasDefaultValue(1024L);

		builder.Property(s => s.StorageMonitorDrives)
			.HasDefaultValue(true);

		// Timestamps
		builder.Property(s => s.LastModified)
			.HasDefaultValueSql("datetime('now')");
	}
}
