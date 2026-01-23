using System.Collections.Concurrent;
using Subrom.Application.Interfaces;
using Subrom.Domain.Aggregates.Scanning;
using Subrom.Domain.Aggregates.Storage;
using DomainDriveType = Subrom.Domain.Aggregates.Storage.DriveType;

namespace Subrom.Application.Services;

/// <summary>
/// Configuration for parallel scanning across drives.
/// </summary>
public sealed class ParallelScanOptions {
	/// <summary>
	/// Maximum number of drives to scan simultaneously.
	/// Default: 3 (optimal for most systems with SSD + HDD mix)
	/// </summary>
	public int MaxConcurrentDrives { get; set; } = 3;

	/// <summary>
	/// Maximum number of files to hash in parallel per drive.
	/// Default: 4 (balance between I/O and CPU usage)
	/// </summary>
	public int MaxConcurrentHashesPerDrive { get; set; } = 4;

	/// <summary>
	/// Whether to prioritize SSD drives (scan them first as they're faster).
	/// </summary>
	public bool PrioritizeSsdDrives { get; set; } = true;
}

/// <summary>
/// Progress information for a multi-drive parallel scan.
/// </summary>
public sealed class MultiDriveScanProgress {
	public required Guid BatchId { get; init; }
	public required int TotalDrives { get; init; }
	public required int CompletedDrives { get; init; }
	public required Dictionary<Guid, ScanProgress> DriveProgress { get; init; }
	public bool IsComplete => CompletedDrives >= TotalDrives;
}

/// <summary>
/// Service for parallel scanning across multiple drives to maximize I/O throughput.
/// </summary>
public sealed class ParallelScanService {
	private readonly IDriveRepository _driveRepository;
	private readonly IRomFileRepository _romFileRepository;
	private readonly IScanJobRepository _scanJobRepository;
	private readonly IHashService _hashService;
	private readonly IUnitOfWork _unitOfWork;

	private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _activeBatches = new();

	public ParallelScanService(
		IDriveRepository driveRepository,
		IRomFileRepository romFileRepository,
		IScanJobRepository scanJobRepository,
		IHashService hashService,
		IUnitOfWork unitOfWork) {
		_driveRepository = driveRepository;
		_romFileRepository = romFileRepository;
		_scanJobRepository = scanJobRepository;
		_hashService = hashService;
		_unitOfWork = unitOfWork;
	}

	/// <summary>
	/// Starts a parallel scan across multiple drives.
	/// </summary>
	/// <param name="driveIds">IDs of drives to scan.</param>
	/// <param name="scanType">Type of scan to perform.</param>
	/// <param name="options">Parallel scan options.</param>
	/// <param name="progress">Progress callback for updates.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A batch ID that can be used to track/cancel the operation.</returns>
	public async Task<Guid> StartParallelScanAsync(
		IReadOnlyList<Guid> driveIds,
		ScanType scanType = ScanType.Full,
		ParallelScanOptions? options = null,
		IProgress<MultiDriveScanProgress>? progress = null,
		CancellationToken cancellationToken = default) {
		if (driveIds.Count == 0) {
			throw new ArgumentException("At least one drive must be specified.", nameof(driveIds));
		}

		options ??= new ParallelScanOptions();

		var batchId = Guid.NewGuid();
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		_activeBatches[batchId] = cts;

		try {
			// Get all drives and validate they're online
			var drives = new List<Drive>();
			foreach (var driveId in driveIds) {
				var drive = await _driveRepository.GetByIdAsync(driveId, cancellationToken)
					?? throw new KeyNotFoundException($"Drive {driveId} not found.");

				if (!drive.IsOnline) {
					throw new InvalidOperationException($"Drive '{drive.Label}' is offline.");
				}

				drives.Add(drive);
			}

			// Sort drives - SSDs/Removable first if prioritized
			if (options.PrioritizeSsdDrives) {
				drives = [.. drives.OrderByDescending(d => d.DriveType == DomainDriveType.Removable)
							  .ThenBy(d => d.Label)];
			}

			// Create scan jobs for each drive
			var jobs = new List<ScanJob>();
			foreach (var drive in drives) {
				var job = ScanJob.Create(scanType, drive.Id);
				await _scanJobRepository.AddAsync(job, cancellationToken);
				jobs.Add(job);
			}
			await _unitOfWork.SaveChangesAsync(cancellationToken);

			// Track progress for each drive
			var driveProgress = new ConcurrentDictionary<Guid, ScanProgress>();
			var completedCount = 0;

			// Use SemaphoreSlim to limit concurrent drives
			using var semaphore = new SemaphoreSlim(options.MaxConcurrentDrives);

			// Process drives in parallel with controlled concurrency
			var tasks = drives.Zip(jobs).Select(async pair => {
				var (drive, job) = pair;

				await semaphore.WaitAsync(cts.Token);
				try {
					var driveProgressReporter = new Progress<ScanProgress>(p => {
						driveProgress[drive.Id] = p;

						// Report aggregate progress
						progress?.Report(new MultiDriveScanProgress {
							BatchId = batchId,
							TotalDrives = drives.Count,
							CompletedDrives = completedCount,
							DriveProgress = new Dictionary<Guid, ScanProgress>(driveProgress)
						});
					});

					await ExecuteScanForDriveAsync(
						drive,
						job,
						options.MaxConcurrentHashesPerDrive,
						driveProgressReporter,
						cts.Token);

					Interlocked.Increment(ref completedCount);
				} finally {
					semaphore.Release();
				}
			});

			await Task.WhenAll(tasks);

			// Report final progress
			progress?.Report(new MultiDriveScanProgress {
				BatchId = batchId,
				TotalDrives = drives.Count,
				CompletedDrives = drives.Count,
				DriveProgress = new Dictionary<Guid, ScanProgress>(driveProgress)
			});

			return batchId;
		} finally {
			_activeBatches.TryRemove(batchId, out _);
		}
	}

	/// <summary>
	/// Scans all online drives in parallel.
	/// </summary>
	public async Task<Guid> ScanAllDrivesAsync(
		ScanType scanType = ScanType.Full,
		ParallelScanOptions? options = null,
		IProgress<MultiDriveScanProgress>? progress = null,
		CancellationToken cancellationToken = default) {
		var drives = await _driveRepository.GetAllAsync(cancellationToken);
		var onlineDriveIds = drives.Where(d => d.IsOnline).Select(d => d.Id).ToList();

		if (onlineDriveIds.Count == 0) {
			throw new InvalidOperationException("No online drives found.");
		}

		return await StartParallelScanAsync(onlineDriveIds, scanType, options, progress, cancellationToken);
	}

	/// <summary>
	/// Cancels a parallel scan batch.
	/// </summary>
	public async Task CancelBatchAsync(Guid batchId) {
		if (_activeBatches.TryGetValue(batchId, out var cts)) {
			await cts.CancelAsync();
		}
	}

	private async Task ExecuteScanForDriveAsync(
		Drive drive,
		ScanJob job,
		int maxConcurrentHashes,
		IProgress<ScanProgress>? progress,
		CancellationToken cancellationToken) {
		try {
			// Mark as running
			job.Start();
			await _scanJobRepository.UpdateAsync(job, cancellationToken);
			await _unitOfWork.SaveChangesAsync(cancellationToken);

			// Discover files
			progress?.Report(new ScanProgress { Phase = ScanPhase.Discovering, JobId = job.Id });

			var discoveredFiles = await DiscoverFilesAsync(drive, drive.RootPath, cancellationToken);
			job.SetPhase("Discovering", discoveredFiles.Count);

			// Create RomFile entries
			progress?.Report(new ScanProgress {
				Phase = ScanPhase.Indexing,
				JobId = job.Id,
				TotalFiles = discoveredFiles.Count
			});

			var romFiles = new List<RomFile>();
			foreach (var file in discoveredFiles) {
				cancellationToken.ThrowIfCancellationRequested();

				if (await _romFileRepository.ExistsByPathAsync(drive.Id, file.RelativePath, cancellationToken)) {
					continue;
				}

				var romFile = RomFile.Create(
					drive.Id,
					file.RelativePath,
					file.Size,
					file.LastModified);

				romFiles.Add(romFile);
			}

			if (romFiles.Count > 0) {
				await _romFileRepository.AddRangeAsync(romFiles, cancellationToken);
			}

			// Hash files in parallel if full scan
			if (job.Type == ScanType.Full || job.Type == ScanType.Hashing) {
				progress?.Report(new ScanProgress {
					Phase = ScanPhase.Hashing,
					JobId = job.Id,
					TotalFiles = romFiles.Count
				});

				var processedCount = 0;
				using var hashSemaphore = new SemaphoreSlim(maxConcurrentHashes);

				// Hash multiple files in parallel per drive
				var hashTasks = romFiles.Select(async romFile => {
					await hashSemaphore.WaitAsync(cancellationToken);
					try {
						var fullPath = drive.GetFullPath(romFile.RelativePath);
						var hashes = await _hashService.ComputeHashesAsync(fullPath, cancellationToken: cancellationToken);
						romFile.SetHashes(hashes);
						await _romFileRepository.UpdateAsync(romFile, cancellationToken);

						var count = Interlocked.Increment(ref processedCount);
						progress?.Report(new ScanProgress {
							Phase = ScanPhase.Hashing,
							JobId = job.Id,
							ProcessedFiles = count,
							TotalFiles = romFiles.Count,
							CurrentFile = romFile.FileName
						});
					} catch (Exception ex) {
						System.Diagnostics.Debug.WriteLine($"Hash error for {romFile.FileName}: {ex.Message}");
					} finally {
						hashSemaphore.Release();
					}
				});

				await Task.WhenAll(hashTasks);
			}

			// Complete
			job.Complete();
			await _scanJobRepository.UpdateAsync(job, cancellationToken);
			await _unitOfWork.SaveChangesAsync(cancellationToken);

			progress?.Report(new ScanProgress {
				Phase = ScanPhase.Complete,
				JobId = job.Id,
				ProcessedFiles = romFiles.Count,
				TotalFiles = romFiles.Count
			});
		} catch (OperationCanceledException) {
			job.Cancel();
			await _scanJobRepository.UpdateAsync(job, CancellationToken.None);
			await _unitOfWork.SaveChangesAsync(CancellationToken.None);
			throw;
		} catch (Exception ex) {
			job.Fail(ex.Message);
			await _scanJobRepository.UpdateAsync(job, CancellationToken.None);
			await _unitOfWork.SaveChangesAsync(CancellationToken.None);
			throw;
		}
	}

	private async Task<List<FileDiscovery>> DiscoverFilesAsync(
		Drive drive,
		string path,
		CancellationToken cancellationToken) {
		var files = new List<FileDiscovery>();

		await Task.Run(() => {
			var directoryInfo = new DirectoryInfo(path);
			if (!directoryInfo.Exists) {
				return;
			}

			foreach (var file in directoryInfo.EnumerateFiles("*", new EnumerationOptions {
				RecurseSubdirectories = true,
				IgnoreInaccessible = true
			})) {
				cancellationToken.ThrowIfCancellationRequested();

				var relativePath = Path.GetRelativePath(drive.RootPath, file.FullName);
				files.Add(new FileDiscovery {
					RelativePath = relativePath,
					Size = file.Length,
					LastModified = file.LastWriteTimeUtc
				});
			}
		}, cancellationToken);

		return files;
	}

	private sealed record FileDiscovery {
		public required string RelativePath { get; init; }
		public required long Size { get; init; }
		public required DateTime LastModified { get; init; }
	}
}
