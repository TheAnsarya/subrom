using Subrom.Domain.Aggregates.DatFiles;

namespace Subrom.Application.Interfaces;

/// <summary>
/// Service for collecting and synchronizing DAT files from multiple providers.
/// </summary>
public interface IDatCollectionService {
	/// <summary>
	/// Synchronizes DATs from a specific provider.
	/// </summary>
	/// <param name="provider">Provider to sync.</param>
	/// <param name="forceRefresh">Force re-download even if up-to-date.</param>
	/// <param name="progress">Progress callback.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Number of DATs updated.</returns>
	Task<int> SyncProviderAsync(
		DatProvider provider,
		bool forceRefresh = false,
		IProgress<DatSyncProgress>? progress = null,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Synchronizes all configured providers.
	/// </summary>
	Task<DatSyncReport> SyncAllAsync(
		IProgress<DatSyncProgress>? progress = null,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets DATs that are outdated and should be refreshed.
	/// </summary>
	Task<IReadOnlyList<DatFile>> GetOutdatedDatsAsync(
		TimeSpan? maxAge = null,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets available providers.
	/// </summary>
	Task<IReadOnlyList<DatProvider>> GetAvailableProvidersAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Progress information for DAT synchronization.
/// </summary>
public sealed record DatSyncProgress {
	public required DatProvider Provider { get; init; }
	public required string CurrentDat { get; init; }
	public int ProcessedCount { get; init; }
	public int TotalCount { get; init; }
	public DatSyncPhase Phase { get; init; }
}

/// <summary>
/// Sync phase.
/// </summary>
public enum DatSyncPhase {
	Discovering,
	Downloading,
	Parsing,
	Saving,
	Complete
}

/// <summary>
/// Summary report of DAT synchronization.
/// </summary>
public sealed record DatSyncReport {
	public required DateTime StartedAt { get; init; }
	public required DateTime CompletedAt { get; init; }
	public required int ProvidersProcessed { get; init; }
	public required int DatsUpdated { get; init; }
	public required int DatsAdded { get; init; }
	public required int DatsSkipped { get; init; }
	public required int Errors { get; init; }
	public required List<string> ErrorMessages { get; init; }

	public TimeSpan Duration => CompletedAt - StartedAt;
}
