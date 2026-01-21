using System.Text.RegularExpressions;
using Subrom.Application.Interfaces;
using Subrom.Domain.Aggregates.DatFiles;

namespace Subrom.Infrastructure.Parsing;

/// <summary>
/// Parser for ClrMamePro text DAT format.
/// This format is used by many TOSEC DATs and some legacy collections.
/// </summary>
public sealed partial class ClrMameProDatParser : IDatParser {
	public DatFormat Format => DatFormat.ClrMamePro;

	public bool CanParse(string filePath) {
		if (!File.Exists(filePath)) return false;

		var extension = Path.GetExtension(filePath).ToLowerInvariant();
		if (extension is not ".dat") return false;

		// Check for ClrMamePro format markers
		try {
			using var reader = new StreamReader(filePath);
			for (var i = 0; i < 20 && !reader.EndOfStream; i++) {
				var line = reader.ReadLine()?.Trim();
				if (string.IsNullOrEmpty(line)) continue;

				// ClrMamePro files start with "clrmamepro (" header block
				if (line.StartsWith("clrmamepro", StringComparison.OrdinalIgnoreCase)) {
					return true;
				}

				// Some files use "emulator (" instead
				if (line.StartsWith("emulator", StringComparison.OrdinalIgnoreCase)) {
					return true;
				}

				// Skip comments and check for "game (" which indicates CMP format
				if (line.StartsWith("game (", StringComparison.OrdinalIgnoreCase)) {
					return true;
				}

				// If we hit XML markers, it's not ClrMamePro
				if (line.StartsWith("<?xml") || line.StartsWith("<datafile") || line.Contains("<!DOCTYPE")) {
					return false;
				}
			}
		} catch {
			// Ignore detection errors
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

		progress?.Report(new DatParseProgress { Phase = "Reading ClrMamePro file..." });

		using var reader = new StreamReader(stream);
		var content = await reader.ReadToEndAsync(cancellationToken);

		// Parse on background thread for large files
		var result = await Task.Run(() => ParseContent(content, fileName, progress, cancellationToken), cancellationToken);

		progress?.Report(new DatParseProgress {
			Phase = "Complete",
			GamesParsed = result.GameCount,
			TotalGames = result.GameCount
		});

		return result;
	}

	private static DatFile ParseContent(
		string content,
		string fileName,
		IProgress<DatParseProgress>? progress,
		CancellationToken ct) {
		var lines = content.Split('\n');
		var header = new HeaderInfo();
		var games = new List<GameEntry>();

		var currentBlock = BlockType.None;
		var currentGame = default(GameInfo?);
		var blockContent = new List<string>();
		var depth = 0;

		for (var i = 0; i < lines.Length; i++) {
			ct.ThrowIfCancellationRequested();

			var line = lines[i].Trim();
			if (string.IsNullOrEmpty(line)) continue;

			// Count parentheses for depth tracking
			var openCount = line.Count(c => c == '(');
			var closeCount = line.Count(c => c == ')');

			// Detect block start
			if (depth == 0) {
				if (line.StartsWith("clrmamepro", StringComparison.OrdinalIgnoreCase) ||
					line.StartsWith("emulator", StringComparison.OrdinalIgnoreCase)) {
					currentBlock = BlockType.Header;
					depth += openCount - closeCount;
					blockContent.Clear();
					continue;
				}

				if (line.StartsWith("game", StringComparison.OrdinalIgnoreCase) ||
					line.StartsWith("machine", StringComparison.OrdinalIgnoreCase) ||
					line.StartsWith("resource", StringComparison.OrdinalIgnoreCase)) {
					currentBlock = BlockType.Game;
					depth += openCount - closeCount;
					blockContent.Clear();
					currentGame = new GameInfo();
					continue;
				}
			}

			depth += openCount - closeCount;

			// Collect block content
			if (currentBlock != BlockType.None) {
				blockContent.Add(line);
			}

			// Block complete when depth returns to 0
			if (depth <= 0 && currentBlock != BlockType.None) {
				if (currentBlock == BlockType.Header) {
					ParseHeader(blockContent, header);
				} else if (currentBlock == BlockType.Game && currentGame != null) {
					ParseGame(blockContent, currentGame);
					if (!string.IsNullOrEmpty(currentGame.Name)) {
						games.Add(ConvertToGameEntry(currentGame));

						if (games.Count % 1000 == 0) {
							progress?.Report(new DatParseProgress {
								Phase = "Parsing games...",
								GamesParsed = games.Count
							});
						}
					}
				}

				currentBlock = BlockType.None;
				currentGame = null;
				blockContent.Clear();
				depth = 0;
			}
		}

		// Detect provider
		var provider = DetectProvider(header.Name, header.Homepage, fileName);

		var datFile = new DatFile {
			FileName = fileName,
			Name = header.Name ?? Path.GetFileNameWithoutExtension(fileName),
			Description = header.Description,
			Version = header.Version,
			Author = header.Author,
			Homepage = header.Homepage,
			Format = DatFormat.ClrMamePro,
			Provider = provider,
			System = ExtractSystem(header.Name),
			CategoryPath = BuildCategoryPath(provider, header.Name)
		};

		// Add all games
		foreach (var game in games) {
			datFile.AddGame(game);
		}

		return datFile;
	}

	private static void ParseHeader(List<string> lines, HeaderInfo header) {
		foreach (var line in lines) {
			var (key, value) = ParseKeyValue(line);
			switch (key?.ToLowerInvariant()) {
				case "name":
					header.Name = value;
					break;
				case "description":
					header.Description = value;
					break;
				case "version":
					header.Version = value;
					break;
				case "author":
					header.Author = value;
					break;
				case "homepage":
				case "url":
					header.Homepage = value;
					break;
			}
		}
	}

	private static void ParseGame(List<string> lines, GameInfo game) {
		var currentRom = default(RomInfo?);

		foreach (var line in lines) {
			// Check for rom block start
			if (line.StartsWith("rom", StringComparison.OrdinalIgnoreCase) && line.Contains('(')) {
				currentRom = new RomInfo();

				// Try to parse inline rom (all on one line)
				if (line.Contains(')')) {
					ParseRomInline(line, currentRom);
					if (!string.IsNullOrEmpty(currentRom.Name)) {
						game.Roms.Add(currentRom);
					}
					currentRom = null;
				}
				continue;
			}

			// Check for end of rom block
			if (currentRom != null && line.StartsWith(")")) {
				if (!string.IsNullOrEmpty(currentRom.Name)) {
					game.Roms.Add(currentRom);
				}
				currentRom = null;
				continue;
			}

			// Parse rom properties
			if (currentRom != null) {
				ParseRomProperty(line, currentRom);
				continue;
			}

			// Parse game properties
			var (key, value) = ParseKeyValue(line);
			switch (key?.ToLowerInvariant()) {
				case "name":
					game.Name = value;
					break;
				case "description":
					game.Description = value;
					break;
				case "year":
					game.Year = value;
					break;
				case "manufacturer":
				case "developer":
				case "publisher":
					game.Manufacturer = value;
					break;
				case "cloneof":
					game.CloneOf = value;
					break;
				case "romof":
					game.RomOf = value;
					break;
			}
		}
	}

	private static void ParseRomInline(string line, RomInfo rom) {
		// Parse: rom ( name "filename.ext" size 12345 crc 12345678 md5 ... sha1 ... )
		var content = ExtractParenContent(line);
		if (string.IsNullOrEmpty(content)) return;

		var tokens = TokenizeLine(content);
		for (var i = 0; i < tokens.Count - 1; i++) {
			var key = tokens[i].ToLowerInvariant();
			var value = tokens[i + 1];

			switch (key) {
				case "name":
					rom.Name = value;
					i++;
					break;
				case "size":
					if (long.TryParse(value, out var size)) rom.Size = size;
					i++;
					break;
				case "crc":
					rom.Crc = NormalizeHash(value);
					i++;
					break;
				case "md5":
					rom.Md5 = NormalizeHash(value);
					i++;
					break;
				case "sha1":
					rom.Sha1 = NormalizeHash(value);
					i++;
					break;
			}
		}
	}

	private static void ParseRomProperty(string line, RomInfo rom) {
		var (key, value) = ParseKeyValue(line);
		switch (key?.ToLowerInvariant()) {
			case "name":
				rom.Name = value;
				break;
			case "size":
				if (long.TryParse(value, out var size)) rom.Size = size;
				break;
			case "crc":
				rom.Crc = NormalizeHash(value);
				break;
			case "md5":
				rom.Md5 = NormalizeHash(value);
				break;
			case "sha1":
				rom.Sha1 = NormalizeHash(value);
				break;
		}
	}

	private static (string? Key, string? Value) ParseKeyValue(string line) {
		// Parse: key "value" or key value
		var tokens = TokenizeLine(line);
		if (tokens.Count >= 2) {
			return (tokens[0], tokens[1]);
		}
		if (tokens.Count == 1) {
			return (tokens[0], null);
		}
		return (null, null);
	}

	private static List<string> TokenizeLine(string line) {
		var tokens = new List<string>();
		var inQuote = false;
		var current = new System.Text.StringBuilder();

		foreach (var c in line) {
			if (c == '"') {
				inQuote = !inQuote;
				continue;
			}

			if (!inQuote && char.IsWhiteSpace(c)) {
				if (current.Length > 0) {
					tokens.Add(current.ToString());
					current.Clear();
				}
				continue;
			}

			// Skip parentheses when not in quotes
			if (!inQuote && (c == '(' || c == ')')) {
				if (current.Length > 0) {
					tokens.Add(current.ToString());
					current.Clear();
				}
				continue;
			}

			current.Append(c);
		}

		if (current.Length > 0) {
			tokens.Add(current.ToString());
		}

		return tokens;
	}

	private static string? ExtractParenContent(string line) {
		var start = line.IndexOf('(');
		var end = line.LastIndexOf(')');
		if (start >= 0 && end > start) {
			return line[(start + 1)..end].Trim();
		}
		return null;
	}

	private static string? NormalizeHash(string? hash) {
		if (string.IsNullOrEmpty(hash)) return null;
		// Remove any dashes and convert to lowercase
		return hash.Replace("-", "").ToLowerInvariant();
	}

	private static GameEntry ConvertToGameEntry(GameInfo info) {
		var game = new GameEntry {
			Name = info.Name ?? "Unknown",
			Description = info.Description ?? info.Name,
			Year = info.Year,
			Publisher = info.Manufacturer,
			CloneOf = info.CloneOf,
			RomOf = info.RomOf,
			Region = ExtractRegion(info.Name ?? "")
		};

		foreach (var romInfo in info.Roms) {
			var rom = new RomEntry {
				Name = romInfo.Name ?? "unknown.rom",
				Size = romInfo.Size,
				Crc = romInfo.Crc,
				Md5 = romInfo.Md5,
				Sha1 = romInfo.Sha1
			};
			game.AddRom(rom);
		}

		return game;
	}

	private static DatProvider DetectProvider(string? name, string? homepage, string fileName) {
		var lower = $"{name} {homepage} {fileName}".ToLowerInvariant();

		if (lower.Contains("no-intro") || lower.Contains("nointro")) return DatProvider.NoIntro;
		if (lower.Contains("tosec")) return DatProvider.TOSEC;
		if (lower.Contains("redump")) return DatProvider.Redump;
		if (lower.Contains("mame")) return DatProvider.MAME;
		if (lower.Contains("goodset") || lower.Contains("goodgen") || lower.Contains("goodnes")) return DatProvider.GoodSets;

		return DatProvider.Unknown;
	}

	private static string? ExtractSystem(string? datName) {
		if (string.IsNullOrEmpty(datName)) return null;

		// Common patterns: "Nintendo - Game Boy" or "TOSEC - Nintendo Game Boy"
		// Try to extract the system name
		var parts = datName.Split(" - ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

		if (parts.Length >= 2) {
			// Skip provider name if present
			return parts[^1];
		}

		return datName;
	}

	private static string BuildCategoryPath(DatProvider provider, string? name) {
		var providerName = provider switch {
			DatProvider.NoIntro => "No-Intro",
			DatProvider.TOSEC => "TOSEC",
			DatProvider.Redump => "Redump",
			DatProvider.MAME => "MAME",
			DatProvider.GoodSets => "GoodSets",
			_ => "Other"
		};

		var system = ExtractSystem(name) ?? "Unknown";
		return $"{providerName}/{system}";
	}

	[GeneratedRegex(@"\(([A-Z]{2,3}(?:,[A-Z]{2,3})*)\)|(\(USA\)|\(Europe\)|\(Japan\)|\(World\)|\(Asia\))", RegexOptions.Compiled)]
	private static partial Regex RegionRegex();

	private static string? ExtractRegion(string name) {
		var match = RegionRegex().Match(name);
		if (match.Success) {
			return match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value.Trim('(', ')');
		}
		return null;
	}

	private enum BlockType { None, Header, Game }

	private sealed class HeaderInfo {
		public string? Name;
		public string? Description;
		public string? Version;
		public string? Author;
		public string? Homepage;
	}

	private sealed class GameInfo {
		public string? Name;
		public string? Description;
		public string? Year;
		public string? Manufacturer;
		public string? CloneOf;
		public string? RomOf;
		public List<RomInfo> Roms { get; } = [];
	}

	private sealed class RomInfo {
		public string? Name;
		public long Size;
		public string? Crc;
		public string? Md5;
		public string? Sha1;
	}
}
