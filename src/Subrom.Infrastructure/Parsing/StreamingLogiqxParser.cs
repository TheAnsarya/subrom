using System.Runtime.CompilerServices;
using System.Xml;
using Subrom.Application.Interfaces;
using Subrom.Domain.Aggregates.DatFiles;

namespace Subrom.Infrastructure.Parsing;

/// <summary>
/// Streaming XML parser for large Logiqx DAT files (60K+ games).
/// Uses XmlReader for forward-only parsing with minimal memory usage.
/// </summary>
public sealed class StreamingLogiqxParser : IDatParser {
	public DatFormat Format => DatFormat.LogiqxXml;

	// Internal DTOs for collecting game data before creating domain objects
	private sealed record GameData(
		string Name,
		string? Description,
		string? Year,
		string? Publisher,
		string? Category,
		string? CloneOf,
		string? RomOf,
		string? SampleOf,
		List<RomData> Roms);

	private sealed record RomData(
		string Name,
		long Size,
		string? Crc,
		string? Md5,
		string? Sha1,
		RomStatus Status,
		string? Serial,
		string? Merge);

	public bool CanParse(string filePath) {
		if (!File.Exists(filePath)) return false;

		var extension = Path.GetExtension(filePath).ToLowerInvariant();
		if (extension is not ".dat" and not ".xml") return false;

		// Quick check for Logiqx format
		try {
			using var reader = new StreamReader(filePath);
			for (var i = 0; i < 10 && !reader.EndOfStream; i++) {
				var line = reader.ReadLine();
				if (line?.Contains("<datafile") == true || line?.Contains("<!DOCTYPE datafile") == true) {
					return true;
				}
			}
		}
		catch {
			// Ignore parsing errors during detection
		}

		return false;
	}

	public async Task<DatFile> ParseAsync(
		string filePath,
		IProgress<DatParseProgress>? progress = null,
		CancellationToken cancellationToken = default) {
		ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

		await using var stream = new FileStream(
			filePath,
			FileMode.Open,
			FileAccess.Read,
			FileShare.Read,
			bufferSize: 64 * 1024,
			FileOptions.Asynchronous | FileOptions.SequentialScan);

		return await ParseAsync(stream, Path.GetFileName(filePath), progress, cancellationToken);
	}

	public async Task<DatFile> ParseAsync(
		Stream stream,
		string fileName,
		IProgress<DatParseProgress>? progress = null,
		CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(stream);
		ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

		progress?.Report(new DatParseProgress { Phase = "Initializing streaming parser..." });

		var settings = new XmlReaderSettings {
			DtdProcessing = DtdProcessing.Ignore,
			IgnoreWhitespace = true,
			IgnoreComments = true,
			Async = true,
		};

		using var reader = XmlReader.Create(stream, settings);

		// Temporary storage for header info
		string? name = null;
		string? description = null;
		string? version = null;
		string? author = null;
		string? homepage = null;
		string? system = null;
		DatFormat format = DatFormat.LogiqxXml;
		DatProvider provider = DatProvider.Unknown;

		var gameDataList = new List<GameData>();
		var gamesProcessed = 0;

		// Parse using streaming approach
		while (await reader.ReadAsync()) {
			cancellationToken.ThrowIfCancellationRequested();

			if (reader.NodeType != XmlNodeType.Element) continue;

			switch (reader.LocalName) {
				case "header":
					(name, description, version, author, homepage, system) = await ParseHeaderAsync(reader);
					break;

				case "game":
				case "machine":
					var gameData = await ParseGameDataAsync(reader);
					if (gameData is not null) {
						gameDataList.Add(gameData);
						gamesProcessed++;

						if (gamesProcessed % 500 == 0) {
							progress?.Report(new DatParseProgress {
								Phase = $"Parsing games ({gamesProcessed:N0})...",
								GamesParsed = gamesProcessed,
								TotalGames = null // Unknown in streaming mode
							});
						}
					}
					break;
			}
		}

		// Detect provider
		provider = DetectProvider(homepage, null, name, fileName);

		// Create the DatFile with all required properties
		var datFile = new DatFile {
			FileName = fileName,
			Name = name ?? Path.GetFileNameWithoutExtension(fileName),
			Description = description,
			Version = version,
			Author = author,
			Homepage = homepage,
			System = system,
			Format = format,
			Provider = provider
		};

		// Convert game data to domain objects with proper DatFileId
		foreach (var gameData in gameDataList) {
			var game = ConvertToGameEntry(gameData, datFile.Id);
			datFile.AddGame(game);
		}

		progress?.Report(new DatParseProgress {
			Phase = "Complete",
			GamesParsed = gamesProcessed,
			TotalGames = gamesProcessed
		});

		return datFile;
	}

	/// <summary>
	/// Streams games as they are parsed, ideal for very large DAT files.
	/// </summary>
	public async IAsyncEnumerable<GameEntry> StreamGamesAsync(
		string filePath,
		IProgress<DatParseProgress>? progress = null,
		[EnumeratorCancellation] CancellationToken cancellationToken = default) {
		ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

		await using var stream = new FileStream(
			filePath,
			FileMode.Open,
			FileAccess.Read,
			FileShare.Read,
			bufferSize: 64 * 1024,
			FileOptions.Asynchronous | FileOptions.SequentialScan);

		await foreach (var game in StreamGamesAsync(stream, progress, cancellationToken)) {
			yield return game;
		}
	}

	/// <summary>
	/// Streams games from a stream as they are parsed.
	/// </summary>
	public async IAsyncEnumerable<GameEntry> StreamGamesAsync(
		Stream stream,
		IProgress<DatParseProgress>? progress = null,
		[EnumeratorCancellation] CancellationToken cancellationToken = default) {
		var settings = new XmlReaderSettings {
			DtdProcessing = DtdProcessing.Ignore,
			IgnoreWhitespace = true,
			IgnoreComments = true,
			Async = true,
		};

		using var reader = XmlReader.Create(stream, settings);
		var gamesYielded = 0;

		while (await reader.ReadAsync()) {
			cancellationToken.ThrowIfCancellationRequested();

			if (reader.NodeType != XmlNodeType.Element) continue;

			if (reader.LocalName is "game" or "machine") {
				var gameData = await ParseGameDataAsync(reader);
				if (gameData is not null) {
					var game = ConvertToGameEntry(gameData);
					gamesYielded++;

					if (gamesYielded % 500 == 0) {
						progress?.Report(new DatParseProgress {
							Phase = $"Streaming games ({gamesYielded:N0})...",
							GamesParsed = gamesYielded,
							TotalGames = null
						});
					}

					yield return game;
				}
			}
		}
	}

	private static GameEntry ConvertToGameEntry(GameData gameData, Guid? datFileId = null) {
		var game = new GameEntry {
			Name = gameData.Name,
			Description = gameData.Description,
			Year = gameData.Year,
			Publisher = gameData.Publisher,
			Category = gameData.Category,
			CloneOf = gameData.CloneOf,
			RomOf = gameData.RomOf,
			SampleOf = gameData.SampleOf,
			DatFileId = datFileId ?? Guid.Empty
		};

		foreach (var romData in gameData.Roms) {
			var rom = new RomEntry {
				Name = romData.Name,
				Size = romData.Size,
				Crc = romData.Crc,
				Md5 = romData.Md5,
				Sha1 = romData.Sha1,
				Status = romData.Status,
				Serial = romData.Serial,
				Merge = romData.Merge,
				GameId = game.Id
			};
			game.AddRom(rom);
		}

		return game;
	}

	private static async Task<(string? Name, string? Description, string? Version, string? Author, string? Homepage, string? System)> ParseHeaderAsync(XmlReader reader) {
		string? name = null;
		string? description = null;
		string? version = null;
		string? author = null;
		string? homepage = null;
		string? system = null;

		using var headerReader = reader.ReadSubtree();

		while (await headerReader.ReadAsync()) {
			if (headerReader.NodeType != XmlNodeType.Element) continue;

			var elementName = headerReader.LocalName;
			var content = await headerReader.ReadElementContentAsStringAsync();

			switch (elementName) {
				case "name":
					name = content;
					break;
				case "description":
					description = NullIfEmpty(content);
					break;
				case "version":
					version = NullIfEmpty(content);
					break;
				case "author":
					author = NullIfEmpty(content);
					break;
				case "homepage":
					homepage = NullIfEmpty(content);
					break;
				case "category":
					system = NullIfEmpty(content);
					break;
			}
		}

		return (name, description, version, author, homepage, system);
	}

	private static async Task<GameData?> ParseGameDataAsync(XmlReader reader) {
		var name = reader.GetAttribute("name") ?? "";
		var cloneOf = NullIfEmpty(reader.GetAttribute("cloneof"));
		var romOf = NullIfEmpty(reader.GetAttribute("romof"));
		var sampleOf = NullIfEmpty(reader.GetAttribute("sampleof"));

		string? description = null;
		string? year = null;
		string? publisher = null;
		string? category = null;
		var roms = new List<RomData>();

		if (!reader.IsEmptyElement) {
			using var gameReader = reader.ReadSubtree();

			while (await gameReader.ReadAsync()) {
				if (gameReader.NodeType != XmlNodeType.Element) continue;

				switch (gameReader.LocalName) {
					case "description":
						description = NullIfEmpty(await gameReader.ReadElementContentAsStringAsync());
						break;
					case "year":
						year = NullIfEmpty(await gameReader.ReadElementContentAsStringAsync());
						break;
					case "manufacturer":
						publisher = NullIfEmpty(await gameReader.ReadElementContentAsStringAsync());
						break;
					case "category":
						category = NullIfEmpty(await gameReader.ReadElementContentAsStringAsync());
						break;
					case "rom":
						var romInfo = ParseRomData(gameReader);
						if (romInfo is not null) {
							roms.Add(romInfo);
						}
						break;
				}
			}
		}

		return new GameData(name, description, year, publisher, category, cloneOf, romOf, sampleOf, roms);
	}

	private static RomData? ParseRomData(XmlReader reader) {
		var name = reader.GetAttribute("name");
		if (string.IsNullOrWhiteSpace(name)) return null;

		var sizeStr = reader.GetAttribute("size");
		var size = long.TryParse(sizeStr, out var s) ? s : 0;

		var statusStr = reader.GetAttribute("status")?.ToLowerInvariant();
		var status = statusStr switch {
			"baddump" => RomStatus.BadDump,
			"nodump" => RomStatus.NoDump,
			"verified" => RomStatus.Verified,
			_ => RomStatus.Good
		};

		return new RomData(
			name,
			size,
			NormalizeHash(reader.GetAttribute("crc")),
			NormalizeHash(reader.GetAttribute("md5")),
			NormalizeHash(reader.GetAttribute("sha1")),
			status,
			NullIfEmpty(reader.GetAttribute("serial")),
			NullIfEmpty(reader.GetAttribute("merge"))
		);
	}

	private static string? NormalizeHash(string? hash) =>
		string.IsNullOrWhiteSpace(hash) ? null : hash.ToLowerInvariant();

	private static string? NullIfEmpty(string? value) =>
		string.IsNullOrWhiteSpace(value) ? null : value;

	private static DatProvider DetectProvider(string? homepage, string? url, string? name, string fileName) {
		var combined = $"{homepage} {url} {name} {fileName}".ToLowerInvariant();

		if (combined.Contains("no-intro") || combined.Contains("nointro")) return DatProvider.NoIntro;
		if (combined.Contains("redump")) return DatProvider.Redump;
		if (combined.Contains("tosec")) return DatProvider.TOSEC;
		if (combined.Contains("goodtools") || combined.Contains("goodset")) return DatProvider.GoodSets;
		if (combined.Contains("mame")) return DatProvider.MAME;

		return DatProvider.Unknown;
	}
}
