using Subrom.Application.Interfaces;
using Subrom.Domain.Aggregates.DatFiles;

namespace Subrom.Infrastructure.Providers;

/// <summary>
/// DAT provider for No-Intro.
/// Note: No-Intro requires authentication via Datomatic.
/// This provider returns metadata but cannot auto-download without credentials.
/// </summary>
public sealed class NoIntroProvider : IDatProvider {
	public DatProvider ProviderType => DatProvider.NoIntro;

	public Task<IReadOnlyList<DatMetadata>> ListAvailableAsync(CancellationToken cancellationToken = default) {
		// Return known No-Intro systems
		var systems = new[] {
			"Nintendo - NES", "Nintendo - SNES", "Nintendo - N64", "Nintendo - GameCube",
			"Nintendo - Game Boy", "Nintendo - GBA", "Nintendo - DS", "Nintendo - 3DS",
			"Sega - Master System", "Sega - Genesis", "Sega - Game Gear", "Sega - Dreamcast",
			"Sony - PlayStation", "Sony - PS2", "Atari - 2600", "Atari - 7800",
			"NEC - PC Engine", "SNK - Neo Geo", "Bandai - WonderSwan"
		};

		var metadata = systems.Select(system => new DatMetadata {
			Identifier = system,
			Name = system,
			Description = $"No-Intro {system} DAT",
			System = system,
			DownloadUrl = null // Requires authentication
		}).ToList();

		return Task.FromResult<IReadOnlyList<DatMetadata>>(metadata);
	}

	public Task<Stream> DownloadDatAsync(string identifier, CancellationToken cancellationToken = default) {
		throw new NotSupportedException(
			"No-Intro requires Datomatic authentication. " +
			"Please download DATs manually from https://datomatic.no-intro.org/");
	}

	public bool SupportsIdentifier(string identifier) =>
		identifier.StartsWith("Nintendo -", StringComparison.OrdinalIgnoreCase) ||
		identifier.StartsWith("Sega -", StringComparison.OrdinalIgnoreCase) ||
		identifier.StartsWith("Sony -", StringComparison.OrdinalIgnoreCase) ||
		identifier.StartsWith("Atari -", StringComparison.OrdinalIgnoreCase) ||
		identifier.StartsWith("NEC -", StringComparison.OrdinalIgnoreCase) ||
		identifier.StartsWith("SNK -", StringComparison.OrdinalIgnoreCase) ||
		identifier.StartsWith("Bandai -", StringComparison.OrdinalIgnoreCase);
}
