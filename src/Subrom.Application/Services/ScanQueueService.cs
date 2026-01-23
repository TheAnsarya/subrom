using System.Collections.Concurrent;
using System.Threading.Channels;
using Subrom.Domain.Aggregates.Storage;

namespace Subrom.Application.Services;

/// <summary>
/// Represents a queued scan item.
/// </summary>
public sealed record QueuedScan {
	public Guid Id { get; init; } = Guid.NewGuid();
	public required Guid DriveId { get; init; }
	public required string DriveLabel { get; init; }
	public required string RootPath { get; init; }
	public QueuedScanPriority Priority { get; init; } = QueuedScanPriority.Normal;
	public QueuedScanStatus Status { get; set; } = QueuedScanStatus.Pending;
	public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
	public DateTime? StartedAt { get; set; }
	public DateTime? CompletedAt { get; set; }
	public int FilesScanned { get; set; }
	public int TotalFiles { get; set; }
	public string? ErrorMessage { get; set; }
}

/// <summary>
/// Queued scan priority levels.
/// </summary>
public enum QueuedScanPriority {
	Low = 0,
	Normal = 1,
	High = 2
}

/// <summary>
/// Queued scan status.
/// </summary>
public enum QueuedScanStatus {
	Pending,
	Running,
	Paused,
	Completed,
	Failed,
	Cancelled
}

/// <summary>
/// Event args for scan status changes.
/// </summary>
public sealed class ScanStatusChangedEventArgs : EventArgs {
	public required QueuedScan Scan { get; init; }
	public required QueuedScanStatus OldStatus { get; init; }
	public required QueuedScanStatus NewStatus { get; init; }
}

/// <summary>
/// Service for managing scan queues with pause, resume, and priority ordering.
/// </summary>
public sealed class ScanQueueService : IDisposable {
	private readonly ConcurrentDictionary<Guid, QueuedScan> _scans = new();
	private readonly Channel<Guid> _scanChannel;
	private readonly PriorityQueue<Guid, (QueuedScanPriority Priority, DateTime Created)> _priorityQueue;
	private readonly SemaphoreSlim _queueLock = new(1, 1);
	private readonly CancellationTokenSource _cts = new();
	private bool _isDisposed;

	public event EventHandler<ScanStatusChangedEventArgs>? ScanStatusChanged;

	public ScanQueueService() {
		_scanChannel = Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions {
			SingleReader = true,
			SingleWriter = false
		});
		_priorityQueue = new PriorityQueue<Guid, (QueuedScanPriority, DateTime)>(
			Comparer<(QueuedScanPriority Priority, DateTime Created)>.Create((a, b) => {
				// Higher priority first, then earlier creation time
				var priorityCompare = b.Priority.CompareTo(a.Priority);
				return priorityCompare != 0 ? priorityCompare : a.Created.CompareTo(b.Created);
			}));
	}

	/// <summary>
	/// Gets whether the queue is paused.
	/// </summary>
	public bool IsPaused { get; private set; }

	/// <summary>
	/// Gets all scans in the queue.
	/// </summary>
	public IReadOnlyCollection<QueuedScan> GetAllScans() {
		return _scans.Values.ToList().AsReadOnly();
	}

	/// <summary>
	/// Gets scans by status.
	/// </summary>
	public IReadOnlyCollection<QueuedScan> GetScansByStatus(QueuedScanStatus status) {
		return _scans.Values.Where(s => s.Status == status).ToList().AsReadOnly();
	}

	/// <summary>
	/// Gets a specific scan by ID.
	/// </summary>
	public QueuedScan? GetScan(Guid scanId) {
		return _scans.GetValueOrDefault(scanId);
	}

	/// <summary>
	/// Enqueues a new scan.
	/// </summary>
	public async Task<QueuedScan> EnqueueAsync(Drive drive, QueuedScanPriority priority = QueuedScanPriority.Normal) {
		var scan = new QueuedScan {
			DriveId = drive.Id,
			DriveLabel = drive.Label,
			RootPath = drive.RootPath,
			Priority = priority
		};

		_scans[scan.Id] = scan;

		await _queueLock.WaitAsync();
		try {
			_priorityQueue.Enqueue(scan.Id, (priority, scan.CreatedAt));
		} finally {
			_queueLock.Release();
		}

		// Signal that a new scan is available
		await _scanChannel.Writer.WriteAsync(scan.Id, _cts.Token);

		return scan;
	}

	/// <summary>
	/// Cancels a specific scan.
	/// </summary>
	public bool CancelScan(Guid scanId) {
		if (!_scans.TryGetValue(scanId, out var scan)) {
			return false;
		}

		if (scan.Status is QueuedScanStatus.Completed or QueuedScanStatus.Failed or QueuedScanStatus.Cancelled) {
			return false;
		}

		var oldStatus = scan.Status;
		scan.Status = QueuedScanStatus.Cancelled;
		OnScanStatusChanged(scan, oldStatus, QueuedScanStatus.Cancelled);
		return true;
	}

	/// <summary>
	/// Pauses the entire queue.
	/// </summary>
	public void PauseQueue() {
		IsPaused = true;
	}

	/// <summary>
	/// Resumes the queue.
	/// </summary>
	public void ResumeQueue() {
		IsPaused = false;
	}

	/// <summary>
	/// Changes the priority of a pending scan.
	/// </summary>
	public async Task<bool> ChangePriorityAsync(Guid scanId, QueuedScanPriority newPriority) {
		if (!_scans.TryGetValue(scanId, out var scan)) {
			return false;
		}

		if (scan.Status != QueuedScanStatus.Pending) {
			return false;
		}

		await _queueLock.WaitAsync();
		try {
			// Rebuild queue with new priority
			var items = new List<(Guid Id, QueuedScanPriority Priority, DateTime Created)>();
			while (_priorityQueue.TryDequeue(out var id, out var priority)) {
				if (id == scanId) {
					items.Add((id, newPriority, scan.CreatedAt));
				} else {
					items.Add((id, priority.Priority, priority.Created));
				}
			}

			foreach (var (Id, Priority, Created) in items) {
				_priorityQueue.Enqueue(Id, (Priority, Created));
			}
		} finally {
			_queueLock.Release();
		}

		return true;
	}

	/// <summary>
	/// Moves a scan to the front of the queue (highest priority).
	/// </summary>
	public Task<bool> MoveToFrontAsync(Guid scanId) {
		return ChangePriorityAsync(scanId, QueuedScanPriority.High);
	}

	/// <summary>
	/// Moves a scan to the back of the queue (lowest priority).
	/// </summary>
	public Task<bool> MoveToBackAsync(Guid scanId) {
		return ChangePriorityAsync(scanId, QueuedScanPriority.Low);
	}

	/// <summary>
	/// Gets the next scan from the queue (for worker consumption).
	/// </summary>
	public async Task<QueuedScan?> DequeueAsync(CancellationToken cancellationToken = default) {
		using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cts.Token);

		// Wait until we're not paused and there's work
		while (!linkedCts.IsCancellationRequested) {
			if (IsPaused) {
				await Task.Delay(100, linkedCts.Token);
				continue;
			}

			await _queueLock.WaitAsync(linkedCts.Token);
			try {
				if (_priorityQueue.TryDequeue(out var scanId, out _)) {
					if (_scans.TryGetValue(scanId, out var scan)) {
						// Skip cancelled scans
						if (scan.Status == QueuedScanStatus.Cancelled) {
							continue;
						}

						var oldStatus = scan.Status;
						scan.Status = QueuedScanStatus.Running;
						scan.StartedAt = DateTime.UtcNow;
						OnScanStatusChanged(scan, oldStatus, QueuedScanStatus.Running);
						return scan;
					}
				}
			} finally {
				_queueLock.Release();
			}

			// Wait for more work
			try {
				await _scanChannel.Reader.WaitToReadAsync(linkedCts.Token);
				await _scanChannel.Reader.ReadAsync(linkedCts.Token);
			} catch (OperationCanceledException) {
				break;
			}
		}

		return null;
	}

	/// <summary>
	/// Marks a scan as completed.
	/// </summary>
	public void MarkCompleted(Guid scanId, int filesScanned) {
		if (_scans.TryGetValue(scanId, out var scan)) {
			var oldStatus = scan.Status;
			scan.Status = QueuedScanStatus.Completed;
			scan.CompletedAt = DateTime.UtcNow;
			scan.FilesScanned = filesScanned;
			OnScanStatusChanged(scan, oldStatus, QueuedScanStatus.Completed);
		}
	}

	/// <summary>
	/// Marks a scan as failed.
	/// </summary>
	public void MarkFailed(Guid scanId, string errorMessage) {
		if (_scans.TryGetValue(scanId, out var scan)) {
			var oldStatus = scan.Status;
			scan.Status = QueuedScanStatus.Failed;
			scan.CompletedAt = DateTime.UtcNow;
			scan.ErrorMessage = errorMessage;
			OnScanStatusChanged(scan, oldStatus, QueuedScanStatus.Failed);
		}
	}

	/// <summary>
	/// Clears completed and failed scans from history.
	/// </summary>
	public void ClearHistory() {
		var toRemove = _scans.Values
			.Where(s => s.Status is QueuedScanStatus.Completed or QueuedScanStatus.Failed or QueuedScanStatus.Cancelled)
			.Select(s => s.Id)
			.ToList();

		foreach (var id in toRemove) {
			_scans.TryRemove(id, out _);
		}
	}

	/// <summary>
	/// Gets queue statistics.
	/// </summary>
	public ScanQueueStats GetStats() {
		var scans = _scans.Values.ToList();
		return new ScanQueueStats {
			TotalScans = scans.Count,
			PendingScans = scans.Count(s => s.Status == QueuedScanStatus.Pending),
			RunningScans = scans.Count(s => s.Status == QueuedScanStatus.Running),
			CompletedScans = scans.Count(s => s.Status == QueuedScanStatus.Completed),
			FailedScans = scans.Count(s => s.Status == QueuedScanStatus.Failed),
			CancelledScans = scans.Count(s => s.Status == QueuedScanStatus.Cancelled),
			IsPaused = IsPaused
		};
	}

	private void OnScanStatusChanged(QueuedScan scan, QueuedScanStatus oldStatus, QueuedScanStatus newStatus) {
		ScanStatusChanged?.Invoke(this, new ScanStatusChangedEventArgs {
			Scan = scan,
			OldStatus = oldStatus,
			NewStatus = newStatus
		});
	}

	public void Dispose() {
		if (_isDisposed) {
			return;
		}

		_cts.Cancel();
		_cts.Dispose();
		_queueLock.Dispose();
		_scanChannel.Writer.Complete();
		_isDisposed = true;
	}
}

/// <summary>
/// Statistics about the scan queue.
/// </summary>
public sealed record ScanQueueStats {
	public int TotalScans { get; init; }
	public int PendingScans { get; init; }
	public int RunningScans { get; init; }
	public int CompletedScans { get; init; }
	public int FailedScans { get; init; }
	public int CancelledScans { get; init; }
	public bool IsPaused { get; init; }
}
