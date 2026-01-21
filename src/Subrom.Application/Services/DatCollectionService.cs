using Subrom.Application.Interfaces;
using Subrom.Domain.Aggregates.DatFiles;

namespace Subrom.Application.Services;

/// <summary>
/// Service for collecting and synchronizing DAT files from multiple providers.
/// </summary>
public sealed class DatCollectionService : IDatCollectionService {
	private readonly IEnumerable<IDatProvider> _providers;
	private readonly IDatFileRepository _datFileRepository;
	private readonly IDatParserFactory _parserFactory;
	private readonly IUnitOfWork _unitOfWork;

	public DatCollectionService(
		IEnumerable<IDatProvider> providers,
		IDatFileRepository datFileRepository,
		IDatParserFactory parserFactory,
		IUnitOfWork unitOfWork) {
		_providers = providers;
		_datFileRepository = datFileRepository;
		_parserFactory = parserFactory;
		_unitOfWork = unitOfWork;
	}

	public async Task<int> SyncProviderAsync(
		DatProvider provider,
		bool forceRefresh = false,
		IProgress<DatSyncProgress>? progress = null,
		CancellationToken cancellationToken = default) {
		var datProvider = _providers.FirstOrDefault(p => p.ProviderType == provider)
			?? throw new InvalidOperationException($"Provider {provider} not registered.");

		// Discover available DATs
		progress?.Report(new DatSyncProgress {
			Provider = provider,
			CurrentDat = "",
			ProcessedCount = 0,
			TotalCount = 0,
			Phase = DatSyncPhase.Discovering
		});

		var availableDats = await datProvider.ListAvailableAsync(cancellationToken);
		var existingDats = await _datFileRepository.GetByProviderAsync(provider, cancellationToken);
		var existingDict = existingDats.ToDictionary(d => d.FileName, StringComparer.OrdinalIgnoreCase);

		int updated = 0;

		for (int i = 0; i < availableDats.Count; i++) {
			var metadata = availableDats[i];
			var fileName = $"{metadata.Identifier}.dat";

			progress?.Report(new DatSyncProgress {
				Provider = provider,
				CurrentDat = metadata.Name,
				ProcessedCount = i,
				TotalCount = availableDats.Count,
				Phase = DatSyncPhase.Downloading
			});

			// Skip if exists and not forcing refresh
			if (!forceRefresh && existingDict.TryGetValue(fileName, out var existing)) {
				// Check if version changed
				if (existing.Version == metadata.Version) {
					continue;
				}
			}

			// Download DAT
			using var stream = await datProvider.DownloadDatAsync(metadata.Identifier, cancellationToken);

			progress?.Report(new DatSyncProgress {
				Provider = provider,
				CurrentDat = metadata.Name,
				ProcessedCount = i,
				TotalCount = availableDats.Count,
				Phase = DatSyncPhase.Parsing
			});

			// Parse DAT
			var parser = _parserFactory.GetParser(stream);
			if (parser is null) {
				continue; // Skip unsupported format
			}

			stream.Position = 0; // Reset for parsing
			var parsedDatFile = await parser.ParseAsync(stream, fileName, cancellationToken: cancellationToken);

			// Create or update DatFile
			DatFile datFile;
			if (existingDict.TryGetValue(fileName, out var existingDat)) {
				// Update existing
				datFile = existingDat;
				datFile.Description = metadata.Description ?? parsedDatFile.Description;
				datFile.Version = metadata.Version ?? parsedDatFile.Version;
				datFile.UpdatedAt = DateTime.UtcNow;

				await _datFileRepository.UpdateAsync(datFile, cancellationToken);
			} else {
				// Create new
				datFile = DatFile.Create(fileName, metadata.Name);
				datFile.Provider = provider;
				datFile.Description = metadata.Description ?? parsedDatFile.Description;
				datFile.Version = metadata.Version ?? parsedDatFile.Version;
				datFile.System = metadata.System;

				// Add games from parsed data
				datFile.AddGames(parsedDatFile.Games);

				await _datFileRepository.AddAsync(datFile, cancellationToken);
			}

			progress?.Report(new DatSyncProgress {
				Provider = provider,
				CurrentDat = metadata.Name,
				ProcessedCount = i + 1,
				TotalCount = availableDats.Count,
				Phase = DatSyncPhase.Saving
			});

			await _unitOfWork.SaveChangesAsync(cancellationToken);
			updated++;
		}

		progress?.Report(new DatSyncProgress {
			Provider = provider,
			CurrentDat = "",
			ProcessedCount = availableDats.Count,
			TotalCount = availableDats.Count,
			Phase = DatSyncPhase.Complete
		});

		return updated;
	}

	public async Task<DatSyncReport> SyncAllAsync(
		IProgress<DatSyncProgress>? progress = null,
		CancellationToken cancellationToken = default) {
		var startedAt = DateTime.UtcNow;
		var errors = new List<string>();
		int providersProcessed = 0;
		int totalUpdated = 0;

		foreach (var provider in _providers) {
			try {
				var updated = await SyncProviderAsync(provider.ProviderType, false, progress, cancellationToken);
				totalUpdated += updated;
				providersProcessed++;
			} catch (Exception ex) {
				errors.Add($"{provider.ProviderType}: {ex.Message}");
			}
		}

		return new DatSyncReport {
			StartedAt = startedAt,
			CompletedAt = DateTime.UtcNow,
			ProvidersProcessed = providersProcessed,
			DatsUpdated = totalUpdated,
			DatsAdded = totalUpdated, // TODO: track separately
			DatsSkipped = 0, // TODO: track skipped
			Errors = errors.Count,
			ErrorMessages = errors
		};
	}

	public async Task<IReadOnlyList<DatFile>> GetOutdatedDatsAsync(
		TimeSpan? maxAge = null,
		CancellationToken cancellationToken = default) {
		var threshold = DateTime.UtcNow - (maxAge ?? TimeSpan.FromDays(30));
		var allDats = await _datFileRepository.GetAllAsync(cancellationToken);

		return allDats
			.Where(d => d.UpdatedAt == null || d.UpdatedAt < threshold)
			.ToList();
	}

	public Task<IReadOnlyList<DatProvider>> GetAvailableProvidersAsync(CancellationToken cancellationToken = default) {
		var providers = _providers.Select(p => p.ProviderType).Distinct().ToList();
		return Task.FromResult<IReadOnlyList<DatProvider>>(providers);
	}
}
