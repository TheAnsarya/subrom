using System.Collections.Concurrent;
using Subrom.Application.Interfaces;
using Subrom.Domain.Aggregates.Scanning;
using Subrom.Domain.Aggregates.Storage;

namespace Subrom.Application.Services;

/// <summary>
/// Application service for file scanning operations.
/// </summary>
public sealed class ScanService {
	private readonly IDriveRepository _driveRepository;
	private readonly IRomFileRepository _romFileRepository;
	private readonly IScanJobRepository _scanJobRepository;
	private readonly IHashService _hashService;
	private readonly IUnitOfWork _unitOfWork;

	// Track active scan operations for cancellation
	private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _activeCancellations = new();

	public ScanService(
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
	/// Starts a new scan job for a drive.
	/// </summary>
	public async Task<ScanJob> StartScanAsync(
		Guid driveId,
		ScanType scanType = ScanType.Full,
		string? targetPath = null,
		CancellationToken cancellationToken = default) {
		var drive = await _driveRepository.GetByIdAsync(driveId, cancellationToken)
			?? throw new KeyNotFoundException($"Drive {driveId} not found.");

		if (!drive.IsOnline) {
			throw new InvalidOperationException($"Drive '{drive.Label}' is offline.");
		}

		// Check for active scan on this drive
		if (await _scanJobRepository.HasActiveJobForDriveAsync(driveId, cancellationToken)) {
			throw new InvalidOperationException($"A scan is already running for drive '{drive.Label}'.");
		}

		// Create the job
		var job = ScanJob.Create(scanType, driveId, targetPath);
		await _scanJobRepository.AddAsync(job, cancellationToken);
		await _unitOfWork.SaveChangesAsync(cancellationToken);

		return job;
	}

	/// <summary>
	/// Executes a scan job, discovering and optionally hashing files.
	/// </summary>
	public async Task ExecuteScanAsync(
		Guid jobId,
		IProgress<ScanProgress>? progress = null,
		CancellationToken cancellationToken = default) {
		var job = await _scanJobRepository.GetByIdAsync(jobId, cancellationToken)
			?? throw new KeyNotFoundException($"Scan job {jobId} not found.");

		var drive = await _driveRepository.GetByIdAsync(job.DriveId!.Value, cancellationToken)
			?? throw new KeyNotFoundException($"Drive {job.DriveId} not found.");

		// Set up cancellation tracking
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		_activeCancellations[jobId] = cts;

		try {
			// Mark as running
			job.Start();
			await _scanJobRepository.UpdateAsync(job, cancellationToken);
			await _unitOfWork.SaveChangesAsync(cancellationToken);

			// Get paths to scan
			var pathsToScan = GetScanPaths(drive, job.TargetPath);

			// Discover files
			progress?.Report(new ScanProgress { Phase = ScanPhase.Discovering, JobId = jobId });

			var discoveredFiles = new List<FileDiscovery>();
			foreach (var path in pathsToScan) {
				cts.Token.ThrowIfCancellationRequested();
				await DiscoverFilesAsync(drive, path, discoveredFiles, progress, cts.Token);
			}

			job.SetPhase("Discovering", discoveredFiles.Count);

			// Create RomFile entries
			progress?.Report(new ScanProgress {
				Phase = ScanPhase.Indexing,
				JobId = jobId,
				TotalFiles = discoveredFiles.Count
			});

			var romFiles = new List<RomFile>();
			foreach (var file in discoveredFiles) {
				cts.Token.ThrowIfCancellationRequested();

				// Skip if already exists
				if (await _romFileRepository.ExistsByPathAsync(drive.Id, file.RelativePath, cts.Token)) {
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
				await _romFileRepository.AddRangeAsync(romFiles, cts.Token);
			}

			// Hash files if full scan
			if (job.Type == ScanType.Full || job.Type == ScanType.Hashing) {
				progress?.Report(new ScanProgress {
					Phase = ScanPhase.Hashing,
					JobId = jobId,
					TotalFiles = romFiles.Count
				});

				var processedCount = 0;
				foreach (var romFile in romFiles) {
					cts.Token.ThrowIfCancellationRequested();

					try {
						var fullPath = drive.GetFullPath(romFile.RelativePath);
						var hashes = await _hashService.ComputeHashesAsync(fullPath, cancellationToken: cts.Token);
						romFile.SetHashes(hashes);
						await _romFileRepository.UpdateAsync(romFile, cts.Token);
					} catch (Exception ex) {
						// Log but continue
						System.Diagnostics.Debug.WriteLine($"Hash error for {romFile.FileName}: {ex.Message}");
					}

					processedCount++;
					progress?.Report(new ScanProgress {
						Phase = ScanPhase.Hashing,
						JobId = jobId,
						ProcessedFiles = processedCount,
						TotalFiles = romFiles.Count,
						CurrentFile = romFile.FileName
					});
				}
			}

			// Complete
			job.Complete();
			await _scanJobRepository.UpdateAsync(job, cts.Token);
			await _unitOfWork.SaveChangesAsync(cts.Token);

			progress?.Report(new ScanProgress {
				Phase = ScanPhase.Complete,
				JobId = jobId,
				ProcessedFiles = romFiles.Count,
				TotalFiles = romFiles.Count
			});
		} catch (OperationCanceledException) {
			job.Cancel();
			await _scanJobRepository.UpdateAsync(job, CancellationToken.None);
			await _unitOfWork.SaveChangesAsync(CancellationToken.None);

			progress?.Report(new ScanProgress { Phase = ScanPhase.Cancelled, JobId = jobId });
			throw;
		} catch (Exception ex) {
			job.Fail(ex.Message);
			await _scanJobRepository.UpdateAsync(job, CancellationToken.None);
			await _unitOfWork.SaveChangesAsync(CancellationToken.None);

			progress?.Report(new ScanProgress { Phase = ScanPhase.Failed, JobId = jobId, ErrorMessage = ex.Message });
			throw;
		} finally {
			_activeCancellations.TryRemove(jobId, out _);
		}
	}

	/// <summary>
	/// Cancels an active scan job.
	/// </summary>
	public async Task CancelScanAsync(Guid jobId, CancellationToken cancellationToken = default) {
		if (_activeCancellations.TryGetValue(jobId, out var cts)) {
			await cts.CancelAsync();
		} else {
			// Mark as cancelled in DB if not actively running
			var job = await _scanJobRepository.GetByIdAsync(jobId, cancellationToken);
			if (job is not null && (job.Status == ScanStatus.Running || job.Status == ScanStatus.Queued)) {
				job.Cancel();
				await _scanJobRepository.UpdateAsync(job, cancellationToken);
				await _unitOfWork.SaveChangesAsync(cancellationToken);
			}
		}
	}

	/// <summary>
	/// Gets all scan jobs, optionally filtered.
	/// </summary>
	public Task<IReadOnlyList<ScanJob>> GetJobsAsync(
		Guid? driveId = null,
		CancellationToken cancellationToken = default) {
		if (driveId.HasValue) {
			return _scanJobRepository.GetByDriveAsync(driveId.Value, cancellationToken);
		}

		return _scanJobRepository.GetAllAsync(cancellationToken);
	}

	/// <summary>
	/// Gets active scan jobs.
	/// </summary>
	public Task<IReadOnlyList<ScanJob>> GetActiveJobsAsync(CancellationToken cancellationToken = default) {
		return _scanJobRepository.GetActiveAsync(cancellationToken);
	}

	private static IEnumerable<string> GetScanPaths(Drive drive, string? targetPath) {
		if (!string.IsNullOrEmpty(targetPath)) {
			return [drive.GetFullPath(targetPath)];
		}

		if (drive.ScanPaths.Count > 0) {
			return drive.ScanPaths.Select(p => drive.GetFullPath(p));
		}

		return [drive.RootPath];
	}

	private static readonly string[] RomExtensions = [
		".zip", ".7z", ".rar",  // Archives
		".nes", ".snes", ".smc", ".sfc",  // Nintendo
		".gb", ".gbc", ".gba", ".nds", ".3ds",  // Nintendo handhelds
		".md", ".smd", ".gen", ".32x",  // Sega
		".gg", ".sms",  // Sega handhelds
		".pce", ".sgx",  // PC Engine
		".a26", ".a52", ".a78",  // Atari
		".lnx",  // Lynx
		".ngp", ".ngc",  // Neo Geo Pocket
		".ws", ".wsc",  // WonderSwan
		".iso", ".bin", ".cue", ".chd",  // Disc images
		".rom", ".bin"  // Generic
	];

	private static async Task DiscoverFilesAsync(
		Drive drive,
		string path,
		List<FileDiscovery> results,
		IProgress<ScanProgress>? progress,
		CancellationToken cancellationToken) {
		if (!Directory.Exists(path)) {
			return;
		}

		await Task.Run(() => {
			var options = new EnumerationOptions {
				RecurseSubdirectories = true,
				IgnoreInaccessible = true,
				AttributesToSkip = FileAttributes.System | FileAttributes.Hidden
			};

			foreach (var file in Directory.EnumerateFiles(path, "*", options)) {
				cancellationToken.ThrowIfCancellationRequested();

				var extension = Path.GetExtension(file).ToLowerInvariant();
				if (!RomExtensions.Contains(extension)) {
					continue;
				}

				try {
					var info = new FileInfo(file);
					var relativePath = Path.GetRelativePath(drive.RootPath, file);

					results.Add(new FileDiscovery {
						RelativePath = relativePath,
						Size = info.Length,
						LastModified = info.LastWriteTimeUtc
					});

					progress?.Report(new ScanProgress {
						Phase = ScanPhase.Discovering,
						TotalFiles = results.Count,
						CurrentFile = Path.GetFileName(file)
					});
				} catch {
					// Skip files we can't access
				}
			}
		}, cancellationToken);
	}

	private sealed class FileDiscovery {
		public required string RelativePath { get; init; }
		public required long Size { get; init; }
		public required DateTime LastModified { get; init; }
	}
}

/// <summary>
/// Progress information for scan operations.
/// </summary>
public sealed record ScanProgress {
	public required ScanPhase Phase { get; init; }
	public Guid JobId { get; init; }
	public int ProcessedFiles { get; init; }
	public int TotalFiles { get; init; }
	public string? CurrentFile { get; init; }
	public string? ErrorMessage { get; init; }
	public double Percentage => TotalFiles > 0 ? (double)ProcessedFiles / TotalFiles * 100 : 0;
}

/// <summary>
/// Phases of a scan operation.
/// </summary>
public enum ScanPhase {
	Queued,
	Discovering,
	Indexing,
	Hashing,
	Complete,
	Cancelled,
	Failed
}
