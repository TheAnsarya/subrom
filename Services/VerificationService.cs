using Microsoft.Extensions.Logging;
using Subrom.Domain.Datfiles;
using Subrom.Domain.Hash;

namespace Subrom.Services;

/// <summary>
/// Service for verifying ROM files against DAT entries.
/// </summary>
public class VerificationService : IVerificationService {
	private readonly ILogger<VerificationService> _logger;

	public VerificationService(ILogger<VerificationService> logger) {
		_logger = logger;
	}

	/// <summary>
	/// Verifies a scanned file against all loaded DAT files.
	/// </summary>
	public VerificationResult Verify(ScannedFile file, IEnumerable<Datafile> datfiles) {
		var result = new VerificationResult {
			FilePath = file.Path,
			FileName = file.FileName,
			Size = file.Size,
			Hashes = file.Hashes
		};

		if (file.Hashes == null) {
			result.Status = VerificationStatus.Error;
			result.Message = "No hashes computed";
			return result;
		}

		foreach (var datfile in datfiles) {
			var match = FindMatch(file, datfile);
			if (match != null) {
				result.Matches.Add(match);
			}
		}

		result.Status = result.Matches.Count > 0
			? VerificationStatus.Verified
			: VerificationStatus.Unknown;

		if (result.Matches.Count > 1) {
			_logger.LogDebug("File {File} matched {Count} DAT entries", file.FileName, result.Matches.Count);
		}

		return result;
	}

	/// <summary>
	/// Finds matching ROM entries in a DAT file.
	/// </summary>
	private DatMatch? FindMatch(ScannedFile file, Datafile datfile) {
		var hashes = file.Hashes!;

		// Search games
		foreach (var game in datfile.Games) {
			foreach (var rom in game.Roms) {
				if (IsMatch(hashes, rom)) {
					return new DatMatch {
						DatName = datfile.Header.Name,
						DatVersion = datfile.Header.Version,
						GameName = game.Name,
						GameDescription = game.Description,
						RomName = rom.Name,
						MatchType = DetermineMatchType(hashes, rom)
					};
				}
			}
		}

		// Search machines
		foreach (var machine in datfile.Machines) {
			foreach (var rom in machine.Roms) {
				if (IsMatch(hashes, rom)) {
					return new DatMatch {
						DatName = datfile.Header.Name,
						DatVersion = datfile.Header.Version,
						GameName = machine.Name,
						GameDescription = machine.Description,
						RomName = rom.Name,
						MatchType = DetermineMatchType(hashes, rom)
					};
				}
			}
		}

		return null;
	}

	/// <summary>
	/// Checks if hashes match a ROM entry.
	/// </summary>
	private static bool IsMatch(Hashes hashes, Rom rom) {
		// SHA1 is most reliable, check first if available
		if (!string.IsNullOrEmpty(rom.Sha1?.Value) && !string.IsNullOrEmpty(hashes.Sha1?.Value)) {
			return string.Equals(rom.Sha1.Value, hashes.Sha1.Value, StringComparison.OrdinalIgnoreCase);
		}

		// Then MD5
		if (!string.IsNullOrEmpty(rom.Md5?.Value) && !string.IsNullOrEmpty(hashes.Md5?.Value)) {
			return string.Equals(rom.Md5.Value, hashes.Md5.Value, StringComparison.OrdinalIgnoreCase);
		}

		// Finally CRC32 (most common but least reliable)
		if (!string.IsNullOrEmpty(rom.Crc?.Value) && !string.IsNullOrEmpty(hashes.Crc32?.Value)) {
			return string.Equals(rom.Crc.Value, hashes.Crc32.Value, StringComparison.OrdinalIgnoreCase);
		}

		return false;
	}

	/// <summary>
	/// Determines the match type based on which hashes matched.
	/// </summary>
	private static MatchType DetermineMatchType(Hashes hashes, Rom rom) {
		var sha1Match = !string.IsNullOrEmpty(rom.Sha1?.Value) &&
			string.Equals(rom.Sha1.Value, hashes.Sha1?.Value, StringComparison.OrdinalIgnoreCase);

		var md5Match = !string.IsNullOrEmpty(rom.Md5?.Value) &&
			string.Equals(rom.Md5.Value, hashes.Md5?.Value, StringComparison.OrdinalIgnoreCase);

		var crcMatch = !string.IsNullOrEmpty(rom.Crc?.Value) &&
			string.Equals(rom.Crc.Value, hashes.Crc32?.Value, StringComparison.OrdinalIgnoreCase);

		if (sha1Match && md5Match && crcMatch) return MatchType.ExactAll;
		if (sha1Match) return MatchType.Sha1;
		if (md5Match) return MatchType.Md5;
		if (crcMatch) return MatchType.Crc32;

		return MatchType.None;
	}

	/// <summary>
	/// Gets statistics about verification results.
	/// </summary>
	public VerificationStatistics GetStatistics(IEnumerable<VerificationResult> results) {
		var list = results.ToList();
		return new VerificationStatistics {
			TotalFiles = list.Count,
			Verified = list.Count(r => r.Status == VerificationStatus.Verified),
			Unknown = list.Count(r => r.Status == VerificationStatus.Unknown),
			Errors = list.Count(r => r.Status == VerificationStatus.Error),
			MultipleMatches = list.Count(r => r.Matches.Count > 1)
		};
	}
}

/// <summary>
/// Result of verifying a file against DAT files.
/// </summary>
public class VerificationResult {
	public string FilePath { get; set; } = "";
	public string FileName { get; set; } = "";
	public long Size { get; set; }
	public Hashes? Hashes { get; set; }
	public VerificationStatus Status { get; set; }
	public string? Message { get; set; }
	public List<DatMatch> Matches { get; set; } = new();
}

/// <summary>
/// A match in a DAT file.
/// </summary>
public class DatMatch {
	public string DatName { get; set; } = "";
	public string DatVersion { get; set; } = "";
	public string GameName { get; set; } = "";
	public string GameDescription { get; set; } = "";
	public string RomName { get; set; } = "";
	public MatchType MatchType { get; set; }
}

/// <summary>
/// Type of hash match.
/// </summary>
public enum MatchType {
	None,
	Crc32,
	Md5,
	Sha1,
	ExactAll
}

/// <summary>
/// Verification status.
/// </summary>
public enum VerificationStatus {
	Unknown,
	Verified,
	BadDump,
	Error
}

/// <summary>
/// Statistics about verification results.
/// </summary>
public class VerificationStatistics {
	public int TotalFiles { get; set; }
	public int Verified { get; set; }
	public int Unknown { get; set; }
	public int Errors { get; set; }
	public int MultipleMatches { get; set; }
}

/// <summary>
/// Interface for verification service.
/// </summary>
public interface IVerificationService {
	VerificationResult Verify(ScannedFile file, IEnumerable<Datafile> datfiles);
	VerificationStatistics GetStatistics(IEnumerable<VerificationResult> results);
}
