using Subrom.Application.Interfaces;
using Subrom.Domain.Aggregates.DatFiles;

namespace Subrom.Infrastructure.Providers;

/// <summary>
/// DAT provider for No-Intro.
/// ⚠️ WARNING: Automated downloads are DISABLED due to IP ban from datomatic.no-intro.org
/// Ban occurred from automated scraping attempts. Manual download required.
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
			"⚠️ No-Intro downloads DISABLED. " +
			"IP banned from datomatic.no-intro.org due to automated scraping. " +
			"Please download DATs manually from https://datomatic.no-intro.org/ or contact shippa6@hotmail.com to lift ban.");
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
