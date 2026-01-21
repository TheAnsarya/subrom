using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Subrom.Application.Interfaces;

namespace Subrom.Infrastructure.Services;

/// <summary>
/// Monitors memory pressure and provides adaptive batch sizing for large operations.
/// Uses .NET GC memory pressure APIs and process memory information.
/// </summary>
public sealed class MemoryPressureMonitor : IMemoryPressureMonitor, IDisposable {
	private readonly ILogger<MemoryPressureMonitor> _logger;
	private readonly Timer _monitorTimer;
	private readonly object _lock = new();

	private MemoryPressureLevel _currentLevel = MemoryPressureLevel.Low;
	private MemoryStats _lastStats;
	private readonly SemaphoreSlim _reliefSignal = new(0);

	// Thresholds (percentage of available memory)
	private const double ElevatedThreshold = 70.0;
	private const double HighThreshold = 85.0;
	private const double CriticalThreshold = 95.0;

	public event EventHandler<MemoryPressureChangedEventArgs>? PressureChanged;

	public MemoryPressureMonitor(ILogger<MemoryPressureMonitor> logger) {
		_logger = logger;
		_lastStats = GetCurrentStats();

		// Monitor every 2 seconds
		_monitorTimer = new Timer(CheckMemoryPressure, null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));
	}

	public bool IsUnderPressure => _currentLevel >= MemoryPressureLevel.High;

	public MemoryStats GetCurrentStats() {
		var process = Process.GetCurrentProcess();
		var gcInfo = GC.GetGCMemoryInfo();

		var totalMemory = gcInfo.TotalAvailableMemoryBytes;
		var usedMemory = process.WorkingSet64;
		var availableMemory = totalMemory - usedMemory;
		var usagePercentage = totalMemory > 0 ? (double)usedMemory / totalMemory * 100.0 : 0.0;

		var level = usagePercentage switch {
			>= CriticalThreshold => MemoryPressureLevel.Critical,
			>= HighThreshold => MemoryPressureLevel.High,
			>= ElevatedThreshold => MemoryPressureLevel.Elevated,
			>= 50.0 => MemoryPressureLevel.Normal,
			_ => MemoryPressureLevel.Low
		};

		return new MemoryStats(
			totalMemory,
			usedMemory,
			availableMemory,
			usagePercentage,
			GC.CollectionCount(0),
			GC.CollectionCount(1),
			GC.CollectionCount(2),
			GC.GetTotalMemory(forceFullCollection: false),
			level);
	}

	public int GetRecommendedBatchSize(int defaultBatchSize = 1000) {
		return _currentLevel switch {
			MemoryPressureLevel.Critical => Math.Max(defaultBatchSize / 10, 50),
			MemoryPressureLevel.High => Math.Max(defaultBatchSize / 4, 100),
			MemoryPressureLevel.Elevated => Math.Max(defaultBatchSize / 2, 250),
			MemoryPressureLevel.Normal => defaultBatchSize,
			_ => defaultBatchSize
		};
	}

	public void RequestMemoryReduction() {
		_logger.LogInformation("Memory reduction requested, current level: {Level}", _currentLevel);

		// Request GC to collect if pressure is high
		if (_currentLevel >= MemoryPressureLevel.Elevated) {
			GC.Collect(2, GCCollectionMode.Optimized, blocking: false);
		}
	}

	public async Task<bool> WaitForMemoryReliefAsync(TimeSpan timeout, CancellationToken cancellationToken = default) {
		if (!IsUnderPressure) return true;

		_logger.LogInformation("Waiting for memory relief, current level: {Level}", _currentLevel);

		// First try GC
		GC.Collect(2, GCCollectionMode.Aggressive, blocking: true, compacting: true);

		// Check if that helped
		var stats = GetCurrentStats();
		if (stats.Level < MemoryPressureLevel.High) {
			return true;
		}

		// Wait for relief signal with timeout
		try {
			return await _reliefSignal.WaitAsync(timeout, cancellationToken);
		} catch (OperationCanceledException) {
			return false;
		}
	}

	private void CheckMemoryPressure(object? state) {
		try {
			var stats = GetCurrentStats();
			var previousLevel = _currentLevel;

			lock (_lock) {
				_lastStats = stats;
				_currentLevel = stats.Level;
			}

			// Fire event if level changed
			if (stats.Level != previousLevel) {
				_logger.LogInformation(
					"Memory pressure changed: {Previous} → {Current} ({Usage:F1}%)",
					previousLevel,
					stats.Level,
					stats.UsagePercentage);

				PressureChanged?.Invoke(this, new MemoryPressureChangedEventArgs(previousLevel, stats.Level, stats));

				// Signal relief if pressure decreased
				if (stats.Level < MemoryPressureLevel.High && previousLevel >= MemoryPressureLevel.High) {
					_reliefSignal.Release();
				}
			}

			// Log warnings for high pressure
			if (stats.Level == MemoryPressureLevel.Critical) {
				_logger.LogWarning(
					"CRITICAL memory pressure: {Usage:F1}% ({Used:N0} MB / {Total:N0} MB)",
					stats.UsagePercentage,
					stats.UsedMemoryBytes / 1024 / 1024,
					stats.TotalMemoryBytes / 1024 / 1024);
			}
		} catch (Exception ex) {
			_logger.LogError(ex, "Error checking memory pressure");
		}
	}

	public void Dispose() {
		_monitorTimer.Dispose();
		_reliefSignal.Dispose();
	}
}
