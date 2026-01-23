using System.Xml;
using System.Xml.Serialization;
using Subrom.Application.Interfaces;
using Subrom.Domain.Aggregates.DatFiles;

namespace Subrom.Infrastructure.Parsing;

/// <summary>
/// Parser for Logiqx XML DAT format (used by No-Intro, Redump, TOSEC).
/// </summary>
public sealed class LogiqxDatParser : IDatParser {
	public DatFormat Format => DatFormat.LogiqxXml;

	public bool CanParse(string filePath) {
		if (!File.Exists(filePath)) return false;

		var extension = Path.GetExtension(filePath).ToLowerInvariant();
		if (extension is not ".dat" and not ".xml") return false;

		// Quick check for Logiqx format by reading first few lines
		try {
			using var reader = new StreamReader(filePath);
			for (var i = 0; i < 10 && !reader.EndOfStream; i++) {
				var line = reader.ReadLine();
				if (line?.Contains("<datafile") == true || line?.Contains("<!DOCTYPE datafile") == true) {
					return true;
				}
			}
		} catch {
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

		progress?.Report(new DatParseProgress { Phase = "Parsing XML..." });

		// Use XmlReader settings that ignore DTD processing
		var settings = new XmlReaderSettings {
			DtdProcessing = DtdProcessing.Ignore,
			IgnoreWhitespace = true,
			IgnoreComments = true,
			Async = true,
		};

		using var reader = XmlReader.Create(stream, settings);
		var serializer = new XmlSerializer(typeof(DatafileXml));

		// Deserialize on a background thread to not block
		var xmlData = await Task.Run(() => {
			cancellationToken.ThrowIfCancellationRequested();
			return (DatafileXml?)serializer.Deserialize(reader);
		}, cancellationToken);

		if (xmlData is null) {
			throw new InvalidDataException($"Failed to parse DAT file: {fileName}");
		}

		var totalGames = xmlData.Games?.Length ?? 0;
		progress?.Report(new DatParseProgress { Phase = "Converting to domain model...", TotalGames = totalGames });

		// Convert to domain model
		var datFile = ConvertToDomain(xmlData, fileName, progress);

		progress?.Report(new DatParseProgress { Phase = "Complete", GamesParsed = datFile.GameCount, TotalGames = datFile.GameCount });

		return datFile;
	}

	private static DatFile ConvertToDomain(DatafileXml xml, string fileName, IProgress<DatParseProgress>? progress) {
		// Detect provider from header or filename
		var provider = DetectProvider(xml.Header?.Homepage, xml.Header?.Url, xml.Header?.Name, fileName);

		var datFile = new DatFile {
			FileName = fileName,
			Name = xml.Header?.Name ?? Path.GetFileNameWithoutExtension(fileName),
			Description = NullIfEmpty(xml.Header?.Description),
			Version = NullIfEmpty(xml.Header?.Version),
			Author = NullIfEmpty(xml.Header?.Author),
			Homepage = NullIfEmpty(xml.Header?.Homepage),
			System = NullIfEmpty(xml.Header?.Category),
			Format = DatFormat.LogiqxXml,
			Provider = provider,
		};

		var totalGames = xml.Games?.Length ?? 0;
		var processedGames = 0;

		foreach (var gameXml in xml.Games ?? []) {
			var game = new GameEntry {
				Name = gameXml.Name ?? "",
				Description = NullIfEmpty(gameXml.Description),
				Year = NullIfEmpty(gameXml.Year),
				Publisher = NullIfEmpty(gameXml.Manufacturer),
				CloneOf = NullIfEmpty(gameXml.CloneOf),
				RomOf = NullIfEmpty(gameXml.RomOf),
				SampleOf = NullIfEmpty(gameXml.SampleOf),
				Category = NullIfEmpty(gameXml.Category),
				DatFileId = datFile.Id,
			};

			foreach (var romXml in gameXml.Roms ?? []) {
				var status = romXml.Status?.ToLowerInvariant() switch {
					"baddump" => RomStatus.BadDump,
					"nodump" => RomStatus.NoDump,
					"verified" => RomStatus.Verified,
					_ => RomStatus.Good,
				};

				var rom = new RomEntry {
					Name = romXml.Name ?? "",
					Size = romXml.Size,
					Crc = NormalizeHash(romXml.Crc),
					Md5 = NormalizeHash(romXml.Md5),
					Sha1 = NormalizeHash(romXml.Sha1),
					Status = status,
					Serial = NullIfEmpty(romXml.Serial),
					Merge = NullIfEmpty(romXml.Merge),
					GameId = game.Id,
				};

				game.AddRom(rom);
			}

			datFile.AddGame(game);

			processedGames++;
			if (progress is not null && processedGames % 100 == 0) {
				progress.Report(new DatParseProgress {
					Phase = $"Processing game {processedGames}/{totalGames}",
					GamesParsed = processedGames,
					TotalGames = totalGames
				});
			}
		}

		return datFile;
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

// XML DTOs for deserialization
[XmlRoot("datafile")]
public class DatafileXml {
	[XmlAttribute("build")]
	public string? Build { get; set; }

	[XmlElement("header")]
	public HeaderXml? Header { get; set; }

	[XmlElement("game")]
	public GameXml[]? Games { get; set; }

	[XmlElement("machine")]
	public GameXml[]? Machines { get; set; }
}

public class HeaderXml {
	[XmlElement("name")]
	public string? Name { get; set; }

	[XmlElement("description")]
	public string? Description { get; set; }

	[XmlElement("category")]
	public string? Category { get; set; }

	[XmlElement("version")]
	public string? Version { get; set; }

	[XmlElement("date")]
	public string? Date { get; set; }

	[XmlElement("author")]
	public string? Author { get; set; }

	[XmlElement("email")]
	public string? Email { get; set; }

	[XmlElement("homepage")]
	public string? Homepage { get; set; }

	[XmlElement("url")]
	public string? Url { get; set; }

	[XmlElement("comment")]
	public string? Comment { get; set; }
}

public class GameXml {
	[XmlAttribute("name")]
	public string? Name { get; set; }

	[XmlAttribute("cloneof")]
	public string? CloneOf { get; set; }

	[XmlAttribute("romof")]
	public string? RomOf { get; set; }

	[XmlAttribute("sampleof")]
	public string? SampleOf { get; set; }

	[XmlElement("description")]
	public string? Description { get; set; }

	[XmlElement("year")]
	public string? Year { get; set; }

	[XmlElement("manufacturer")]
	public string? Manufacturer { get; set; }

	[XmlElement("category")]
	public string? Category { get; set; }

	[XmlElement("rom")]
	public RomXml[]? Roms { get; set; }
}

public class RomXml {
	[XmlAttribute("name")]
	public string? Name { get; set; }

	[XmlAttribute("size")]
	public long Size { get; set; }

	[XmlAttribute("crc")]
	public string? Crc { get; set; }

	[XmlAttribute("md5")]
	public string? Md5 { get; set; }

	[XmlAttribute("sha1")]
	public string? Sha1 { get; set; }

	[XmlAttribute("serial")]
	public string? Serial { get; set; }

	[XmlAttribute("merge")]
	public string? Merge { get; set; }

	[XmlAttribute("status")]
	public string? Status { get; set; }
}
