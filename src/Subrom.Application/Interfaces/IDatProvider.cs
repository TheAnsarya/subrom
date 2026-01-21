using Subrom.Domain.Aggregates.DatFiles;

namespace Subrom.Application.Interfaces;

/// <summary>
/// Interface for DAT file providers (No-Intro, Redump, MAME, etc.).
/// </summary>
public interface IDatProvider {
	/// <summary>
	/// Provider type.
	/// </summary>
	DatProvider ProviderType { get; }

	/// <summary>
	/// Lists all available DATs from this provider.
	/// </summary>
	Task<IReadOnlyList<DatMetadata>> ListAvailableAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Downloads a DAT file by identifier.
	/// </summary>
	/// <param name="identifier">Provider-specific DAT identifier.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Stream containing the DAT file content.</returns>
	Task<Stream> DownloadDatAsync(string identifier, CancellationToken cancellationToken = default);

	/// <summary>
	/// Checks if a DAT identifier is supported by this provider.
	/// </summary>
	bool SupportsIdentifier(string identifier);
}
