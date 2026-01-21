using Subrom.Application.Interfaces;
using Subrom.Domain.Aggregates.DatFiles;

namespace Subrom.Infrastructure.Providers;

/// <summary>
/// DAT provider for MAME (Multiple Arcade Machine Emulator).
/// Downloads from official MAME GitHub releases.
/// </summary>
public sealed class MameProvider : IDatProvider {
	private readonly HttpClient _httpClient;
	private const string MameApiUrl = "https://api.github.com/repos/mamedev/mame/releases/latest";
	private const string MameDatUrl = "https://github.com/mamedev/mame/releases/latest/download/mame.xml";

	public DatProvider ProviderType => DatProvider.MAME;

	public MameProvider(HttpClient httpClient) {
		_httpClient = httpClient;
		_httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Subrom/1.0");
	}

	public async Task<IReadOnlyList<DatMetadata>> ListAvailableAsync(CancellationToken cancellationToken = default) {
		// MAME has a single comprehensive DAT
		return new List<DatMetadata> {
			new() {
				Identifier = "mame",
				Name = "MAME - Arcade",
				Description = "MAME arcade machine database",
				System = "Arcade",
				DownloadUrl = MameDatUrl,
				LastUpdated = DateTime.UtcNow // TODO: Get from GitHub API
			}
		};
	}

	public async Task<Stream> DownloadDatAsync(string identifier, CancellationToken cancellationToken = default) {
		if (!SupportsIdentifier(identifier)) {
			throw new ArgumentException($"Unsupported MAME identifier: {identifier}", nameof(identifier));
		}

		var response = await _httpClient.GetAsync(MameDatUrl, cancellationToken);
		response.EnsureSuccessStatusCode();

		// Return as MemoryStream to allow multiple reads
		var memoryStream = new MemoryStream();
		await response.Content.CopyToAsync(memoryStream, cancellationToken);
		memoryStream.Position = 0;

		return memoryStream;
	}

	public bool SupportsIdentifier(string identifier) =>
		identifier.Equals("mame", StringComparison.OrdinalIgnoreCase);
}
