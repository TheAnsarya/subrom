namespace Subrom.Application.Interfaces;

/// <summary>
/// Service for monitoring memory pressure and managing resources during large operations.
/// Used to prevent OutOfMemory conditions when processing 500K+ files.
/// </summary>
public interface IMemoryPressureMonitor {
	/// <summary>
	/// Gets current memory usage statistics.
	/// </summary>
	MemoryStats GetCurrentStats();

	/// <summary>
	/// Checks if memory pressure is high enough to pause operations.
	/// </summary>
	bool IsUnderPressure { get; }

	/// <summary>
	/// Gets the recommended batch size based on current memory conditions.
	/// </summary>
	int GetRecommendedBatchSize(int defaultBatchSize = 1000);

	/// <summary>
	/// Requests that operations reduce memory usage.
	/// </summary>
	void RequestMemoryReduction();

	/// <summary>
	/// Waits until memory pressure is reduced to acceptable levels.
	/// </summary>
	/// <param name="timeout">Maximum time to wait.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>True if pressure was reduced, false if timeout.</returns>
	Task<bool> WaitForMemoryReliefAsync(TimeSpan timeout, CancellationToken cancellationToken = default);

	/// <summary>
	/// Event raised when memory pressure changes significantly.
	/// </summary>
	event EventHandler<MemoryPressureChangedEventArgs>? PressureChanged;
}

/// <summary>
/// Current memory usage statistics.
/// </summary>
public record MemoryStats(
	long TotalMemoryBytes,
	long UsedMemoryBytes,
	long AvailableMemoryBytes,
	double UsagePercentage,
	long Gen0Collections,
	long Gen1Collections,
	long Gen2Collections,
	long ManagedHeapBytes,
	MemoryPressureLevel Level);

/// <summary>
/// Memory pressure levels.
/// </summary>
public enum MemoryPressureLevel {
	/// <summary>Memory usage is low, full speed operations allowed.</summary>
	Low,
	/// <summary>Memory usage is moderate, operations proceed normally.</summary>
	Normal,
	/// <summary>Memory usage is elevated, reduce batch sizes.</summary>
	Elevated,
	/// <summary>Memory usage is high, minimize allocations.</summary>
	High,
	/// <summary>Memory usage is critical, pause non-essential operations.</summary>
	Critical
}

/// <summary>
/// Event args for memory pressure changes.
/// </summary>
public record MemoryPressureChangedEventArgs(
	MemoryPressureLevel PreviousLevel,
	MemoryPressureLevel CurrentLevel,
	MemoryStats Stats);
