namespace Subrom.Domain.Aggregates.Settings;

/// <summary>
/// Represents application-level settings stored in the database.
/// Uses a singleton pattern - only one row exists in the table.
/// </summary>
public class AppSettings {
	/// <summary>
	/// Fixed ID for the singleton settings row.
	/// </summary>
	public const int SingletonId = 1;

	/// <summary>
	/// Settings ID (always 1 for singleton).
	/// </summary>
	public int Id { get; set; } = SingletonId;

	/// <summary>
	/// When settings were last modified.
	/// </summary>
	public DateTime LastModified { get; set; } = DateTime.UtcNow;

	// ============================================
	// Scanning Settings
	// ============================================

	/// <summary>
	/// Number of parallel threads for hashing operations.
	/// </summary>
	public int ScanningParallelThreads { get; set; } = 4;

	/// <summary>
	/// Whether to skip hidden files during scanning.
	/// </summary>
	public bool ScanningSkipHiddenFiles { get; set; } = true;

	/// <summary>
	/// Whether to scan inside archive files.
	/// </summary>
	public bool ScanningScanArchives { get; set; } = true;

	/// <summary>
	/// Whether to calculate MD5 hash (in addition to CRC32).
	/// </summary>
	public bool ScanningCalculateMd5 { get; set; } = true;

	/// <summary>
	/// Whether to calculate SHA1 hash (in addition to CRC32).
	/// </summary>
	public bool ScanningCalculateSha1 { get; set; } = true;

	/// <summary>
	/// Whether to detect and remove ROM headers during hashing.
	/// </summary>
	public bool ScanningDetectHeaders { get; set; } = true;

	// ============================================
	// Organization Settings
	// ============================================

	/// <summary>
	/// Default organization template name.
	/// </summary>
	public string OrganizationDefaultTemplate { get; set; } = "system-game";

	/// <summary>
	/// Comma-separated list of preferred regions for 1G1R.
	/// </summary>
	public string OrganizationRegionPriority { get; set; } = "USA,Europe,Japan,World";

	/// <summary>
	/// Comma-separated list of preferred languages for 1G1R.
	/// </summary>
	public string OrganizationLanguagePriority { get; set; } = "En,Ja,De,Fr,Es,It";

	/// <summary>
	/// Whether to use 1G1R filtering by default.
	/// </summary>
	public bool OrganizationUse1G1R { get; set; } = false;

	/// <summary>
	/// Whether to prefer parent ROMs over clones.
	/// </summary>
	public bool OrganizationPreferParent { get; set; } = true;

	// ============================================
	// Verification Settings
	// ============================================

	/// <summary>
	/// Whether to auto-verify after scanning.
	/// </summary>
	public bool VerificationAutoVerify { get; set; } = true;

	/// <summary>
	/// Whether to mark unverified ROMs as unknown.
	/// </summary>
	public bool VerificationMarkUnknown { get; set; } = true;

	// ============================================
	// UI Settings
	// ============================================

	/// <summary>
	/// UI theme: "light" or "dark".
	/// </summary>
	public string UiTheme { get; set; } = "dark";

	/// <summary>
	/// Number of items per page in lists.
	/// </summary>
	public int UiPageSize { get; set; } = 100;

	/// <summary>
	/// Whether to show ROM sizes in human-readable format.
	/// </summary>
	public bool UiShowHumanSizes { get; set; } = true;

	// ============================================
	// Storage Settings
	// ============================================

	/// <summary>
	/// Warning threshold for low disk space (in MB).
	/// </summary>
	public long StorageLowSpaceWarningMb { get; set; } = 1024; // 1GB

	/// <summary>
	/// Whether to monitor drives for online/offline status.
	/// </summary>
	public bool StorageMonitorDrives { get; set; } = true;

	/// <summary>
	/// Gets the region priority as a list.
	/// </summary>
	public IReadOnlyList<string> GetRegionPriority() =>
		OrganizationRegionPriority.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

	/// <summary>
	/// Sets the region priority from a list.
	/// </summary>
	public void SetRegionPriority(IEnumerable<string> regions) =>
		OrganizationRegionPriority = string.Join(",", regions);

	/// <summary>
	/// Gets the language priority as a list.
	/// </summary>
	public IReadOnlyList<string> GetLanguagePriority() =>
		OrganizationLanguagePriority.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

	/// <summary>
	/// Sets the language priority from a list.
	/// </summary>
	public void SetLanguagePriority(IEnumerable<string> languages) =>
		OrganizationLanguagePriority = string.Join(",", languages);
}
