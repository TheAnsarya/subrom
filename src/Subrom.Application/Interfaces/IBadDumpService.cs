using Subrom.Domain.Aggregates.DatFiles;
using Subrom.Domain.ValueObjects;

namespace Subrom.Application.Interfaces;

/// <summary>
/// Service for identifying bad ROM dumps by comparing against DAT databases
/// and detecting bad dump markers in filenames.
/// </summary>
public interface IBadDumpService {
	/// <summary>
	/// Checks if a ROM is a known bad dump by matching its hashes against DAT files.
	/// </summary>
	/// <param name="hashes">ROM hashes to check</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Bad dump result with status and matched entry if found</returns>
	Task<BadDumpResult> CheckByHashAsync(
		RomHashes hashes,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Checks multiple ROMs for bad dump status in batch.
	/// </summary>
	/// <param name="entries">Scanned ROM entries to check</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Dictionary mapping entries to their bad dump results</returns>
	Task<IReadOnlyDictionary<ScannedRomEntry, BadDumpResult>> CheckBatchAsync(
		IEnumerable<ScannedRomEntry> entries,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Analyzes a filename for bad dump markers (GoodTools-style flags).
	/// </summary>
	/// <param name="fileName">Filename to analyze</param>
	/// <returns>Filename analysis result with detected flags</returns>
	FileNameAnalysis AnalyzeFileName(string fileName);

	/// <summary>
	/// Filters a collection of ROMs to return only bad dumps.
	/// </summary>
	/// <param name="entries">Scanned ROM entries to filter</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Only the entries identified as bad dumps</returns>
	Task<IReadOnlyList<BadDumpEntry>> FindBadDumpsAsync(
		IEnumerable<ScannedRomEntry> entries,
		CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of checking a ROM for bad dump status.
/// </summary>
/// <param name="Status">The dump status (Good, BadDump, NoDump, Verified)</param>
/// <param name="Source">How the status was determined</param>
/// <param name="MatchedRomEntry">The matching ROM entry from DAT file if found</param>
/// <param name="MatchedGameEntry">The matching game entry from DAT file if found</param>
/// <param name="DatFile">The DAT file that contained the match</param>
/// <param name="FileNameFlags">Flags detected in filename analysis</param>
public sealed record BadDumpResult(
	RomStatus Status,
	BadDumpSource Source,
	RomEntry? MatchedRomEntry = null,
	GameEntry? MatchedGameEntry = null,
	DatFile? DatFile = null,
	FileNameFlags FileNameFlags = FileNameFlags.None) {

	/// <summary>
	/// Whether this ROM is considered a bad dump.
	/// </summary>
	public bool IsBadDump => Status == RomStatus.BadDump;

	/// <summary>
	/// Whether this ROM is verified/good.
	/// </summary>
	public bool IsGood => Status is RomStatus.Good or RomStatus.Verified;

	/// <summary>
	/// Whether no match was found in any DAT file.
	/// </summary>
	public bool IsUnknown => Source == BadDumpSource.NoMatch;

	/// <summary>
	/// Creates a result for an unknown/unmatched ROM.
	/// </summary>
	public static BadDumpResult Unknown(FileNameFlags fileNameFlags = FileNameFlags.None) =>
		new(RomStatus.Good, BadDumpSource.NoMatch, FileNameFlags: fileNameFlags);

	/// <summary>
	/// Creates a result for a DAT-matched ROM.
	/// </summary>
	public static BadDumpResult FromDatMatch(
		RomEntry romEntry,
		GameEntry gameEntry,
		DatFile datFile) =>
		new(romEntry.Status, BadDumpSource.DatFile, romEntry, gameEntry, datFile);

	/// <summary>
	/// Creates a result detected from filename analysis.
	/// </summary>
	public static BadDumpResult FromFileName(FileNameFlags flags) =>
		new(flags.HasFlag(FileNameFlags.BadDump) ? RomStatus.BadDump : RomStatus.Good,
			BadDumpSource.FileName,
			FileNameFlags: flags);
}

/// <summary>
/// How the bad dump status was determined.
/// </summary>
public enum BadDumpSource {
	/// <summary>
	/// No match found in any DAT file.
	/// </summary>
	NoMatch,

	/// <summary>
	/// Status from DAT file entry.
	/// </summary>
	DatFile,

	/// <summary>
	/// Detected from filename markers.
	/// </summary>
	FileName,

	/// <summary>
	/// Combined analysis from both DAT and filename.
	/// </summary>
	Combined
}

/// <summary>
/// Flags detected in ROM filename.
/// Based on GoodTools naming convention.
/// </summary>
[Flags]
public enum FileNameFlags {
	None = 0,

	/// <summary>[!] - Verified good dump</summary>
	Verified = 1 << 0,

	/// <summary>[b] - Bad dump</summary>
	BadDump = 1 << 1,

	/// <summary>[a] - Alternate version</summary>
	Alternate = 1 << 2,

	/// <summary>[o] - Overdump</summary>
	Overdump = 1 << 3,

	/// <summary>[h] - Hack</summary>
	Hack = 1 << 4,

	/// <summary>[p] - Pirate</summary>
	Pirate = 1 << 5,

	/// <summary>[t] - Trainer</summary>
	Trainer = 1 << 6,

	/// <summary>[f] - Fixed</summary>
	Fixed = 1 << 7,

	/// <summary>[T] - Translation</summary>
	Translation = 1 << 8,

	/// <summary>[c] - Cracked</summary>
	Cracked = 1 << 9,

	/// <summary>[x] - Bad checksum</summary>
	BadChecksum = 1 << 10,

	/// <summary>(Unl) - Unlicensed</summary>
	Unlicensed = 1 << 11,

	/// <summary>(Proto) - Prototype</summary>
	Prototype = 1 << 12,

	/// <summary>(Beta) - Beta version</summary>
	Beta = 1 << 13,

	/// <summary>(Sample) - Sample version</summary>
	Sample = 1 << 14,

	/// <summary>(Demo) - Demo version</summary>
	Demo = 1 << 15,

	/// <summary>(PD) - Public domain</summary>
	PublicDomain = 1 << 16
}

/// <summary>
/// Result of analyzing a filename for ROM dump flags.
/// </summary>
/// <param name="OriginalFileName">The analyzed filename</param>
/// <param name="CleanName">Filename with flags removed</param>
/// <param name="Flags">Detected flags</param>
/// <param name="Region">Detected region if present</param>
/// <param name="AlternateVersion">Alternate version number if present (e.g., [a1], [a2])</param>
/// <param name="BadDumpVersion">Bad dump version number if present (e.g., [b1], [b2])</param>
public sealed record FileNameAnalysis(
	string OriginalFileName,
	string CleanName,
	FileNameFlags Flags,
	string? Region,
	int? AlternateVersion,
	int? BadDumpVersion) {

	/// <summary>
	/// Whether any concerning flags are present.
	/// </summary>
	public bool HasConcerningFlags =>
		Flags.HasFlag(FileNameFlags.BadDump) ||
		Flags.HasFlag(FileNameFlags.Overdump) ||
		Flags.HasFlag(FileNameFlags.BadChecksum);

	/// <summary>
	/// Whether verified flag is present.
	/// </summary>
	public bool IsVerified => Flags.HasFlag(FileNameFlags.Verified);

	/// <summary>
	/// Whether the ROM appears to be a hack/modification.
	/// </summary>
	public bool IsModified =>
		Flags.HasFlag(FileNameFlags.Hack) ||
		Flags.HasFlag(FileNameFlags.Translation) ||
		Flags.HasFlag(FileNameFlags.Fixed) ||
		Flags.HasFlag(FileNameFlags.Trainer) ||
		Flags.HasFlag(FileNameFlags.Cracked);
}

/// <summary>
/// A scanned ROM entry that has been identified as a bad dump.
/// </summary>
/// <param name="Entry">The original scanned ROM entry</param>
/// <param name="Result">The bad dump analysis result</param>
public sealed record BadDumpEntry(
	ScannedRomEntry Entry,
	BadDumpResult Result) {

	/// <summary>
	/// Gets a user-friendly reason for the bad dump identification.
	/// </summary>
	public string Reason => Result.Source switch {
		BadDumpSource.DatFile => $"Marked as bad dump in {Result.DatFile?.Name ?? "DAT file"}",
		BadDumpSource.FileName => $"Bad dump flag in filename: {Result.FileNameFlags}",
		BadDumpSource.Combined => "Bad dump confirmed by DAT and filename",
		_ => "Unknown bad dump"
	};
}
