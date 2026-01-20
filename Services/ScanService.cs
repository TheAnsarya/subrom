using System.Collections.Concurrent;
using System.Threading.Channels;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Subrom.Domain.Storage;
using Subrom.Services.Interfaces;

namespace Subrom.Services;

/// <summary>
/// Background service that processes file scan jobs from a queue.
/// Scans directories for ROM files, computes hashes, and stores results.
/// </summary>
public sealed class ScanService : BackgroundService, IScanService {
	private readonly IHashService _hashService;
	private readonly ILogger<ScanService> _logger;
	private readonly Channel<ScanJob> _jobChannel;
	private readonly ConcurrentDictionary<Guid, ScanJob> _activeJobs = new();
	private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _jobCancellations = new();
	private ScanProgressBroadcaster? _broadcaster;

	// Common ROM file extensions
	private static readonly HashSet<string> RomExtensions = new(StringComparer.OrdinalIgnoreCase) {
		".nes", ".sfc", ".smc", ".gb", ".gbc", ".gba", ".nds", ".n64", ".z64", ".v64",
		".gen", ".md", ".smd", ".gg", ".sms", ".32x", ".pce", ".iso", ".cue", ".bin",
		".a26", ".a78", ".lnx", ".jag", ".ngp", ".ngc", ".ws", ".wsc", ".vb",
		".zip", ".7z", ".rar"
	};

	public ScanService(IHashService hashService, ILogger<ScanService> logger) {
		_hashService = hashService ?? throw new ArgumentNullException(nameof(hashService));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_jobChannel = Channel.CreateUnbounded<ScanJob>(new UnboundedChannelOptions {
			SingleReader = true,
			SingleWriter = false,
		});
	}

	/// <summary>
	/// Sets the SignalR broadcaster for progress updates.
	/// </summary>
	public void SetBroadcaster(ScanProgressBroadcaster broadcaster) {
		_broadcaster = broadcaster;
	}

	public ValueTask<ScanJob> EnqueueScanAsync(string rootPath, Guid? driveId = null, bool recursive = true, bool verifyHashes = true) {
		var job = ScanJob.Create(rootPath, driveId, recursive, verifyHashes);
		_activeJobs[job.Id] = job;
		_jobCancellations[job.Id] = new CancellationTokenSource();

		if (!_jobChannel.Writer.TryWrite(job)) {
			throw new InvalidOperationException("Failed to enqueue scan job");
		}

		_logger.LogInformation("Scan job {JobId} enqueued for path: {Path}", job.Id, rootPath);
		return ValueTask.FromResult(job);
	}

	public ScanJob? GetJob(Guid jobId) {
		return _activeJobs.TryGetValue(jobId, out var job) ? job : null;
	}

	public IEnumerable<ScanJob> GetActiveJobs() {
		return _activeJobs.Values.ToList();
	}

	public bool CancelJob(Guid jobId) {
		if (_jobCancellations.TryGetValue(jobId, out var cts)) {
			cts.Cancel();
			_logger.LogInformation("Scan job {JobId} cancellation requested", jobId);
			return true;
		}
		return false;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		_logger.LogInformation("ScanService started");

		await foreach (var job in _jobChannel.Reader.ReadAllAsync(stoppingToken)) {
			try {
				await ProcessScanJobAsync(job, stoppingToken);
			}
			catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
				_logger.LogInformation("ScanService stopping");
				break;
			}
			catch (Exception ex) {
				_logger.LogError(ex, "Error processing scan job {JobId}", job.Id);
				await UpdateJobStatusAsync(job.Id, ScanJobStatus.Failed);
			}
		}

		_logger.LogInformation("ScanService stopped");
	}

	private async Task ProcessScanJobAsync(ScanJob job, CancellationToken stoppingToken) {
		if (!_jobCancellations.TryGetValue(job.Id, out var jobCts)) {
			return;
		}

		using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, jobCts.Token);
		var ct = linkedCts.Token;

		_logger.LogInformation("Starting scan job {JobId} for path: {Path}", job.Id, job.RootPath);
		await UpdateJobStatusAsync(job.Id, ScanJobStatus.Running, startedAt: DateTime.UtcNow);
		await BroadcastAsync(job.Id, "ScanStarted");

		try {
			if (!Directory.Exists(job.RootPath)) {
				_logger.LogWarning("Scan path does not exist: {Path}", job.RootPath);
				await UpdateJobStatusAsync(job.Id, ScanJobStatus.Failed);
				await BroadcastAsync(job.Id, "ScanFailed");
				return;
			}

			// Count files first
			var files = EnumerateRomFiles(job.RootPath, job.Recursive).ToList();
			await UpdateJobProgressAsync(job.Id, totalFiles: files.Count);

			var processedFiles = 0;
			var verifiedFiles = 0;
			var errorFiles = 0;
			var scannedRoms = new List<ScannedRomInfo>();
			var lastBroadcast = DateTime.UtcNow;

			foreach (var filePath in files) {
				ct.ThrowIfCancellationRequested();

				await UpdateJobProgressAsync(job.Id, currentFile: filePath);

				try {
					var fileInfo = new FileInfo(filePath);
					ScannedRomInfo rom;

					if (job.VerifyHashes) {
						var hashes = await _hashService.GetAllFromFileAsync(filePath, ct);
						rom = new ScannedRomInfo(
							filePath,
							fileInfo.Name,
							fileInfo.Length,
							hashes.Crc32.Value,
							hashes.Md5.Value,
							hashes.Sha1.Value,
							fileInfo.LastWriteTimeUtc
						);
						verifiedFiles++;
					}
					else {
						rom = new ScannedRomInfo(
							filePath,
							fileInfo.Name,
							fileInfo.Length,
							null,
							null,
							null,
							fileInfo.LastWriteTimeUtc
						);
					}

					scannedRoms.Add(rom);
					processedFiles++;
					await UpdateJobProgressAsync(job.Id, processedFiles: processedFiles, verifiedFiles: verifiedFiles);

					// Broadcast progress every 500ms to avoid overwhelming clients
					if ((DateTime.UtcNow - lastBroadcast).TotalMilliseconds > 500) {
						await BroadcastAsync(job.Id, "ScanProgress");
						lastBroadcast = DateTime.UtcNow;
					}
				}
				catch (Exception ex) when (ex is not OperationCanceledException) {
					_logger.LogWarning(ex, "Error scanning file: {FilePath}", filePath);
					errorFiles++;
					processedFiles++;
					await UpdateJobProgressAsync(job.Id, processedFiles: processedFiles, errorFiles: errorFiles);
				}
			}

			// Store results
			if (_activeJobs.TryGetValue(job.Id, out var finalJob)) {
				finalJob = finalJob with {
					ScannedRoms = scannedRoms,
					CompletedAt = DateTime.UtcNow,
					Status = ct.IsCancellationRequested ? ScanJobStatus.Cancelled : ScanJobStatus.Completed,
				};
				_activeJobs[job.Id] = finalJob;
			}

			await BroadcastAsync(job.Id, "ScanCompleted");

			_logger.LogInformation(
				"Scan job {JobId} completed: {Processed} files, {Verified} verified, {Errors} errors",
				job.Id, processedFiles, verifiedFiles, errorFiles
			);
		}
		catch (OperationCanceledException) {
			_logger.LogInformation("Scan job {JobId} was cancelled", job.Id);
			await UpdateJobStatusAsync(job.Id, ScanJobStatus.Cancelled, completedAt: DateTime.UtcNow);
			await BroadcastAsync(job.Id, "ScanCancelled");
		}
		finally {
			_jobCancellations.TryRemove(job.Id, out _);
		}
	}

	private IEnumerable<string> EnumerateRomFiles(string rootPath, bool recursive) {
		var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

		return Directory.EnumerateFiles(rootPath, "*", searchOption)
			.Where(f => RomExtensions.Contains(Path.GetExtension(f)));
	}

	private async Task BroadcastAsync(Guid jobId, string eventName) {
		if (_broadcaster is not null && _activeJobs.TryGetValue(jobId, out var job)) {
			try {
				await _broadcaster(job, eventName);
			}
			catch (Exception ex) {
				_logger.LogWarning(ex, "Failed to broadcast {Event} for job {JobId}", eventName, jobId);
			}
		}
	}

	private Task UpdateJobStatusAsync(Guid jobId, ScanJobStatus status, DateTime? startedAt = null, DateTime? completedAt = null) {
		if (_activeJobs.TryGetValue(jobId, out var job)) {
			job = job with {
				Status = status,
				StartedAt = startedAt ?? job.StartedAt,
				CompletedAt = completedAt ?? job.CompletedAt,
			};
			_activeJobs[jobId] = job;
		}
		return Task.CompletedTask;
	}

	private Task UpdateJobProgressAsync(
		Guid jobId,
		int? totalFiles = null,
		int? processedFiles = null,
		int? verifiedFiles = null,
		int? errorFiles = null,
		string? currentFile = null) {
		if (_activeJobs.TryGetValue(jobId, out var job)) {
			job = job with {
				TotalFiles = totalFiles ?? job.TotalFiles,
				ProcessedFiles = processedFiles ?? job.ProcessedFiles,
				VerifiedFiles = verifiedFiles ?? job.VerifiedFiles,
				ErrorFiles = errorFiles ?? job.ErrorFiles,
				CurrentFile = currentFile ?? job.CurrentFile,
			};
			_activeJobs[jobId] = job;
		}
		return Task.CompletedTask;
	}
}
