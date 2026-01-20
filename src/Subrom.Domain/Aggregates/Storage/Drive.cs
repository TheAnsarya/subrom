using Subrom.Domain.Common;

namespace Subrom.Domain.Aggregates.Storage;

/// <summary>
/// Aggregate root for storage drives.
/// Tracks drives that may be online or offline.
/// </summary>
public class Drive : AggregateRoot {
	/// <summary>
	/// User-defined label for this drive.
	/// </summary>
	public required string Label { get; set; }

	/// <summary>
	/// Root path of the drive (e.g., "E:\", "/mnt/roms").
	/// </summary>
	public required string RootPath { get; set; }

	/// <summary>
	/// Volume serial number for identification when offline.
	/// </summary>
	public string? VolumeSerial { get; init; }

	/// <summary>
	/// Volume label from the filesystem.
	/// </summary>
	public string? VolumeLabel { get; set; }

	/// <summary>
	/// Drive type (Fixed, Removable, Network, etc.).
	/// </summary>
	public DriveType DriveType { get; init; } = DriveType.Fixed;

	/// <summary>
	/// Whether the drive is currently accessible.
	/// </summary>
	public bool IsOnline { get; set; } = true;

	/// <summary>
	/// When the drive was last seen online.
	/// </summary>
	public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;

	/// <summary>
	/// When the drive was first registered.
	/// </summary>
	public DateTime RegisteredAt { get; init; } = DateTime.UtcNow;

	/// <summary>
	/// Total size of the drive in bytes.
	/// </summary>
	public long? TotalSize { get; set; }

	/// <summary>
	/// Available free space in bytes.
	/// </summary>
	public long? FreeSpace { get; set; }

	/// <summary>
	/// Whether to automatically scan this drive when it comes online.
	/// </summary>
	public bool AutoScan { get; set; } = true;

	/// <summary>
	/// Scan paths relative to the root path.
	/// If empty, the entire drive is scanned.
	/// </summary>
	public List<string> ScanPaths { get; init; } = [];

	/// <summary>
	/// Creates a new drive registration.
	/// </summary>
	public static Drive Create(string label, string rootPath, DriveType driveType = DriveType.Fixed) {
		var drive = new Drive {
			Label = label,
			RootPath = rootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
			DriveType = driveType
		};

		drive.AddDomainEvent(new DriveRegisteredEvent(drive.Id, label, rootPath));
		return drive;
	}

	/// <summary>
	/// Marks the drive as online with current stats.
	/// </summary>
	public void MarkOnline(long? totalSize = null, long? freeSpace = null) {
		IsOnline = true;
		LastSeenAt = DateTime.UtcNow;
		TotalSize = totalSize;
		FreeSpace = freeSpace;

		AddDomainEvent(new DriveOnlineEvent(Id, RootPath));
	}

	/// <summary>
	/// Marks the drive as offline.
	/// </summary>
	public void MarkOffline() {
		IsOnline = false;
		AddDomainEvent(new DriveOfflineEvent(Id, RootPath));
	}

	/// <summary>
	/// Gets the full path for a relative path on this drive.
	/// </summary>
	public string GetFullPath(string relativePath) =>
		Path.Combine(RootPath, relativePath);
}

/// <summary>
/// Drive type enumeration.
/// </summary>
public enum DriveType {
	Unknown = 0,
	Fixed,
	Removable,
	Network,
	Optical
}

/// <summary>
/// Event raised when a drive is registered.
/// </summary>
public sealed record DriveRegisteredEvent(Guid DriveId, string Label, string RootPath) : DomainEvent;

/// <summary>
/// Event raised when a drive comes online.
/// </summary>
public sealed record DriveOnlineEvent(Guid DriveId, string RootPath) : DomainEvent;

/// <summary>
/// Event raised when a drive goes offline.
/// </summary>
public sealed record DriveOfflineEvent(Guid DriveId, string RootPath) : DomainEvent;
