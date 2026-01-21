using Subrom.Domain.Aggregates.DatFiles;

namespace Subrom.Application.Interfaces;

/// <summary>
/// Service interface for parsing DAT files.
/// </summary>
public interface IDatParser {
	/// <summary>
	/// Gets the supported format for this parser.
	/// </summary>
	DatFormat Format { get; }

	/// <summary>
	/// Checks if this parser can handle the given file.
	/// </summary>
	/// <param name="filePath">Path to the DAT file.</param>
	/// <returns>True if this parser can handle the file.</returns>
	bool CanParse(string filePath);

	/// <summary>
	/// Parses a DAT file and returns the aggregate.
	/// </summary>
	/// <param name="filePath">Path to the DAT file.</param>
	/// <param name="progress">Optional progress callback.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Parsed DAT file aggregate.</returns>
	Task<DatFile> ParseAsync(
		string filePath,
		IProgress<DatParseProgress>? progress = null,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Parses a DAT file from a stream.
	/// </summary>
	/// <param name="stream">Stream containing DAT content.</param>
	/// <param name="fileName">Original filename.</param>
	/// <param name="progress">Optional progress callback.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Parsed DAT file aggregate.</returns>
	Task<DatFile> ParseAsync(
		Stream stream,
		string fileName,
		IProgress<DatParseProgress>? progress = null,
		CancellationToken cancellationToken = default);
}

/// <summary>
/// Factory for getting the appropriate DAT parser.
/// </summary>
public interface IDatParserFactory {
	/// <summary>
	/// Gets a parser for the given file.
	/// </summary>
	IDatParser? GetParser(string filePath);

	/// <summary>
	/// Gets a parser for the given format.
	/// </summary>
	IDatParser? GetParser(DatFormat format);

	/// <summary>
	/// Gets a parser for the given stream (peeks content to determine format).
	/// </summary>
	IDatParser? GetParser(Stream stream);
}

/// <summary>
/// Progress information for DAT parsing.
/// </summary>
public record DatParseProgress {
	/// <summary>
	/// Current phase of parsing.
	/// </summary>
	public required string Phase { get; init; }

	/// <summary>
	/// Games parsed so far.
	/// </summary>
	public int GamesParsed { get; init; }

	/// <summary>
	/// Total games (if known).
	/// </summary>
	public int? TotalGames { get; init; }

	/// <summary>
	/// ROMs parsed so far.
	/// </summary>
	public int RomsParsed { get; init; }

	/// <summary>
	/// Bytes read.
	/// </summary>
	public long BytesRead { get; init; }

	/// <summary>
	/// Total bytes (if known).
	/// </summary>
	public long? TotalBytes { get; init; }
}
