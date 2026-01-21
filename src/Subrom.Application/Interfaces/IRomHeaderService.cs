namespace Subrom.Application.Interfaces;

/// <summary>
/// Service interface for detecting and removing ROM headers.
/// Many ROMs have headers added by copiers/dumpers that need to be
/// removed to calculate the correct hash for DAT matching.
/// </summary>
public interface IRomHeaderService {
	/// <summary>
	/// Detects if a ROM file has a header and returns information about it.
	/// </summary>
	/// <param name="stream">Stream to analyze (must be readable and seekable).</param>
	/// <param name="extension">File extension including dot (e.g., ".nes", ".smc").</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Header info if detected, null if no header present.</returns>
	Task<RomHeaderInfo?> DetectHeaderAsync(
		Stream stream,
		string extension,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets the header size for a known ROM format.
	/// Returns 0 if the format has no standard header.
	/// </summary>
	/// <param name="extension">File extension including dot (e.g., ".nes", ".smc").</param>
	/// <returns>Standard header size in bytes, or 0 if no header.</returns>
	int GetStandardHeaderSize(string extension);

	/// <summary>
	/// Checks if this service can handle the given ROM format.
	/// </summary>
	/// <param name="extension">File extension including dot.</param>
	/// <returns>True if headers can be detected for this format.</returns>
	bool SupportsFormat(string extension);

	/// <summary>
	/// Gets all supported ROM extensions.
	/// </summary>
	IReadOnlySet<string> SupportedExtensions { get; }
}

/// <summary>
/// Information about a detected ROM header.
/// </summary>
public sealed record RomHeaderInfo {
	/// <summary>
	/// Size of the header in bytes.
	/// </summary>
	public required int HeaderSize { get; init; }

	/// <summary>
	/// The ROM format (e.g., "iNES", "SMC", "A78").
	/// </summary>
	public required string Format { get; init; }

	/// <summary>
	/// Human-readable description of the header.
	/// </summary>
	public required string Description { get; init; }

	/// <summary>
	/// Whether this is a standard header for the format or an unusual/unknown header.
	/// </summary>
	public bool IsStandardHeader { get; init; } = true;

	/// <summary>
	/// Additional metadata extracted from the header, if any.
	/// </summary>
	public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
