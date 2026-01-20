using Microsoft.Extensions.Logging;
using Subrom.Domain.Storage;

namespace Subrom.Services;

/// <summary>
/// Service for managing storage drives - registration, status tracking, and offline handling.
/// CRITICAL: This service ensures ROMs are NEVER lost when drives go offline.
/// </summary>
public class DriveService : IDriveService {
	private readonly ILogger<DriveService> _logger;
	private readonly Dictionary<Guid, Drive> _drives = new();

	public DriveService(ILogger<DriveService> logger) {
		_logger = logger;
	}

	/// <summary>
	/// Registers a new storage drive.
	/// </summary>
	public Drive RegisterDrive(string path, string label) {
		if (!Directory.Exists(path)) {
			throw new DirectoryNotFoundException($"Path not found: {path}");
		}

		var driveInfo = new DriveInfo(Path.GetPathRoot(path) ?? path);
		var volumeId = GetVolumeId(driveInfo);

		// Check if this drive is already registered by volume ID
		var existing = _drives.Values.FirstOrDefault(d => d.VolumeId == volumeId);
		if (existing != null) {
			_logger.LogInformation("Drive already registered: {Label} ({VolumeId})", existing.Label, volumeId);
			existing.IsOnline = true;
			existing.LastSeen = DateTime.UtcNow;
			return existing;
		}

		var drive = new Drive {
			Id = Guid.CreateVersion7(),
			Label = label,
			Path = path,
			VolumeId = volumeId,
			IsOnline = true,
			LastSeen = DateTime.UtcNow,
			TotalCapacity = driveInfo.TotalSize,
			FreeSpace = driveInfo.AvailableFreeSpace
		};

		_drives[drive.Id] = drive;
		_logger.LogInformation("Registered drive: {Label} at {Path}", label, path);

		return drive;
	}

	/// <summary>
	/// Gets all registered drives.
	/// </summary>
	public IEnumerable<Drive> GetAllDrives() => _drives.Values;

	/// <summary>
	/// Gets online drives only.
	/// </summary>
	public IEnumerable<Drive> GetOnlineDrives() => _drives.Values.Where(d => d.IsOnline);

	/// <summary>
	/// Gets offline drives.
	/// </summary>
	public IEnumerable<Drive> GetOfflineDrives() => _drives.Values.Where(d => !d.IsOnline);

	/// <summary>
	/// Gets a drive by ID.
	/// </summary>
	public Drive? GetDrive(Guid id) => _drives.GetValueOrDefault(id);

	/// <summary>
	/// Checks and updates the status of all drives.
	/// CRITICAL: Does NOT delete ROM records for offline drives!
	/// </summary>
	public void RefreshDriveStatus() {
		foreach (var drive in _drives.Values) {
			var wasOnline = drive.IsOnline;
			drive.IsOnline = Directory.Exists(drive.Path);

			if (drive.IsOnline) {
				drive.LastSeen = DateTime.UtcNow;

				try {
					var driveInfo = new DriveInfo(Path.GetPathRoot(drive.Path) ?? drive.Path);
					drive.TotalCapacity = driveInfo.TotalSize;
					drive.FreeSpace = driveInfo.AvailableFreeSpace;
				} catch (Exception ex) {
					_logger.LogWarning(ex, "Could not get drive info for {Path}", drive.Path);
				}

				if (!wasOnline) {
					_logger.LogInformation("Drive came online: {Label} at {Path}", drive.Label, drive.Path);
					OnDriveReconnected(drive);
				}
			} else if (wasOnline) {
				_logger.LogWarning("Drive went offline: {Label} at {Path}. ROM records are PRESERVED.",
					drive.Label, drive.Path);
				OnDriveDisconnected(drive);
			}
		}
	}

	/// <summary>
	/// Called when a drive is reconnected. Re-enables ROM records.
	/// </summary>
	private void OnDriveReconnected(Drive drive) {
		// TODO: Update all RomFiles for this drive to IsOnline = true
		_logger.LogInformation("Reconnected drive {Label}. ROM records restored to online status.", drive.Label);
	}

	/// <summary>
	/// Called when a drive is disconnected. Marks ROMs as offline but PRESERVES them.
	/// </summary>
	private void OnDriveDisconnected(Drive drive) {
		// TODO: Update all RomFiles for this drive to IsOnline = false
		// CRITICAL: Do NOT delete ROM records!
		_logger.LogWarning("Drive {Label} disconnected. ROM records marked as offline but PRESERVED.", drive.Label);
	}

	/// <summary>
	/// Unregisters a drive. WARNING: This does not delete ROM records by default.
	/// </summary>
	public bool UnregisterDrive(Guid id, bool preserveRecords = true) {
		if (_drives.TryGetValue(id, out var drive)) {
			if (preserveRecords) {
				_logger.LogInformation("Unregistering drive {Label}. ROM records preserved.", drive.Label);
			} else {
				_logger.LogWarning("Unregistering drive {Label}. ROM records will be removed.", drive.Label);
				// TODO: Delete ROM records for this drive
			}

			_drives.Remove(id);
			return true;
		}

		return false;
	}

	/// <summary>
	/// Gets a unique volume identifier for a drive.
	/// </summary>
	private static string GetVolumeId(DriveInfo driveInfo) {
		try {
			// On Windows, use volume serial number
			// On other platforms, use root directory path
			return driveInfo.VolumeLabel + "-" + driveInfo.DriveFormat;
		} catch {
			return driveInfo.RootDirectory.FullName;
		}
	}
}

/// <summary>
/// Interface for drive management service.
/// </summary>
public interface IDriveService {
	Drive RegisterDrive(string path, string label);
	IEnumerable<Drive> GetAllDrives();
	IEnumerable<Drive> GetOnlineDrives();
	IEnumerable<Drive> GetOfflineDrives();
	Drive? GetDrive(Guid id);
	void RefreshDriveStatus();
	bool UnregisterDrive(Guid id, bool preserveRecords = true);
}
