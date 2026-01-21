using Subrom.Domain.Aggregates.Storage;

namespace Subrom.Application.Interfaces;

/// <summary>
/// Service for monitoring storage drives and detecting changes.
/// </summary>
public interface IStorageMonitorService {
	/// <summary>
	/// Starts monitoring for drive changes.
	/// </summary>
	Task StartMonitoringAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Stops monitoring for drive changes.
	/// </summary>
	Task StopMonitoringAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Checks all registered drives and updates their online status.
	/// </summary>
	Task<IReadOnlyList<DriveStatusChange>> RefreshAllDrivesAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets the current status of a specific drive.
	/// </summary>
	Task<DriveStatus> GetDriveStatusAsync(Guid driveId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets a summary of all registered drives.
	/// </summary>
	Task<StorageSummary> GetSummaryAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Finds all ROMs that are currently inaccessible (on offline drives).
	/// </summary>
	Task<IReadOnlyList<RomFile>> GetOfflineRomsAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Finds potential duplicates across all drives.
	/// </summary>
	Task<IReadOnlyList<DuplicateRomGroup>> FindDuplicatesAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets relocation suggestions for ROMs on a drive.
	/// </summary>
	Task<IReadOnlyList<RelocationSuggestion>> GetRelocationSuggestionsAsync(
		Guid sourceDriveId,
		Guid? targetDriveId = null,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Event raised when a drive status changes.
	/// </summary>
	event EventHandler<DriveStatusChangedEventArgs>? DriveStatusChanged;
}

/// <summary>
/// Represents a change in drive status.
/// </summary>
public record DriveStatusChange(
	Guid DriveId,
	string Label,
	string RootPath,
	bool WasOnline,
	bool IsOnline,
	long? TotalSize,
	long? FreeSpace
);

/// <summary>
/// Current status of a drive.
/// </summary>
public record DriveStatus(
	Guid DriveId,
	string Label,
	string RootPath,
	bool IsOnline,
	DateTime LastSeenAt,
	long? TotalSize,
	long? FreeSpace,
	int RomCount,
	long TotalRomSize
);

/// <summary>
/// Summary of all storage.
/// </summary>
public record StorageSummary(
	int TotalDrives,
	int OnlineDrives,
	int OfflineDrives,
	long TotalCapacity,
	long TotalFreeSpace,
	int TotalRoms,
	int OnlineRoms,
	int OfflineRoms,
	long TotalRomSize
);

/// <summary>
/// Group of duplicate ROMs (same hashes on different drives/paths).
/// </summary>
public record DuplicateRomGroup(
	string Crc,
	string? Sha1,
	IReadOnlyList<RomFile> Files
) {
	/// <summary>
	/// Gets the wasted space (total size minus one copy).
	/// </summary>
	public long WastedSpace => Files.Count > 1 ? Files.Skip(1).Sum(f => f.Size) : 0;
}

/// <summary>
/// Suggestion to relocate a ROM to another drive.
/// </summary>
public record RelocationSuggestion(
	RomFile RomFile,
	Drive SourceDrive,
	Drive TargetDrive,
	string SuggestedPath,
	string Reason
);

/// <summary>
/// Event args for drive status changes.
/// </summary>
public class DriveStatusChangedEventArgs : EventArgs {
	public required DriveStatusChange Change { get; init; }
}
