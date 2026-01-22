using Subrom.Application.Interfaces;
using Subrom.Domain.Aggregates.Settings;

namespace Subrom.Application.Services;

/// <summary>
/// Application service for managing application settings.
/// </summary>
public sealed class SettingsService {
	private readonly ISettingsRepository _settingsRepository;
	private readonly IUnitOfWork _unitOfWork;

	public SettingsService(ISettingsRepository settingsRepository, IUnitOfWork unitOfWork) {
		_settingsRepository = settingsRepository;
		_unitOfWork = unitOfWork;
	}

	/// <summary>
	/// Gets the current application settings.
	/// </summary>
	public async Task<AppSettings> GetAsync(CancellationToken cancellationToken = default) {
		return await _settingsRepository.GetAsync(cancellationToken);
	}

	/// <summary>
	/// Updates the application settings.
	/// </summary>
	public async Task<AppSettings> UpdateAsync(
		Action<AppSettings> configure,
		CancellationToken cancellationToken = default) {
		var settings = await _settingsRepository.GetAsync(cancellationToken);
		configure(settings);
		settings.LastModified = DateTime.UtcNow;
		await _settingsRepository.SaveAsync(settings, cancellationToken);
		await _unitOfWork.SaveChangesAsync(cancellationToken);
		return settings;
	}

	/// <summary>
	/// Updates scanning settings.
	/// </summary>
	public async Task<AppSettings> UpdateScanningSettingsAsync(
		int? parallelThreads = null,
		bool? skipHiddenFiles = null,
		bool? scanArchives = null,
		bool? calculateMd5 = null,
		bool? calculateSha1 = null,
		bool? detectHeaders = null,
		CancellationToken cancellationToken = default) {
		return await UpdateAsync(settings => {
			if (parallelThreads.HasValue)
				settings.ScanningParallelThreads = Math.Clamp(parallelThreads.Value, 1, Environment.ProcessorCount * 2);
			if (skipHiddenFiles.HasValue)
				settings.ScanningSkipHiddenFiles = skipHiddenFiles.Value;
			if (scanArchives.HasValue)
				settings.ScanningScanArchives = scanArchives.Value;
			if (calculateMd5.HasValue)
				settings.ScanningCalculateMd5 = calculateMd5.Value;
			if (calculateSha1.HasValue)
				settings.ScanningCalculateSha1 = calculateSha1.Value;
			if (detectHeaders.HasValue)
				settings.ScanningDetectHeaders = detectHeaders.Value;
		}, cancellationToken);
	}

	/// <summary>
	/// Updates organization settings.
	/// </summary>
	public async Task<AppSettings> UpdateOrganizationSettingsAsync(
		string? defaultTemplate = null,
		IEnumerable<string>? regionPriority = null,
		IEnumerable<string>? languagePriority = null,
		bool? use1G1R = null,
		bool? preferParent = null,
		CancellationToken cancellationToken = default) {
		return await UpdateAsync(settings => {
			if (defaultTemplate != null)
				settings.OrganizationDefaultTemplate = defaultTemplate;
			if (regionPriority != null)
				settings.SetRegionPriority(regionPriority);
			if (languagePriority != null)
				settings.SetLanguagePriority(languagePriority);
			if (use1G1R.HasValue)
				settings.OrganizationUse1G1R = use1G1R.Value;
			if (preferParent.HasValue)
				settings.OrganizationPreferParent = preferParent.Value;
		}, cancellationToken);
	}

	/// <summary>
	/// Updates UI settings.
	/// </summary>
	public async Task<AppSettings> UpdateUiSettingsAsync(
		string? theme = null,
		int? pageSize = null,
		bool? showHumanSizes = null,
		CancellationToken cancellationToken = default) {
		return await UpdateAsync(settings => {
			if (theme != null && (theme == "light" || theme == "dark"))
				settings.UiTheme = theme;
			if (pageSize.HasValue)
				settings.UiPageSize = Math.Clamp(pageSize.Value, 10, 1000);
			if (showHumanSizes.HasValue)
				settings.UiShowHumanSizes = showHumanSizes.Value;
		}, cancellationToken);
	}

	/// <summary>
	/// Updates storage settings.
	/// </summary>
	public async Task<AppSettings> UpdateStorageSettingsAsync(
		long? lowSpaceWarningMb = null,
		bool? monitorDrives = null,
		CancellationToken cancellationToken = default) {
		return await UpdateAsync(settings => {
			if (lowSpaceWarningMb.HasValue)
				settings.StorageLowSpaceWarningMb = Math.Max(0, lowSpaceWarningMb.Value);
			if (monitorDrives.HasValue)
				settings.StorageMonitorDrives = monitorDrives.Value;
		}, cancellationToken);
	}

	/// <summary>
	/// Resets all settings to defaults.
	/// </summary>
	public async Task<AppSettings> ResetToDefaultsAsync(CancellationToken cancellationToken = default) {
		var defaults = new AppSettings();
		defaults.LastModified = DateTime.UtcNow;
		await _settingsRepository.SaveAsync(defaults, cancellationToken);
		await _unitOfWork.SaveChangesAsync(cancellationToken);
		return defaults;
	}
}
