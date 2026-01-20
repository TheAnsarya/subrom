using System.Text;
using System.Text.RegularExpressions;
using Subrom.Domain.Datfiles;
using Subrom.Domain.Datfiles.Kinds;
using Subrom.Domain.Hash;

namespace Subrom.Infrastructure.Parsers;

/// <summary>
/// Parser for ClrMame Pro plain-text DAT format.
/// </summary>
public partial class ClrMameProDatParser : IDatParser {
	public string FormatName => "ClrMame Pro";

	public bool CanParse(Stream stream) {
		try {
			using var reader = new StreamReader(stream, leaveOpen: true);
			var buffer = new char[200];
			var read = reader.Read(buffer, 0, 200);
			stream.Position = 0;

			var content = new string(buffer, 0, read);
			return content.Contains("clrmamepro (") || content.Contains("game (");
		} catch {
			return false;
		}
	}

	public async Task<Datafile> ParseAsync(Stream stream, CancellationToken cancellationToken = default) {
		var datafile = new Datafile();

		using var reader = new StreamReader(stream, Encoding.UTF8);
		var content = await reader.ReadToEndAsync(cancellationToken);

		// Parse header block
		var headerMatch = HeaderBlockRegex().Match(content);
		if (headerMatch.Success) {
			datafile.Header = ParseHeader(headerMatch.Groups[1].Value);
		}

		// Parse game blocks
		var gameMatches = GameBlockRegex().Matches(content);
		foreach (Match match in gameMatches) {
			cancellationToken.ThrowIfCancellationRequested();
			var game = ParseGame(match.Groups[1].Value);
			datafile.Games.Add(game);
		}

		return datafile;
	}

	private static Header ParseHeader(string block) {
		var header = new Header();

		var lines = ParseBlockLines(block);
		foreach (var (key, value) in lines) {
			switch (key.ToLowerInvariant()) {
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
					header.Homepage = value;
					break;
				case "url":
					header.Url = value;
					break;
				case "category":
					header.Category = value;
					break;
				case "date":
					header.Date = value;
					break;
				case "email":
					header.Email = value;
					break;
				case "comment":
					header.Comment = value;
					break;
			}
		}

		return header;
	}

	private static Game ParseGame(string block) {
		var game = new Game();

		var lines = ParseBlockLines(block);
		foreach (var (key, value) in lines) {
			switch (key.ToLowerInvariant()) {
				case "name":
					game.Name = value;
					break;
				case "description":
					game.Description = value;
					break;
				case "year":
					game.Year = Year.From(value);
					break;
				case "manufacturer":
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

		// Parse ROM entries
		var romMatches = RomBlockRegex().Matches(block);
		foreach (Match match in romMatches) {
			var rom = ParseRom(match.Groups[1].Value);
			game.Roms.Add(rom);
		}

		return game;
	}

	private static Rom ParseRom(string block) {
		var rom = new Rom();

		var parts = ParseInlineBlock(block);
		foreach (var (key, value) in parts) {
			switch (key.ToLowerInvariant()) {
				case "name":
					rom.Name = value;
					break;
				case "size":
					if (long.TryParse(value, out var size)) {
						rom.Size = size;
					}
					break;
				case "crc":
					rom.Crc = Crc.From(value.ToLowerInvariant());
					break;
				case "md5":
					rom.Md5 = Md5.From(value.ToLowerInvariant());
					break;
				case "sha1":
					rom.Sha1 = Sha1.From(value.ToLowerInvariant());
					break;
				case "status":
					rom.Status = StatusKind.From(value);
					break;
				case "merge":
					rom.Merge = value;
					break;
				case "date":
					rom.Date = value;
					break;
			}
		}

		return rom;
	}

	private static List<(string Key, string Value)> ParseBlockLines(string block) {
		var result = new List<(string, string)>();
		var matches = KeyValueRegex().Matches(block);

		foreach (Match match in matches) {
			var key = match.Groups[1].Value;
			var value = match.Groups[2].Success
				? match.Groups[2].Value
				: match.Groups[3].Value;
			result.Add((key, value));
		}

		return result;
	}

	private static List<(string Key, string Value)> ParseInlineBlock(string block) {
		var result = new List<(string, string)>();
		var matches = InlineKeyValueRegex().Matches(block);

		foreach (Match match in matches) {
			var key = match.Groups[1].Value;
			var value = match.Groups[2].Success
				? match.Groups[2].Value
				: match.Groups[3].Value;
			result.Add((key, value));
		}

		return result;
	}

	[GeneratedRegex(@"clrmamepro\s*\(\s*(.*?)\s*\)", RegexOptions.Singleline)]
	private static partial Regex HeaderBlockRegex();

	[GeneratedRegex(@"game\s*\(\s*(.*?)\s*\)\s*(?=game\s*\(|$)", RegexOptions.Singleline)]
	private static partial Regex GameBlockRegex();

	[GeneratedRegex(@"rom\s*\(\s*(.*?)\s*\)", RegexOptions.Singleline)]
	private static partial Regex RomBlockRegex();

	[GeneratedRegex(@"(\w+)\s+(?:""([^""]*)""|(\S+))")]
	private static partial Regex KeyValueRegex();

	[GeneratedRegex(@"(\w+)\s+(?:""([^""]*)""|(\S+))")]
	private static partial Regex InlineKeyValueRegex();
}
