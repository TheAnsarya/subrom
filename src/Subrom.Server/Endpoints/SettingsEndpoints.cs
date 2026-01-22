using Subrom.Application.Services;

namespace Subrom.Server.Endpoints;

/// <summary>
/// Application settings endpoints.
/// </summary>
public static class SettingsEndpoints {
	public static IEndpointRouteBuilder MapSettingsEndpoints(this IEndpointRouteBuilder endpoints) {
		var group = endpoints.MapGroup("/settings")
			.WithTags("Settings");

		// Get all settings
		group.MapGet("/", async (SettingsService settingsService, CancellationToken ct) => {
			var settings = await settingsService.GetAsync(ct);
			return Results.Ok(new SettingsResponse(settings));
		});

		// Update all settings
		group.MapPut("/", async (UpdateSettingsRequest request, SettingsService settingsService, CancellationToken ct) => {
			var settings = await settingsService.UpdateAsync(s => {
				// Scanning
				if (request.Scanning is not null) {
					if (request.Scanning.ParallelThreads.HasValue)
						s.ScanningParallelThreads = request.Scanning.ParallelThreads.Value;
					if (request.Scanning.SkipHiddenFiles.HasValue)
						s.ScanningSkipHiddenFiles = request.Scanning.SkipHiddenFiles.Value;
					if (request.Scanning.ScanArchives.HasValue)
						s.ScanningScanArchives = request.Scanning.ScanArchives.Value;
					if (request.Scanning.CalculateMd5.HasValue)
						s.ScanningCalculateMd5 = request.Scanning.CalculateMd5.Value;
					if (request.Scanning.CalculateSha1.HasValue)
						s.ScanningCalculateSha1 = request.Scanning.CalculateSha1.Value;
					if (request.Scanning.DetectHeaders.HasValue)
						s.ScanningDetectHeaders = request.Scanning.DetectHeaders.Value;
				}
				// Organization
				if (request.Organization is not null) {
					if (request.Organization.DefaultTemplate is not null)
						s.OrganizationDefaultTemplate = request.Organization.DefaultTemplate;
					if (request.Organization.RegionPriority is not null)
						s.SetRegionPriority(request.Organization.RegionPriority);
					if (request.Organization.LanguagePriority is not null)
						s.SetLanguagePriority(request.Organization.LanguagePriority);
					if (request.Organization.Use1G1R.HasValue)
						s.OrganizationUse1G1R = request.Organization.Use1G1R.Value;
					if (request.Organization.PreferParent.HasValue)
						s.OrganizationPreferParent = request.Organization.PreferParent.Value;
				}
				// Verification
				if (request.Verification is not null) {
					if (request.Verification.AutoVerify.HasValue)
						s.VerificationAutoVerify = request.Verification.AutoVerify.Value;
					if (request.Verification.MarkUnknown.HasValue)
						s.VerificationMarkUnknown = request.Verification.MarkUnknown.Value;
				}
				// UI
				if (request.Ui is not null) {
					if (request.Ui.Theme is not null)
						s.UiTheme = request.Ui.Theme;
					if (request.Ui.PageSize.HasValue)
						s.UiPageSize = request.Ui.PageSize.Value;
					if (request.Ui.ShowHumanSizes.HasValue)
						s.UiShowHumanSizes = request.Ui.ShowHumanSizes.Value;
				}
				// Storage
				if (request.Storage is not null) {
					if (request.Storage.LowSpaceWarningMb.HasValue)
						s.StorageLowSpaceWarningMb = request.Storage.LowSpaceWarningMb.Value;
					if (request.Storage.MonitorDrives.HasValue)
						s.StorageMonitorDrives = request.Storage.MonitorDrives.Value;
				}
			}, ct);
			return Results.Ok(new SettingsResponse(settings));
		});

		// Update scanning settings
		group.MapPatch("/scanning", async (ScanningSettingsRequest request, SettingsService settingsService, CancellationToken ct) => {
			var settings = await settingsService.UpdateScanningSettingsAsync(
				request.ParallelThreads,
				request.SkipHiddenFiles,
				request.ScanArchives,
				request.CalculateMd5,
				request.CalculateSha1,
				request.DetectHeaders,
				ct);
			return Results.Ok(new SettingsResponse(settings));
		});

		// Update organization settings
		group.MapPatch("/organization", async (OrganizationSettingsRequest request, SettingsService settingsService, CancellationToken ct) => {
			var settings = await settingsService.UpdateOrganizationSettingsAsync(
				request.DefaultTemplate,
				request.RegionPriority,
				request.LanguagePriority,
				request.Use1G1R,
				request.PreferParent,
				ct);
			return Results.Ok(new SettingsResponse(settings));
		});

		// Update UI settings
		group.MapPatch("/ui", async (UiSettingsRequest request, SettingsService settingsService, CancellationToken ct) => {
			var settings = await settingsService.UpdateUiSettingsAsync(
				request.Theme,
				request.PageSize,
				request.ShowHumanSizes,
				ct);
			return Results.Ok(new SettingsResponse(settings));
		});

		// Update storage settings
		group.MapPatch("/storage", async (StorageSettingsRequest request, SettingsService settingsService, CancellationToken ct) => {
			var settings = await settingsService.UpdateStorageSettingsAsync(
				request.LowSpaceWarningMb,
				request.MonitorDrives,
				ct);
			return Results.Ok(new SettingsResponse(settings));
		});

		// Reset settings to defaults
		group.MapPost("/reset", async (SettingsService settingsService, CancellationToken ct) => {
			var settings = await settingsService.ResetToDefaultsAsync(ct);
			return Results.Ok(new SettingsResponse(settings));
		});

		return endpoints;
	}
}

// DTOs

public record SettingsResponse {
	public ScanningSettings Scanning { get; init; } = new();
	public OrganizationSettings Organization { get; init; } = new();
	public VerificationSettings Verification { get; init; } = new();
	public UiSettings Ui { get; init; } = new();
	public StorageSettings Storage { get; init; } = new();
	public DateTime LastModified { get; init; }

	public SettingsResponse() { }

	public SettingsResponse(Subrom.Domain.Aggregates.Settings.AppSettings settings) {
		Scanning = new ScanningSettings {
			ParallelThreads = settings.ScanningParallelThreads,
			SkipHiddenFiles = settings.ScanningSkipHiddenFiles,
			ScanArchives = settings.ScanningScanArchives,
			CalculateMd5 = settings.ScanningCalculateMd5,
			CalculateSha1 = settings.ScanningCalculateSha1,
			DetectHeaders = settings.ScanningDetectHeaders
		};
		Organization = new OrganizationSettings {
			DefaultTemplate = settings.OrganizationDefaultTemplate,
			RegionPriority = settings.GetRegionPriority().ToList(),
			LanguagePriority = settings.GetLanguagePriority().ToList(),
			Use1G1R = settings.OrganizationUse1G1R,
			PreferParent = settings.OrganizationPreferParent
		};
		Verification = new VerificationSettings {
			AutoVerify = settings.VerificationAutoVerify,
			MarkUnknown = settings.VerificationMarkUnknown
		};
		Ui = new UiSettings {
			Theme = settings.UiTheme,
			PageSize = settings.UiPageSize,
			ShowHumanSizes = settings.UiShowHumanSizes
		};
		Storage = new StorageSettings {
			LowSpaceWarningMb = settings.StorageLowSpaceWarningMb,
			MonitorDrives = settings.StorageMonitorDrives
		};
		LastModified = settings.LastModified;
	}
}

public record ScanningSettings {
	public int? ParallelThreads { get; init; }
	public bool? SkipHiddenFiles { get; init; }
	public bool? ScanArchives { get; init; }
	public bool? CalculateMd5 { get; init; }
	public bool? CalculateSha1 { get; init; }
	public bool? DetectHeaders { get; init; }
}

public record OrganizationSettings {
	public string? DefaultTemplate { get; init; }
	public List<string>? RegionPriority { get; init; }
	public List<string>? LanguagePriority { get; init; }
	public bool? Use1G1R { get; init; }
	public bool? PreferParent { get; init; }
}

public record VerificationSettings {
	public bool? AutoVerify { get; init; }
	public bool? MarkUnknown { get; init; }
}

public record UiSettings {
	public string? Theme { get; init; }
	public int? PageSize { get; init; }
	public bool? ShowHumanSizes { get; init; }
}

public record StorageSettings {
	public long? LowSpaceWarningMb { get; init; }
	public bool? MonitorDrives { get; init; }
}

// Request DTOs
public record UpdateSettingsRequest(
	ScanningSettingsRequest? Scanning = null,
	OrganizationSettingsRequest? Organization = null,
	VerificationSettingsRequest? Verification = null,
	UiSettingsRequest? Ui = null,
	StorageSettingsRequest? Storage = null
);

public record ScanningSettingsRequest(
	int? ParallelThreads = null,
	bool? SkipHiddenFiles = null,
	bool? ScanArchives = null,
	bool? CalculateMd5 = null,
	bool? CalculateSha1 = null,
	bool? DetectHeaders = null
);

public record OrganizationSettingsRequest(
	string? DefaultTemplate = null,
	List<string>? RegionPriority = null,
	List<string>? LanguagePriority = null,
	bool? Use1G1R = null,
	bool? PreferParent = null
);

public record VerificationSettingsRequest(
	bool? AutoVerify = null,
	bool? MarkUnknown = null
);

public record UiSettingsRequest(
	string? Theme = null,
	int? PageSize = null,
	bool? ShowHumanSizes = null
);

public record StorageSettingsRequest(
	long? LowSpaceWarningMb = null,
	bool? MonitorDrives = null
);
