using System.Diagnostics;
using System.IO.Compression;
using Subrom.Application.Interfaces;
using Subrom.Domain.Aggregates.DatFiles;

namespace Subrom.Infrastructure.Providers;

/// <summary>
/// DAT provider for TOSEC (The Old School Emulation Center).
/// Downloads the latest TOSEC DAT pack via PowerShell script.
/// </summary>
public sealed class TosecProvider : IDatProvider {
	private readonly string _scriptPath;
	private readonly string _cacheDirectory;
	private string? _cachedPackPath;
	private List<DatMetadata>? _cachedMetadata;

	public TosecProvider(string? scriptPath = null, string? cacheDirectory = null) {
		_scriptPath = scriptPath ?? Path.Combine(
			Directory.GetCurrentDirectory(),
			"..", "..", "..", "..", "..",
			"scripts", "download-tosec-dats.ps1");

		_cacheDirectory = cacheDirectory ?? Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			"Subrom", "Cache", "TOSEC");

		Directory.CreateDirectory(_cacheDirectory);
	}

	public DatProvider ProviderType => DatProvider.TOSEC;

	public async Task<IReadOnlyList<DatMetadata>> ListAvailableAsync(CancellationToken cancellationToken = default) {
		if (_cachedMetadata is not null) {
			return _cachedMetadata;
		}

		// Ensure TOSEC pack is downloaded
		await EnsureTosecPackDownloadedAsync(cancellationToken);

		// Extract metadata from zip without extracting all files
		var metadata = new List<DatMetadata>();

		if (_cachedPackPath is null || !File.Exists(_cachedPackPath)) {
			return metadata;
		}

		using var archive = ZipFile.OpenRead(_cachedPackPath);

		foreach (var entry in archive.Entries) {
			if (!entry.FullName.EndsWith(".dat", StringComparison.OrdinalIgnoreCase)) {
				continue;
			}

			// Extract system name from filename
			// Example: "Nintendo - Game Boy Advance - Games (TOSEC-v2025-01-15_CM).dat"
			var fileName = Path.GetFileNameWithoutExtension(entry.Name);
			var parts = fileName.Split(" - ");

			var system = parts.Length > 0 ? parts[0].Trim() : "Unknown";
			var category = parts.Length > 1 ? parts[1].Trim() : "Games";

			metadata.Add(new DatMetadata {
				Identifier = entry.FullName,
				Name = fileName,
				System = system,
				Description = $"TOSEC {system} - {category}",
				DownloadUrl = null // Embedded in pack
			});
		}

		_cachedMetadata = metadata;
		return metadata;
	}

	public async Task<Stream> DownloadDatAsync(string identifier, CancellationToken cancellationToken = default) {
		// Ensure pack is downloaded
		await EnsureTosecPackDownloadedAsync(cancellationToken);

		if (_cachedPackPath is null || !File.Exists(_cachedPackPath)) {
			throw new FileNotFoundException("TOSEC pack not found. Download failed.");
		}

		// Extract specific DAT from the pack
		using var archive = ZipFile.OpenRead(_cachedPackPath);
		var entry = archive.GetEntry(identifier);

		if (entry is null) {
			throw new FileNotFoundException($"DAT file '{identifier}' not found in TOSEC pack.");
		}

		// Read into memory stream
		var memoryStream = new MemoryStream();
		await using (var entryStream = entry.Open()) {
			await entryStream.CopyToAsync(memoryStream, cancellationToken);
		}

		memoryStream.Position = 0;
		return memoryStream;
	}

	public bool SupportsIdentifier(string identifier) =>
		identifier.EndsWith(".dat", StringComparison.OrdinalIgnoreCase) &&
		identifier.Contains("TOSEC", StringComparison.OrdinalIgnoreCase);

	private async Task EnsureTosecPackDownloadedAsync(CancellationToken cancellationToken) {
		// Check if pack already exists in cache
		var existingPacks = Directory.GetFiles(_cacheDirectory, "TOSEC*.zip");

		if (existingPacks.Length > 0) {
			_cachedPackPath = existingPacks[0];
			return;
		}

		// Run PowerShell script to download
		if (!File.Exists(_scriptPath)) {
			throw new FileNotFoundException($"TOSEC download script not found: {_scriptPath}");
		}

		var startInfo = new ProcessStartInfo {
			FileName = "pwsh",
			Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{_scriptPath}\" -OutputPath \"{_cacheDirectory}\"",
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true
		};

		using var process = new Process { StartInfo = startInfo };
		process.Start();

		await process.WaitForExitAsync(cancellationToken);

		if (process.ExitCode != 0) {
			var error = await process.StandardError.ReadToEndAsync(cancellationToken);
			throw new InvalidOperationException($"TOSEC download script failed: {error}");
		}

		// Find the downloaded pack
		existingPacks = Directory.GetFiles(_cacheDirectory, "TOSEC*.zip");

		if (existingPacks.Length == 0) {
			throw new FileNotFoundException("TOSEC pack not found after download.");
		}

		_cachedPackPath = existingPacks[0];
	}
}
