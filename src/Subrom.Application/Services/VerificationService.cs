using Subrom.Application.Interfaces;
using Subrom.Domain.Aggregates.DatFiles;
using Subrom.Domain.Aggregates.Storage;
using Subrom.Domain.ValueObjects;

namespace Subrom.Application.Services;

/// <summary>
/// Application service for ROM verification.
/// </summary>
public sealed class VerificationService {
	private readonly IDatFileRepository _datFileRepository;
	private readonly IRomFileRepository _romFileRepository;
	private readonly IHashService _hashService;
	private readonly IUnitOfWork _unitOfWork;

	public VerificationService(
		IDatFileRepository datFileRepository,
		IRomFileRepository romFileRepository,
		IHashService hashService,
		IUnitOfWork unitOfWork) {
		_datFileRepository = datFileRepository;
		_romFileRepository = romFileRepository;
		_hashService = hashService;
		_unitOfWork = unitOfWork;
	}

	/// <summary>
	/// Verifies a single file against known ROMs.
	/// </summary>
	public async Task<VerificationResult> VerifyFileAsync(
		string filePath,
		IProgress<HashProgress>? hashProgress = null,
		CancellationToken cancellationToken = default) {
		ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

		if (!File.Exists(filePath)) {
			throw new FileNotFoundException("File not found.", filePath);
		}

		var fileInfo = new FileInfo(filePath);

		// Compute hashes
		var hashes = await _hashService.ComputeHashesAsync(filePath, hashProgress, cancellationToken);

		// Look up in database
		var match = await LookupByHashesAsync(hashes, cancellationToken);

		return new VerificationResult {
			FilePath = filePath,
			FileName = Path.GetFileName(filePath),
			FileSize = fileInfo.Length,
			Hashes = hashes,
			Match = match,
			VerifiedAt = DateTime.UtcNow
		};
	}

	/// <summary>
	/// Verifies an already-scanned ROM file record.
	/// </summary>
	public async Task<VerificationResult> VerifyRomFileAsync(
		Guid romFileId,
		CancellationToken cancellationToken = default) {
		var romFile = await _romFileRepository.GetByIdAsync(romFileId, cancellationToken)
			?? throw new KeyNotFoundException($"ROM file {romFileId} not found.");

		// If not hashed, we need the file on disk
		var hashes = romFile.GetHashes();
		if (hashes is null) {
			throw new InvalidOperationException($"ROM file {romFileId} has not been hashed yet.");
		}

		var hashesValue = hashes.Value;

		// Look up match
		var match = await LookupByHashesAsync(hashesValue, cancellationToken);

		// Update verification status
		if (match is not null) {
			romFile.MarkVerified(match.DatFileId, match.RomEntryId);
		} else {
			romFile.MarkUnverified();
		}

		await _romFileRepository.UpdateAsync(romFile, cancellationToken);
		await _unitOfWork.SaveChangesAsync(cancellationToken);

		return new VerificationResult {
			FilePath = romFile.RelativePath,
			FileName = romFile.FileName,
			FileSize = romFile.Size,
			Hashes = hashesValue,
			Match = match,
			VerifiedAt = DateTime.UtcNow
		};
	}

	/// <summary>
	/// Looks up a ROM by its hashes.
	/// </summary>
	public async Task<RomMatch?> LookupByHashesAsync(
		RomHashes hashes,
		CancellationToken cancellationToken = default) {
		// Get all enabled DAT files
		var datFiles = await _datFileRepository.GetAllAsync(cancellationToken);
		var enabledDats = datFiles.Where(d => d.IsEnabled).ToList();

		foreach (var datFile in enabledDats) {
			// Load with games
			var fullDat = await _datFileRepository.GetByIdWithGamesAsync(datFile.Id, cancellationToken);
			if (fullDat is null) continue;

			foreach (var game in fullDat.Games) {
				foreach (var rom in game.Roms) {
					// Match by SHA-1 first (most accurate), then CRC32
					if (!string.IsNullOrEmpty(hashes.Sha1.Value) &&
						string.Equals(hashes.Sha1.Value, rom.Sha1, StringComparison.OrdinalIgnoreCase)) {
						return new RomMatch {
							DatFileId = datFile.Id,
							DatFileName = datFile.Name,
							GameId = game.Id,
							GameName = game.Name,
							RomEntryId = rom.Id,
							RomName = rom.Name,
							MatchType = MatchType.Sha1
						};
					}

					// CRC32 match (faster but less accurate)
					if (!string.IsNullOrEmpty(hashes.Crc.Value) &&
						string.Equals(hashes.Crc.Value, rom.Crc, StringComparison.OrdinalIgnoreCase)) {
						return new RomMatch {
							DatFileId = datFile.Id,
							DatFileName = datFile.Name,
							GameId = game.Id,
							GameName = game.Name,
							RomEntryId = rom.Id,
							RomName = rom.Name,
							MatchType = MatchType.Crc32
						};
					}
				}
			}
		}

		return null;
	}

	/// <summary>
	/// Batch verifies multiple ROM files.
	/// </summary>
	public async Task<IReadOnlyList<VerificationResult>> VerifyBatchAsync(
		IEnumerable<Guid> romFileIds,
		IProgress<BatchVerificationProgress>? progress = null,
		CancellationToken cancellationToken = default) {
		var ids = romFileIds.ToList();
		var results = new List<VerificationResult>(ids.Count);
		var completed = 0;

		foreach (var id in ids) {
			cancellationToken.ThrowIfCancellationRequested();

			try {
				var result = await VerifyRomFileAsync(id, cancellationToken);
				results.Add(result);
			} catch (Exception ex) {
				results.Add(new VerificationResult {
					FilePath = id.ToString(),
					FileName = id.ToString(),
					FileSize = 0,
					Hashes = RomHashes.Empty,
					Match = null,
					VerifiedAt = DateTime.UtcNow,
					Error = ex.Message
				});
			}

			completed++;
			progress?.Report(new BatchVerificationProgress {
				Completed = completed,
				Total = ids.Count,
				LastFilePath = results[^1].FilePath
			});
		}

		return results;
	}
}

/// <summary>
/// Result of a verification operation.
/// </summary>
public sealed record VerificationResult {
	public required string FilePath { get; init; }
	public required string FileName { get; init; }
	public required long FileSize { get; init; }
	public required RomHashes Hashes { get; init; }
	public RomMatch? Match { get; init; }
	public required DateTime VerifiedAt { get; init; }
	public string? Error { get; init; }
	public bool IsMatch => Match is not null;
}

/// <summary>
/// Information about a matched ROM.
/// </summary>
public sealed record RomMatch {
	public required Guid DatFileId { get; init; }
	public required string DatFileName { get; init; }
	public required int GameId { get; init; }
	public required string GameName { get; init; }
	public required int RomEntryId { get; init; }
	public required string RomName { get; init; }
	public required MatchType MatchType { get; init; }
}

/// <summary>
/// How the ROM was matched.
/// </summary>
public enum MatchType {
	None,
	Crc32,
	Md5,
	Sha1,
	Sha256
}

/// <summary>
/// Progress for batch verification.
/// </summary>
public sealed record BatchVerificationProgress {
	public required int Completed { get; init; }
	public required int Total { get; init; }
	public required string LastFilePath { get; init; }
	public double Percentage => Total > 0 ? (double)Completed / Total * 100 : 0;
}
