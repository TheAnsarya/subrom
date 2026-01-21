using Subrom.Application.Interfaces;
using Subrom.Domain.Aggregates.Storage;
using DriveType = Subrom.Domain.Aggregates.Storage.DriveType;

namespace Subrom.Application.Services;

/// <summary>
/// Application service for drive management.
/// </summary>
public sealed class DriveService {
	private readonly IDriveRepository _driveRepository;
	private readonly IUnitOfWork _unitOfWork;

	public DriveService(IDriveRepository driveRepository, IUnitOfWork unitOfWork) {
		_driveRepository = driveRepository;
		_unitOfWork = unitOfWork;
	}

	/// <summary>
	/// Registers a new drive for scanning.
	/// </summary>
	public async Task<Drive> RegisterAsync(
		string label,
		string rootPath,
		DriveType driveType = DriveType.Fixed,
		CancellationToken cancellationToken = default) {
		ArgumentException.ThrowIfNullOrWhiteSpace(label);
		ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);

		// Validate path exists (may fail for network paths that are offline)
		if (!Directory.Exists(rootPath)) {
			// For network paths, we allow registration even if currently offline
			if (!IsNetworkPath(rootPath)) {
				throw new DirectoryNotFoundException($"Root path does not exist: {rootPath}");
			}
		}

		var drive = Drive.Create(label, rootPath, driveType);

		// Try to get initial stats
		if (Directory.Exists(rootPath)) {
			try {
				var (totalSize, freeSpace) = GetDriveStats(rootPath);
				drive.MarkOnline(totalSize, freeSpace);
			} catch {
				drive.MarkOnline();
			}
		} else {
			drive.MarkOffline();
		}

		await _driveRepository.AddAsync(drive, cancellationToken);
		await _unitOfWork.SaveChangesAsync(cancellationToken);

		return drive;
	}

	/// <summary>
	/// Registers a network drive with UNC path.
	/// </summary>
	public async Task<Drive> RegisterNetworkDriveAsync(
		string label,
		string uncPath,
		CancellationToken cancellationToken = default) {
		ArgumentException.ThrowIfNullOrWhiteSpace(label);
		ArgumentException.ThrowIfNullOrWhiteSpace(uncPath);

		var drive = Drive.CreateNetworkDrive(label, uncPath);

		// Network drives may be offline at registration
		if (Directory.Exists(uncPath)) {
			drive.MarkOnline();
		} else {
			drive.MarkOffline();
		}

		await _driveRepository.AddAsync(drive, cancellationToken);
		await _unitOfWork.SaveChangesAsync(cancellationToken);

		return drive;
	}

	/// <summary>
	/// Gets all registered drives.
	/// </summary>
	public Task<IReadOnlyList<Drive>> GetAllAsync(CancellationToken cancellationToken = default) {
		return _driveRepository.GetAllAsync(cancellationToken);
	}

	/// <summary>
	/// Gets only online drives.
	/// </summary>
	public Task<IReadOnlyList<Drive>> GetOnlineAsync(CancellationToken cancellationToken = default) {
		return _driveRepository.GetOnlineAsync(cancellationToken);
	}

	/// <summary>
	/// Gets a drive by ID.
	/// </summary>
	public Task<Drive?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) {
		return _driveRepository.GetByIdAsync(id, cancellationToken);
	}

	/// <summary>
	/// Updates drive status (online/offline check).
	/// </summary>
	public async Task<Drive> RefreshStatusAsync(Guid id, CancellationToken cancellationToken = default) {
		var drive = await _driveRepository.GetByIdAsync(id, cancellationToken)
			?? throw new KeyNotFoundException($"Drive {id} not found.");

		// Check if root path exists
		var isOnline = Directory.Exists(drive.RootPath);

		if (isOnline) {
			var driveInfo = new DriveInfo(Path.GetPathRoot(drive.RootPath)!);
			drive.MarkOnline(driveInfo.TotalSize, driveInfo.AvailableFreeSpace);
		} else {
			drive.MarkOffline();
		}

		await _driveRepository.UpdateAsync(drive, cancellationToken);
		await _unitOfWork.SaveChangesAsync(cancellationToken);

		return drive;
	}

	/// <summary>
	/// Refreshes status of all drives.
	/// </summary>
	public async Task<IReadOnlyList<Drive>> RefreshAllStatusAsync(CancellationToken cancellationToken = default) {
		var drives = await _driveRepository.GetAllAsync(cancellationToken);
		var results = new List<Drive>(drives.Count);

		foreach (var drive in drives) {
			var isOnline = Directory.Exists(drive.RootPath);

			if (isOnline) {
				try {
					var driveInfo = new DriveInfo(Path.GetPathRoot(drive.RootPath)!);
					drive.MarkOnline(driveInfo.TotalSize, driveInfo.AvailableFreeSpace);
				} catch {
					drive.MarkOnline();
				}
			} else {
				drive.MarkOffline();
			}

			await _driveRepository.UpdateAsync(drive, cancellationToken);
			results.Add(drive);
		}

		await _unitOfWork.SaveChangesAsync(cancellationToken);

		return results;
	}

	/// <summary>
	/// Adds a relative scan path to a drive.
	/// </summary>
	public async Task<Drive> AddScanPathAsync(
		Guid id,
		string relativePath,
		CancellationToken cancellationToken = default) {
		ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

		var drive = await _driveRepository.GetByIdAsync(id, cancellationToken)
			?? throw new KeyNotFoundException($"Drive {id} not found.");

		var fullPath = drive.GetFullPath(relativePath);
		if (!Directory.Exists(fullPath)) {
			throw new DirectoryNotFoundException($"Path does not exist: {fullPath}");
		}

		drive.ScanPaths.Add(relativePath);

		await _driveRepository.UpdateAsync(drive, cancellationToken);
		await _unitOfWork.SaveChangesAsync(cancellationToken);

		return drive;
	}

	/// <summary>
	/// Removes a scan path from a drive.
	/// </summary>
	public async Task<Drive> RemoveScanPathAsync(
		Guid id,
		string relativePath,
		CancellationToken cancellationToken = default) {
		var drive = await _driveRepository.GetByIdAsync(id, cancellationToken)
			?? throw new KeyNotFoundException($"Drive {id} not found.");

		drive.ScanPaths.Remove(relativePath);

		await _driveRepository.UpdateAsync(drive, cancellationToken);
		await _unitOfWork.SaveChangesAsync(cancellationToken);

		return drive;
	}

	/// <summary>
	/// Deletes a drive.
	/// </summary>
	public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) {
		var drive = await _driveRepository.GetByIdAsync(id, cancellationToken)
			?? throw new KeyNotFoundException($"Drive {id} not found.");

		await _driveRepository.RemoveAsync(drive, cancellationToken);
		await _unitOfWork.SaveChangesAsync(cancellationToken);
	}

	/// <summary>
	/// Checks if a path is a network path (UNC or mapped network drive).
	/// </summary>
	private static bool IsNetworkPath(string path) {
		// UNC path
		if (path.StartsWith(@"\\", StringComparison.Ordinal)) {
			return true;
		}

		// Check if it's a mapped network drive
		try {
			var root = Path.GetPathRoot(path);
			if (!string.IsNullOrEmpty(root)) {
				var driveInfo = new DriveInfo(root);
				return driveInfo.DriveType == System.IO.DriveType.Network;
			}
		} catch {
			// Ignore errors
		}

		return false;
	}

	/// <summary>
	/// Gets drive statistics (total size, free space).
	/// </summary>
	private static (long? TotalSize, long? FreeSpace) GetDriveStats(string path) {
		try {
			// For UNC paths, we can't easily get space info without P/Invoke
			if (path.StartsWith(@"\\", StringComparison.Ordinal)) {
				return (null, null);
			}

			var root = Path.GetPathRoot(path);
			if (!string.IsNullOrEmpty(root)) {
				var driveInfo = new DriveInfo(root);
				return (driveInfo.TotalSize, driveInfo.AvailableFreeSpace);
			}
		} catch {
			// Ignore errors
		}

		return (null, null);
	}
}
