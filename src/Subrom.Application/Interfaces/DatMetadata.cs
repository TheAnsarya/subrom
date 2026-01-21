namespace Subrom.Application.Interfaces;

/// <summary>
/// Metadata for an available DAT file from a provider.
/// </summary>
public sealed record DatMetadata {
	/// <summary>
	/// Unique identifier within the provider (e.g., "Nintendo - SNES").
	/// </summary>
	public required string Identifier { get; init; }

	/// <summary>
	/// Display name of the DAT.
	/// </summary>
	public required string Name { get; init; }

	/// <summary>
	/// Description of the DAT contents.
	/// </summary>
	public string? Description { get; init; }

	/// <summary>
	/// Version string (e.g., "20240115-120000").
	/// </summary>
	public string? Version { get; init; }

	/// <summary>
	/// Category/system this DAT covers.
	/// </summary>
	public string? System { get; init; }

	/// <summary>
	/// Direct download URL.
	/// </summary>
	public string? DownloadUrl { get; init; }

	/// <summary>
	/// File size in bytes.
	/// </summary>
	public long? FileSize { get; init; }

	/// <summary>
	/// Last update date from provider.
	/// </summary>
	public DateTime? LastUpdated { get; init; }

	/// <summary>
	/// Number of games in this DAT (if known).
	/// </summary>
	public int? GameCount { get; init; }
}
