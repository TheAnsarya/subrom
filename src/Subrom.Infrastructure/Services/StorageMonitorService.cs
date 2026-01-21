using Microsoft.Extensions.Logging;
using Subrom.Application.Interfaces;
using Subrom.Domain.Aggregates.Storage;

namespace Subrom.Infrastructure.Services;

/// <summary>
/// Implementation of storage drive monitoring service.
/// </summary>
public sealed class StorageMonitorService : IStorageMonitorService, IDisposable {
	private readonly ILogger<StorageMonitorService> _logger;
	private readonly IDriveRepository _driveRepository;
	private readonly IRomFileRepository _romFileRepository;
	private readonly IUnitOfWork _unitOfWork;
	private Timer? _monitorTimer;
	private bool _isMonitoring;
	private readonly object _lock = new();

	public event EventHandler<DriveStatusChangedEventArgs>? DriveStatusChanged;

	public StorageMonitorService(
		ILogger<StorageMonitorService> logger,
		IDriveRepository driveRepository,
		IRomFileRepository romFileRepository,
		IUnitOfWork unitOfWork) {
		_logger = logger;
		_driveRepository = driveRepository;
		_romFileRepository = romFileRepository;
		_unitOfWork = unitOfWork;
	}

	/// <inheritdoc />
	public Task StartMonitoringAsync(CancellationToken cancellationToken = default) {
		lock (_lock) {
			if (_isMonitoring) {
				_logger.LogWarning("Storage monitoring is already running");
				return Task.CompletedTask;
			}

			_logger.LogInformation("Starting storage drive monitoring");
			_isMonitoring = true;

			// Check every 30 seconds
			_monitorTimer = new Timer(
				async _ => await CheckDrivesAsync(),
				null,
				TimeSpan.Zero,
				TimeSpan.FromSeconds(30));
		}

		return Task.CompletedTask;
	}

	/// <inheritdoc />
	public Task StopMonitoringAsync(CancellationToken cancellationToken = default) {
		lock (_lock) {
			if (!_isMonitoring) {
				return Task.CompletedTask;
			}

			_logger.LogInformation("Stopping storage drive monitoring");
			_isMonitoring = false;
			_monitorTimer?.Dispose();
			_monitorTimer = null;
		}

		return Task.CompletedTask;
	}

	/// <inheritdoc />
	public async Task<IReadOnlyList<DriveStatusChange>> RefreshAllDrivesAsync(CancellationToken cancellationToken = default) {
		var drives = await _driveRepository.GetAllAsync(cancellationToken);
		var changes = new List<DriveStatusChange>();

		foreach (var drive in drives) {
			var wasOnline = drive.IsOnline;
			var isOnline = IsDriveOnline(drive.RootPath);
			long? totalSize = null;
			long? freeSpace = null;

			if (isOnline) {
				try {
					var driveInfo = new DriveInfo(Path.GetPathRoot(drive.RootPath)!);
					totalSize = driveInfo.TotalSize;
					freeSpace = driveInfo.AvailableFreeSpace;
					drive.MarkOnline(totalSize, freeSpace);
				} catch (Exception ex) {
					_logger.LogWarning(ex, "Failed to get drive info for {Path}", drive.RootPath);
					drive.MarkOnline();
				}
			} else {
				drive.MarkOffline();
			}

			await _driveRepository.UpdateAsync(drive, cancellationToken);

			if (wasOnline != isOnline) {
				var change = new DriveStatusChange(
					drive.Id,
					drive.Label,
					drive.RootPath,
					wasOnline,
					isOnline,
					totalSize,
					freeSpace);

				changes.Add(change);

				_logger.LogInformation(
					"Drive {Label} ({Path}) is now {Status}",
					drive.Label, drive.RootPath, isOnline ? "online" : "offline");

				DriveStatusChanged?.Invoke(this, new DriveStatusChangedEventArgs { Change = change });
			}
		}

		await _unitOfWork.SaveChangesAsync(cancellationToken);
		return changes;
	}

	/// <inheritdoc />
	public async Task<DriveStatus> GetDriveStatusAsync(Guid driveId, CancellationToken cancellationToken = default) {
		var drive = await _driveRepository.GetByIdAsync(driveId, cancellationToken)
			?? throw new KeyNotFoundException($"Drive {driveId} not found");

		var romCount = await _romFileRepository.GetCountByDriveAsync(driveId, cancellationToken);
		var romFiles = await _romFileRepository.GetByDriveAsync(driveId, cancellationToken);
		var totalRomSize = romFiles.Sum(r => r.Size);

		return new DriveStatus(
			drive.Id,
			drive.Label,
			drive.RootPath,
			drive.IsOnline,
			drive.LastSeenAt,
			drive.TotalSize,
			drive.FreeSpace,
			romCount,
			totalRomSize);
	}

	/// <inheritdoc />
	public async Task<StorageSummary> GetSummaryAsync(CancellationToken cancellationToken = default) {
		var drives = await _driveRepository.GetAllAsync(cancellationToken);
		var totalRoms = await _romFileRepository.GetTotalCountAsync(cancellationToken);

		int onlineDrives = 0;
		int offlineDrives = 0;
		long totalCapacity = 0;
		long totalFreeSpace = 0;
		int onlineRoms = 0;
		int offlineRoms = 0;
		long totalRomSize = 0;

		foreach (var drive in drives) {
			if (drive.IsOnline) {
				onlineDrives++;
				totalCapacity += drive.TotalSize ?? 0;
				totalFreeSpace += drive.FreeSpace ?? 0;
			} else {
				offlineDrives++;
			}

			var romFiles = await _romFileRepository.GetByDriveAsync(drive.Id, cancellationToken);
			totalRomSize += romFiles.Sum(r => r.Size);

			if (drive.IsOnline) {
				onlineRoms += romFiles.Count;
			} else {
				offlineRoms += romFiles.Count;
			}
		}

		return new StorageSummary(
			drives.Count,
			onlineDrives,
			offlineDrives,
			totalCapacity,
			totalFreeSpace,
			totalRoms,
			onlineRoms,
			offlineRoms,
			totalRomSize);
	}

	/// <inheritdoc />
	public async Task<IReadOnlyList<RomFile>> GetOfflineRomsAsync(CancellationToken cancellationToken = default) {
		var offlineDrives = await _driveRepository.GetAllAsync(cancellationToken);
		var offlineDriveIds = offlineDrives.Where(d => !d.IsOnline).Select(d => d.Id).ToHashSet();

		if (offlineDriveIds.Count == 0) {
			return [];
		}

		var result = new List<RomFile>();
		foreach (var driveId in offlineDriveIds) {
			var roms = await _romFileRepository.GetByDriveAsync(driveId, cancellationToken);
			result.AddRange(roms);
		}

		return result;
	}

	/// <inheritdoc />
	public async Task<IReadOnlyList<DuplicateRomGroup>> FindDuplicatesAsync(CancellationToken cancellationToken = default) {
		// Get all ROM files grouped by CRC
		var drives = await _driveRepository.GetAllAsync(cancellationToken);
		var allRoms = new List<RomFile>();

		foreach (var drive in drives) {
			var roms = await _romFileRepository.GetByDriveAsync(drive.Id, cancellationToken);
			allRoms.AddRange(roms);
		}

		// Group by CRC (or SHA1 if available)
		var groups = allRoms
			.Where(r => r.Crc is not null)
			.GroupBy(r => r.Crc!)
			.Where(g => g.Count() > 1)
			.Select(g => new DuplicateRomGroup(
				g.Key,
				g.FirstOrDefault(r => r.Sha1 is not null)?.Sha1,
				g.ToList()))
			.ToList();

		return groups;
	}

	/// <inheritdoc />
	public async Task<IReadOnlyList<RelocationSuggestion>> GetRelocationSuggestionsAsync(
		Guid sourceDriveId,
		Guid? targetDriveId = null,
		CancellationToken cancellationToken = default) {
		var sourceDrive = await _driveRepository.GetByIdAsync(sourceDriveId, cancellationToken)
			?? throw new KeyNotFoundException($"Source drive {sourceDriveId} not found");

		var targetDrives = targetDriveId.HasValue
			? [await _driveRepository.GetByIdAsync(targetDriveId.Value, cancellationToken)
				?? throw new KeyNotFoundException($"Target drive {targetDriveId} not found")]
			: (await _driveRepository.GetOnlineAsync(cancellationToken))
				.Where(d => d.Id != sourceDriveId)
				.ToList();

		var romsOnSource = await _romFileRepository.GetByDriveAsync(sourceDriveId, cancellationToken);
		var suggestions = new List<RelocationSuggestion>();

		foreach (var rom in romsOnSource) {
			// Find best target drive based on free space
			var bestTarget = targetDrives
				.Where(d => d.IsOnline && (d.FreeSpace ?? 0) > rom.Size)
				.OrderByDescending(d => d.FreeSpace ?? 0)
				.FirstOrDefault();

			if (bestTarget == null) {
				continue;
			}

			// Check if same file already exists on target
			var existsOnTarget = await _romFileRepository.ExistsByPathAsync(
				bestTarget.Id, rom.RelativePath, cancellationToken);

			if (existsOnTarget) {
				continue;
			}

			var reason = !sourceDrive.IsOnline
				? "Source drive is offline"
				: "Consolidation suggestion";

			suggestions.Add(new RelocationSuggestion(
				rom,
				sourceDrive,
				bestTarget,
				rom.RelativePath,
				reason));
		}

		return suggestions;
	}

	private async Task CheckDrivesAsync() {
		try {
			await RefreshAllDrivesAsync();
		} catch (Exception ex) {
			_logger.LogError(ex, "Error checking drives");
		}
	}

	private static bool IsDriveOnline(string rootPath) {
		try {
			// For network paths, try a quick existence check
			if (rootPath.StartsWith(@"\\", StringComparison.Ordinal)) {
				return Directory.Exists(rootPath);
			}

			// For local drives, check if the drive is ready
			var driveLetter = Path.GetPathRoot(rootPath);
			if (!string.IsNullOrEmpty(driveLetter)) {
				var driveInfo = new DriveInfo(driveLetter);
				return driveInfo.IsReady && Directory.Exists(rootPath);
			}

			return Directory.Exists(rootPath);
		} catch {
			return false;
		}
	}

	public void Dispose() {
		_monitorTimer?.Dispose();
	}
}
