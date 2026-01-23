using System.Text.RegularExpressions;
using Subrom.Application.Interfaces;
using Subrom.Domain.Aggregates.DatFiles;
using Subrom.Domain.ValueObjects;

namespace Subrom.Infrastructure.Services;

/// <summary>
/// Service for identifying bad ROM dumps by comparing against DAT databases
/// and detecting bad dump markers in filenames.
/// </summary>
public sealed partial class BadDumpService : IBadDumpService {
	private readonly IDatFileRepository _datFileRepository;

	public BadDumpService(IDatFileRepository datFileRepository) {
		_datFileRepository = datFileRepository;
	}

	/// <inheritdoc />
	public async Task<BadDumpResult> CheckByHashAsync(
		RomHashes hashes,
		CancellationToken cancellationToken = default) {
		var matches = await _datFileRepository.FindRomsByHashAsync(hashes, cancellationToken);

		if (matches.Count == 0) {
			return BadDumpResult.Unknown();
		}

		// If any match is marked as bad dump, return that
		var badDump = matches.FirstOrDefault(m => m.RomEntry.Status == RomStatus.BadDump);
		if (badDump is not null) {
			return BadDumpResult.FromDatMatch(badDump.RomEntry, badDump.GameEntry, badDump.DatFile);
		}

		// Return first good match
		var goodMatch = matches[0];
		return BadDumpResult.FromDatMatch(goodMatch.RomEntry, goodMatch.GameEntry, goodMatch.DatFile);
	}

	/// <inheritdoc />
	public async Task<IReadOnlyDictionary<ScannedRomEntry, BadDumpResult>> CheckBatchAsync(
		IEnumerable<ScannedRomEntry> entries,
		CancellationToken cancellationToken = default) {
		var entryList = entries.ToList();
		if (entryList.Count == 0) {
			return new Dictionary<ScannedRomEntry, BadDumpResult>();
		}

		// Batch lookup all hashes
		var hashes = entryList.Select(e => e.Hashes).ToList();
		var matches = await _datFileRepository.FindRomsByHashesAsync(hashes, cancellationToken);

		// Group matches by hash for quick lookup
		var matchByHash = new Dictionary<string, List<DatRomMatch>>();
		foreach (var match in matches) {
			// Index by all hash types
			AddToHashIndex(matchByHash, match.RomEntry.Crc, match);
			AddToHashIndex(matchByHash, match.RomEntry.Md5, match);
			AddToHashIndex(matchByHash, match.RomEntry.Sha1, match);
		}

		var results = new Dictionary<ScannedRomEntry, BadDumpResult>();
		foreach (var entry in entryList) {
			// Analyze filename for flags
			var fileNameAnalysis = AnalyzeFileName(entry.FileName);

			// Try to find DAT match
			var datMatches = FindMatchesForEntry(matchByHash, entry.Hashes);

			if (datMatches.Count == 0) {
				// No DAT match - rely on filename analysis
				results[entry] = fileNameAnalysis.HasConcerningFlags
					? BadDumpResult.FromFileName(fileNameAnalysis.Flags)
					: BadDumpResult.Unknown(fileNameAnalysis.Flags);

				continue;
			}

			// Check if any DAT match is a bad dump
			var badDump = datMatches.FirstOrDefault(m => m.RomEntry.Status == RomStatus.BadDump);
			if (badDump is not null) {
				var source = fileNameAnalysis.Flags.HasFlag(FileNameFlags.BadDump)
					? BadDumpSource.Combined
					: BadDumpSource.DatFile;

				results[entry] = new BadDumpResult(
					RomStatus.BadDump,
					source,
					badDump.RomEntry,
					badDump.GameEntry,
					badDump.DatFile,
					fileNameAnalysis.Flags);
			} else {
				// Good DAT match
				var goodMatch = datMatches[0];
				results[entry] = new BadDumpResult(
					goodMatch.RomEntry.Status,
					BadDumpSource.DatFile,
					goodMatch.RomEntry,
					goodMatch.GameEntry,
					goodMatch.DatFile,
					fileNameAnalysis.Flags);
			}
		}

		return results;
	}

	/// <inheritdoc />
	public FileNameAnalysis AnalyzeFileName(string fileName) {
		if (string.IsNullOrEmpty(fileName)) {
			return new FileNameAnalysis(fileName, fileName, FileNameFlags.None, null, null, null);
		}

		var flags = FileNameFlags.None;
		int? alternateVersion = null;
		int? badDumpVersion = null;
		string? region = null;

		// Extract base name without extension
		var name = Path.GetFileNameWithoutExtension(fileName);
		var cleanName = name;

		// Detect GoodTools-style single-letter flags: [!], [b], [a], etc.
		// Also detect numbered variants: [b1], [a2], etc.
		if (VerifiedFlagRegex().IsMatch(name)) {
			flags |= FileNameFlags.Verified;
			cleanName = VerifiedFlagRegex().Replace(cleanName, "");
		}

		var badDumpMatch = BadDumpFlagRegex().Match(name);
		if (badDumpMatch.Success) {
			flags |= FileNameFlags.BadDump;
			if (badDumpMatch.Groups[1].Success && int.TryParse(badDumpMatch.Groups[1].Value, out var bv)) {
				badDumpVersion = bv;
			}

			cleanName = BadDumpFlagRegex().Replace(cleanName, "");
		}

		var alternateMatch = AlternateFlagRegex().Match(name);
		if (alternateMatch.Success) {
			flags |= FileNameFlags.Alternate;
			if (alternateMatch.Groups[1].Success && int.TryParse(alternateMatch.Groups[1].Value, out var av)) {
				alternateVersion = av;
			}

			cleanName = AlternateFlagRegex().Replace(cleanName, "");
		}

		if (OverdumpFlagRegex().IsMatch(name)) {
			flags |= FileNameFlags.Overdump;
			cleanName = OverdumpFlagRegex().Replace(cleanName, "");
		}

		if (HackFlagRegex().IsMatch(name)) {
			flags |= FileNameFlags.Hack;
			cleanName = HackFlagRegex().Replace(cleanName, "");
		}

		if (PirateFlagRegex().IsMatch(name)) {
			flags |= FileNameFlags.Pirate;
			cleanName = PirateFlagRegex().Replace(cleanName, "");
		}

		if (TrainerFlagRegex().IsMatch(name)) {
			flags |= FileNameFlags.Trainer;
			cleanName = TrainerFlagRegex().Replace(cleanName, "");
		}

		if (FixedFlagRegex().IsMatch(name)) {
			flags |= FileNameFlags.Fixed;
			cleanName = FixedFlagRegex().Replace(cleanName, "");
		}

		if (TranslationFlagRegex().IsMatch(name)) {
			flags |= FileNameFlags.Translation;
			cleanName = TranslationFlagRegex().Replace(cleanName, "");
		}

		if (CrackedFlagRegex().IsMatch(name)) {
			flags |= FileNameFlags.Cracked;
			cleanName = CrackedFlagRegex().Replace(cleanName, "");
		}

		if (BadChecksumFlagRegex().IsMatch(name)) {
			flags |= FileNameFlags.BadChecksum;
			cleanName = BadChecksumFlagRegex().Replace(cleanName, "");
		}

		// Detect parenthetical flags
		if (UnlicensedRegex().IsMatch(name)) {
			flags |= FileNameFlags.Unlicensed;
		}

		if (PrototypeRegex().IsMatch(name)) {
			flags |= FileNameFlags.Prototype;
		}

		if (BetaRegex().IsMatch(name)) {
			flags |= FileNameFlags.Beta;
		}

		if (SampleRegex().IsMatch(name)) {
			flags |= FileNameFlags.Sample;
		}

		if (DemoRegex().IsMatch(name)) {
			flags |= FileNameFlags.Demo;
		}

		if (PublicDomainRegex().IsMatch(name)) {
			flags |= FileNameFlags.PublicDomain;
		}

		// Extract region
		var regionMatch = RegionRegex().Match(name);
		if (regionMatch.Success) {
			region = regionMatch.Groups[1].Value;
		}

		cleanName = cleanName.Trim();

		return new FileNameAnalysis(
			fileName,
			cleanName,
			flags,
			region,
			alternateVersion,
			badDumpVersion);
	}

	/// <inheritdoc />
	public async Task<IReadOnlyList<BadDumpEntry>> FindBadDumpsAsync(
		IEnumerable<ScannedRomEntry> entries,
		CancellationToken cancellationToken = default) {
		var results = await CheckBatchAsync(entries, cancellationToken);

		return results
			.Where(kvp => kvp.Value.IsBadDump || kvp.Value.FileNameFlags.HasFlag(FileNameFlags.BadDump))
			.Select(kvp => new BadDumpEntry(kvp.Key, kvp.Value))
			.ToList();
	}

	private static void AddToHashIndex(Dictionary<string, List<DatRomMatch>> index, string? hash, DatRomMatch match) {
		if (string.IsNullOrEmpty(hash)) return;

		if (!index.TryGetValue(hash, out var list)) {
			list = [];
			index[hash] = list;
		}

		list.Add(match);
	}

	private static List<DatRomMatch> FindMatchesForEntry(Dictionary<string, List<DatRomMatch>> matchByHash, RomHashes hashes) {
		var matches = new List<DatRomMatch>();

		var sha1 = hashes.Sha1.Value;
		var md5 = hashes.Md5.Value;
		var crc = hashes.Crc.Value;

		// Check SHA1 first (most reliable)
		if (!string.IsNullOrEmpty(sha1) && matchByHash.TryGetValue(sha1, out var sha1Matches)) {
			matches.AddRange(sha1Matches);
		}

		// Then MD5
		if (!string.IsNullOrEmpty(md5) && matchByHash.TryGetValue(md5, out var md5Matches)) {
			foreach (var m in md5Matches) {
				if (!matches.Contains(m)) matches.Add(m);
			}
		}

		// Then CRC32
		if (!string.IsNullOrEmpty(crc) && matchByHash.TryGetValue(crc, out var crcMatches)) {
			foreach (var m in crcMatches) {
				if (!matches.Contains(m)) matches.Add(m);
			}
		}

		return matches;
	}

	// GoodTools-style flags regex patterns (case-insensitive)
	[GeneratedRegex(@"\[!\]", RegexOptions.IgnoreCase)]
	private static partial Regex VerifiedFlagRegex();

	[GeneratedRegex(@"\[b(\d*)\]", RegexOptions.IgnoreCase)]
	private static partial Regex BadDumpFlagRegex();

	[GeneratedRegex(@"\[a(\d*)\]", RegexOptions.IgnoreCase)]
	private static partial Regex AlternateFlagRegex();

	[GeneratedRegex(@"\[o\d*\]", RegexOptions.IgnoreCase)]
	private static partial Regex OverdumpFlagRegex();

	[GeneratedRegex(@"\[h\d*\w*\]", RegexOptions.IgnoreCase)]
	private static partial Regex HackFlagRegex();

	[GeneratedRegex(@"\[p\d*\]", RegexOptions.IgnoreCase)]
	private static partial Regex PirateFlagRegex();

	[GeneratedRegex(@"\[t\d*\]", RegexOptions.IgnoreCase)]
	private static partial Regex TrainerFlagRegex();

	[GeneratedRegex(@"\[f\d*\]", RegexOptions.IgnoreCase)]
	private static partial Regex FixedFlagRegex();

	[GeneratedRegex(@"\[T[-+\w]*\]", RegexOptions.IgnoreCase)]
	private static partial Regex TranslationFlagRegex();

	[GeneratedRegex(@"\[c\]", RegexOptions.IgnoreCase)]
	private static partial Regex CrackedFlagRegex();

	[GeneratedRegex(@"\[x\]", RegexOptions.IgnoreCase)]
	private static partial Regex BadChecksumFlagRegex();

	// Parenthetical flags
	[GeneratedRegex(@"\(Unl\)", RegexOptions.IgnoreCase)]
	private static partial Regex UnlicensedRegex();

	[GeneratedRegex(@"\(Proto\)", RegexOptions.IgnoreCase)]
	private static partial Regex PrototypeRegex();

	[GeneratedRegex(@"\(Beta\)", RegexOptions.IgnoreCase)]
	private static partial Regex BetaRegex();

	[GeneratedRegex(@"\(Sample\)", RegexOptions.IgnoreCase)]
	private static partial Regex SampleRegex();

	[GeneratedRegex(@"\(Demo\)", RegexOptions.IgnoreCase)]
	private static partial Regex DemoRegex();

	[GeneratedRegex(@"\(PD\)", RegexOptions.IgnoreCase)]
	private static partial Regex PublicDomainRegex();

	// Region extraction (common formats)
	[GeneratedRegex(@"\(([UJEKFSGIACBDHNWP]{1,3}|USA|Europe|Japan|World|Germany|France|Spain|Italy|Korea|Brazil|Australia|Netherlands|Sweden|China)\)", RegexOptions.IgnoreCase)]
	private static partial Regex RegionRegex();
}
