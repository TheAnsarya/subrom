using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Subrom.Application.Interfaces;
using Subrom.Domain.ValueObjects;

namespace Subrom.Infrastructure.Services;

/// <summary>
/// Service for managing hash job queues with priority, cancellation, and caching.
/// Ideal for large disc images (4GB+) where hashing must be non-blocking.
/// </summary>
public sealed class HashJobService : IHashJobService, IDisposable {
	private readonly IHashService _hashService;
	private readonly ILogger<HashJobService> _logger;
	private readonly ConcurrentDictionary<Guid, HashJobEntry> _jobs = new();
	private readonly ConcurrentDictionary<string, HashCacheEntry> _hashCache = new();
	private readonly Channel<HashJobEntry> _highPriorityChannel;
	private readonly Channel<HashJobEntry> _normalChannel;
	private readonly Channel<HashJobEntry> _backgroundChannel;
	private readonly CancellationTokenSource _shutdownCts = new();
	private readonly Task[] _workerTasks;
	private readonly int _maxConcurrency;
	private int _inProgressCount;
	private int _completedCount;
	private int _failedCount;

	public event EventHandler<HashJobCompletedEventArgs>? JobCompleted;
	public event EventHandler<HashJobProgressEventArgs>? JobProgress;

	public HashJobService(IHashService hashService, ILogger<HashJobService> logger, int maxConcurrency = 2) {
		_hashService = hashService;
		_logger = logger;
		_maxConcurrency = maxConcurrency;

		// Unbounded channels with priority separation
		_highPriorityChannel = Channel.CreateUnbounded<HashJobEntry>(new UnboundedChannelOptions {
			SingleReader = false,
			SingleWriter = false
		});
		_normalChannel = Channel.CreateUnbounded<HashJobEntry>();
		_backgroundChannel = Channel.CreateUnbounded<HashJobEntry>();

		// Start worker tasks
		_workerTasks = Enumerable.Range(0, maxConcurrency)
			.Select(_ => Task.Run(ProcessJobsAsync))
			.ToArray();

		_logger.LogInformation("HashJobService started with {Concurrency} workers", maxConcurrency);
	}

	public Guid QueueHashJob(string filePath, HashJobPriority priority = HashJobPriority.Normal, int skipBytes = 0) {
		ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

		var fileInfo = new FileInfo(filePath);
		if (!fileInfo.Exists) {
			throw new FileNotFoundException("File not found", filePath);
		}

		var job = new HashJobEntry {
			JobId = Guid.NewGuid(),
			FilePath = filePath,
			Priority = priority,
			SkipBytes = skipBytes,
			State = HashJobState.Queued,
			QueuedAt = DateTime.UtcNow,
			TotalBytes = fileInfo.Length - skipBytes,
			Cts = CancellationTokenSource.CreateLinkedTokenSource(_shutdownCts.Token)
		};

		_jobs[job.JobId] = job;
		GetChannelForPriority(priority).Writer.TryWrite(job);

		_logger.LogDebug("Queued hash job {JobId} for {FilePath} ({Size:N0} bytes, priority {Priority})",
			job.JobId, filePath, job.TotalBytes, priority);

		return job.JobId;
	}

	public Guid QueueBatch(IEnumerable<string> filePaths, HashJobPriority priority = HashJobPriority.Normal) {
		var batchId = Guid.NewGuid();
		var paths = filePaths.ToList();

		foreach (var path in paths) {
			var jobId = QueueHashJob(path, priority);
			if (_jobs.TryGetValue(jobId, out var job)) {
				job.BatchId = batchId;
			}
		}

		_logger.LogInformation("Queued batch {BatchId} with {Count} files", batchId, paths.Count);
		return batchId;
	}

	public Task<HashJobStatus?> GetJobStatusAsync(Guid jobId, CancellationToken ct = default) {
		if (_jobs.TryGetValue(jobId, out var job)) {
			return Task.FromResult<HashJobStatus?>(job.ToStatus());
		}
		return Task.FromResult<HashJobStatus?>(null);
	}

	public Task<RomHashes?> GetJobResultAsync(Guid jobId, CancellationToken ct = default) {
		if (_jobs.TryGetValue(jobId, out var job) && job.State == HashJobState.Completed) {
			return Task.FromResult(job.Result);
		}
		return Task.FromResult<RomHashes?>(null);
	}

	public Task<bool> CancelJobAsync(Guid jobId) {
		if (_jobs.TryGetValue(jobId, out var job)) {
			if (job.State is HashJobState.Queued or HashJobState.InProgress) {
				job.Cts?.Cancel();
				job.State = HashJobState.Cancelled;
				_logger.LogDebug("Cancelled job {JobId}", jobId);
				return Task.FromResult(true);
			}
		}
		return Task.FromResult(false);
	}

	public Task CancelBatchAsync(Guid batchId) {
		foreach (var job in _jobs.Values.Where(j => j.BatchId == batchId)) {
			job.Cts?.Cancel();
			job.State = HashJobState.Cancelled;
		}
		_logger.LogDebug("Cancelled batch {BatchId}", batchId);
		return Task.CompletedTask;
	}

	public Task<RomHashes?> GetCachedHashesAsync(string filePath, CancellationToken ct = default) {
		if (_hashCache.TryGetValue(filePath, out var entry)) {
			var fileInfo = new FileInfo(filePath);
			// Validate cache by mtime and size
			if (fileInfo.Exists &&
				fileInfo.LastWriteTimeUtc == entry.ModifiedTime &&
				fileInfo.Length == entry.FileSize) {
				_logger.LogDebug("Cache hit for {FilePath}", filePath);
				return Task.FromResult<RomHashes?>(entry.Hashes);
			}
			// Invalid cache entry
			_hashCache.TryRemove(filePath, out _);
		}
		return Task.FromResult<RomHashes?>(null);
	}

	public Task InvalidateCacheAsync(string filePath) {
		_hashCache.TryRemove(filePath, out _);
		return Task.CompletedTask;
	}

	public HashQueueStats GetQueueStats() {
		var queued = _jobs.Values.Count(j => j.State == HashJobState.Queued);
		var totalQueued = _jobs.Values.Where(j => j.State == HashJobState.Queued).Sum(j => j.TotalBytes);
		var totalProcessed = _jobs.Values.Sum(j => j.BytesProcessed);

		return new HashQueueStats {
			QueuedCount = queued,
			InProgressCount = _inProgressCount,
			CompletedCount = _completedCount,
			FailedCount = _failedCount,
			TotalBytesQueued = totalQueued,
			TotalBytesProcessed = totalProcessed,
			MaxConcurrency = _maxConcurrency
		};
	}

	private Channel<HashJobEntry> GetChannelForPriority(HashJobPriority priority) => priority switch {
		HashJobPriority.Critical or HashJobPriority.High => _highPriorityChannel,
		HashJobPriority.Normal => _normalChannel,
		_ => _backgroundChannel
	};

	private async Task ProcessJobsAsync() {
		while (!_shutdownCts.IsCancellationRequested) {
			try {
				// Priority: High > Normal > Background
				var job = await GetNextJobAsync();
				if (job is null) continue;

				if (job.State == HashJobState.Cancelled) continue;

				job.State = HashJobState.InProgress;
				job.StartedAt = DateTime.UtcNow;
				Interlocked.Increment(ref _inProgressCount);

				try {
					var progress = new Progress<HashProgress>(p => {
						job.BytesProcessed = p.ProcessedBytes;
						JobProgress?.Invoke(this, new HashJobProgressEventArgs {
							JobId = job.JobId,
							FilePath = job.FilePath,
							BytesProcessed = p.ProcessedBytes,
							TotalBytes = job.TotalBytes
						});
					});

					var hashes = await _hashService.ComputeHashesAsync(
						job.FilePath,
						job.SkipBytes,
						progress,
						job.Cts?.Token ?? CancellationToken.None);

					job.Result = hashes;
					job.State = HashJobState.Completed;
					job.CompletedAt = DateTime.UtcNow;
					Interlocked.Increment(ref _completedCount);

					// Cache the result
					var fileInfo = new FileInfo(job.FilePath);
					_hashCache[job.FilePath] = new HashCacheEntry {
						Hashes = hashes,
						ModifiedTime = fileInfo.LastWriteTimeUtc,
						FileSize = fileInfo.Length
					};

					_logger.LogDebug("Completed hash job {JobId} in {Duration}",
						job.JobId, job.CompletedAt - job.StartedAt);

					JobCompleted?.Invoke(this, new HashJobCompletedEventArgs {
						JobId = job.JobId,
						FilePath = job.FilePath,
						State = HashJobState.Completed,
						Result = hashes,
						Duration = job.CompletedAt.Value - job.StartedAt.Value
					});
				}
				catch (OperationCanceledException) {
					job.State = HashJobState.Cancelled;
					_logger.LogDebug("Hash job {JobId} was cancelled", job.JobId);
				}
				catch (Exception ex) {
					job.State = HashJobState.Failed;
					job.ErrorMessage = ex.Message;
					job.CompletedAt = DateTime.UtcNow;
					Interlocked.Increment(ref _failedCount);

					_logger.LogWarning(ex, "Hash job {JobId} failed", job.JobId);

					JobCompleted?.Invoke(this, new HashJobCompletedEventArgs {
						JobId = job.JobId,
						FilePath = job.FilePath,
						State = HashJobState.Failed,
						ErrorMessage = ex.Message,
						Duration = (job.CompletedAt ?? DateTime.UtcNow) - job.StartedAt!.Value
					});
				}
				finally {
					Interlocked.Decrement(ref _inProgressCount);
				}
			}
			catch (OperationCanceledException) when (_shutdownCts.IsCancellationRequested) {
				break;
			}
			catch (Exception ex) {
				_logger.LogError(ex, "Error in hash job processor");
			}
		}
	}

	private async Task<HashJobEntry?> GetNextJobAsync() {
		// Try high priority first
		if (_highPriorityChannel.Reader.TryRead(out var highJob)) {
			return highJob;
		}

		// Then normal
		if (_normalChannel.Reader.TryRead(out var normalJob)) {
			return normalJob;
		}

		// Then background
		if (_backgroundChannel.Reader.TryRead(out var bgJob)) {
			return bgJob;
		}

		// Wait for any channel
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(_shutdownCts.Token);
		cts.CancelAfter(TimeSpan.FromSeconds(1));

		try {
			// Wait on high priority first
			return await _highPriorityChannel.Reader.ReadAsync(cts.Token);
		}
		catch (OperationCanceledException) {
			return null;
		}
	}

	public void Dispose() {
		_shutdownCts.Cancel();
		_highPriorityChannel.Writer.Complete();
		_normalChannel.Writer.Complete();
		_backgroundChannel.Writer.Complete();

		try {
			Task.WaitAll(_workerTasks, TimeSpan.FromSeconds(5));
		}
		catch { /* Ignore timeout */ }

		foreach (var job in _jobs.Values) {
			job.Cts?.Dispose();
		}
		_shutdownCts.Dispose();
	}

	private sealed class HashJobEntry {
		public required Guid JobId { get; init; }
		public required string FilePath { get; init; }
		public required HashJobPriority Priority { get; init; }
		public int SkipBytes { get; init; }
		public HashJobState State { get; set; }
		public Guid? BatchId { get; set; }
		public DateTime QueuedAt { get; init; }
		public DateTime? StartedAt { get; set; }
		public DateTime? CompletedAt { get; set; }
		public long TotalBytes { get; init; }
		public long BytesProcessed { get; set; }
		public RomHashes? Result { get; set; }
		public string? ErrorMessage { get; set; }
		public CancellationTokenSource? Cts { get; init; }

		public HashJobStatus ToStatus() => new() {
			JobId = JobId,
			FilePath = FilePath,
			State = State,
			Priority = Priority,
			BatchId = BatchId,
			BytesProcessed = BytesProcessed,
			TotalBytes = TotalBytes,
			QueuedAt = QueuedAt,
			StartedAt = StartedAt,
			CompletedAt = CompletedAt,
			ErrorMessage = ErrorMessage,
			Result = Result
		};
	}

	private sealed class HashCacheEntry {
		public required RomHashes Hashes { get; init; }
		public required DateTime ModifiedTime { get; init; }
		public required long FileSize { get; init; }
	}
}
