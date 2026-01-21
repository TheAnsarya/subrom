using System.Text;
using Subrom.Application.Interfaces;
using Subrom.Infrastructure.Parsing;
using Subrom.Domain.Aggregates.DatFiles;

namespace Subrom.Tests.Unit.Infrastructure;

/// <summary>
/// Unit tests for ClrMameProDatParser.
/// </summary>
public class ClrMameProDatParserTests {
	private readonly ClrMameProDatParser _parser = new();

	[Fact]
	public void Format_ReturnsClrMamePro() {
		Assert.Equal(DatFormat.ClrMamePro, _parser.Format);
	}

	[Theory]
	[InlineData("clrmamepro (")]
	[InlineData("emulator (")]
	[InlineData("game (")]
	public async Task CanParse_WithValidClrMameProContent_ReturnsTrue(string startLine) {
		// Arrange
		var content = $"""
			{startLine}
				name "Test DAT"
			)
			""";
		var tempFile = Path.GetTempFileName() + ".dat";
		await File.WriteAllTextAsync(tempFile, content);

		try {
			// Act
			var result = _parser.CanParse(tempFile);

			// Assert
			Assert.True(result);
		} finally {
			File.Delete(tempFile);
		}
	}

	[Fact]
	public async Task CanParse_WithXmlContent_ReturnsFalse() {
		// Arrange
		var content = """
			<?xml version="1.0"?>
			<datafile>
			</datafile>
			""";
		var tempFile = Path.GetTempFileName() + ".dat";
		await File.WriteAllTextAsync(tempFile, content);

		try {
			// Act
			var result = _parser.CanParse(tempFile);

			// Assert
			Assert.False(result);
		} finally {
			File.Delete(tempFile);
		}
	}

	[Fact]
	public async Task ParseAsync_WithBasicDat_ParsesHeaderCorrectly() {
		// Arrange
		var content = """
			clrmamepro (
				name "Test DAT"
				description "Test Description"
				version "1.0"
				author "Test Author"
				homepage "http://example.com"
			)
			""";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

		// Act
		var result = await _parser.ParseAsync(stream, "test.dat");

		// Assert
		Assert.Equal("Test DAT", result.Name);
		Assert.Equal("Test Description", result.Description);
		Assert.Equal("1.0", result.Version);
		Assert.Equal("Test Author", result.Author);
	}

	[Fact]
	public async Task ParseAsync_WithSingleGame_ParsesGameCorrectly() {
		// Arrange
		var content = """
			clrmamepro (
				name "Test DAT"
			)

			game (
				name "Test Game (USA)"
				description "Test Game Description"
				year "1990"
				manufacturer "Test Company"
				rom ( name "test.rom" size 12345 crc abcd1234 md5 12345678901234567890123456789012 sha1 1234567890123456789012345678901234567890 )
			)
			""";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

		// Act
		var result = await _parser.ParseAsync(stream, "test.dat");

		// Assert
		Assert.Single(result.Games);
		var game = result.Games[0];
		Assert.Equal("Test Game (USA)", game.Name);
		Assert.Equal("Test Game Description", game.Description);
		Assert.Equal("1990", game.Year);
		Assert.Equal("Test Company", game.Publisher);
		Assert.Equal("USA", game.Region);
	}

	[Fact]
	public async Task ParseAsync_WithSingleGame_ParsesRomCorrectly() {
		// Arrange
		var content = """
			clrmamepro (
				name "Test DAT"
			)

			game (
				name "Test Game"
				rom ( name "test.rom" size 12345 crc abcd1234 md5 12345678901234567890123456789012 sha1 1234567890123456789012345678901234567890 )
			)
			""";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

		// Act
		var result = await _parser.ParseAsync(stream, "test.dat");

		// Assert
		Assert.Single(result.Games);
		Assert.Single(result.Games[0].Roms);
		var rom = result.Games[0].Roms[0];
		Assert.Equal("test.rom", rom.Name);
		Assert.Equal(12345L, rom.Size);
		Assert.Equal("abcd1234", rom.Crc);
		Assert.Equal("12345678901234567890123456789012", rom.Md5);
		Assert.Equal("1234567890123456789012345678901234567890", rom.Sha1);
	}

	[Fact]
	public async Task ParseAsync_WithMultipleGames_ParsesAllGames() {
		// Arrange
		var content = """
			clrmamepro (
				name "Test DAT"
			)

			game (
				name "Game 1"
				rom ( name "game1.rom" size 100 crc 11111111 )
			)

			game (
				name "Game 2"
				rom ( name "game2.rom" size 200 crc 22222222 )
			)

			game (
				name "Game 3"
				rom ( name "game3.rom" size 300 crc 33333333 )
			)
			""";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

		// Act
		var result = await _parser.ParseAsync(stream, "test.dat");

		// Assert
		Assert.Equal(3, result.Games.Count);
		Assert.Equal("Game 1", result.Games[0].Name);
		Assert.Equal("Game 2", result.Games[1].Name);
		Assert.Equal("Game 3", result.Games[2].Name);
	}

	[Fact]
	public async Task ParseAsync_WithMultipleRoms_ParsesAllRoms() {
		// Arrange
		var content = """
			clrmamepro (
				name "Test DAT"
			)

			game (
				name "Multi-ROM Game"
				rom ( name "rom1.bin" size 100 crc 11111111 )
				rom ( name "rom2.bin" size 200 crc 22222222 )
				rom ( name "rom3.bin" size 300 crc 33333333 )
			)
			""";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

		// Act
		var result = await _parser.ParseAsync(stream, "test.dat");

		// Assert
		Assert.Single(result.Games);
		Assert.Equal(3, result.Games[0].Roms.Count);
		Assert.Equal(600L, result.Games[0].TotalSize); // Sum of all ROM sizes
	}

	[Theory]
	[InlineData("(USA)", "USA")]
	[InlineData("(Europe)", "Europe")]
	[InlineData("(Japan)", "Japan")]
	[InlineData("(World)", "World")]
	public async Task ParseAsync_ExtractsRegionFromGameName(string suffix, string? expectedRegion) {
		// Arrange
		var content = $"""
			clrmamepro (
				name "Test DAT"
			)

			game (
				name "Test Game {suffix}"
				rom ( name "test.rom" size 100 crc 12345678 )
			)
			""";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

		// Act
		var result = await _parser.ParseAsync(stream, "test.dat");

		// Assert
		Assert.Single(result.Games);
		Assert.Equal(expectedRegion, result.Games[0].Region);
	}

	[Theory]
	[InlineData("No-Intro", "no-intro", DatProvider.NoIntro)]
	[InlineData("TOSEC", "TOSEC", DatProvider.TOSEC)]
	[InlineData("Redump", "redump.org", DatProvider.Redump)]
	[InlineData("GoodSets", null, DatProvider.GoodSets)]
	public async Task ParseAsync_DetectsProviderCorrectly(string datName, string? homepage, DatProvider expectedProvider) {
		// Arrange
		var homepageLine = homepage != null ? $"homepage \"{homepage}\"" : "";
		var content = $"""
			clrmamepro (
				name "{datName}"
				{homepageLine}
			)
			""";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

		// Act
		var result = await _parser.ParseAsync(stream, "test.dat");

		// Assert
		Assert.Equal(expectedProvider, result.Provider);
	}

	[Fact]
	public async Task ParseAsync_WithCloneOfAttribute_SetsCloneOf() {
		// Arrange
		var content = """
			clrmamepro (
				name "Test DAT"
			)

			game (
				name "Parent Game"
				rom ( name "parent.rom" size 100 crc 11111111 )
			)

			game (
				name "Clone Game"
				cloneof "Parent Game"
				rom ( name "clone.rom" size 100 crc 22222222 )
			)
			""";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

		// Act
		var result = await _parser.ParseAsync(stream, "test.dat");

		// Assert
		Assert.Equal(2, result.Games.Count);
		Assert.Null(result.Games[0].CloneOf);
		Assert.Equal("Parent Game", result.Games[1].CloneOf);
	}

	[Fact]
	public async Task ParseAsync_WithEmptyContent_ReturnsEmptyDatFile() {
		// Arrange
		var content = """
			clrmamepro (
				name "Empty DAT"
			)
			""";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

		// Act
		var result = await _parser.ParseAsync(stream, "empty.dat");

		// Assert
		Assert.Equal("Empty DAT", result.Name);
		Assert.Empty(result.Games);
		Assert.Equal(0, result.GameCount);
	}

	[Fact]
	public async Task ParseAsync_ReportsProgress() {
		// Arrange
		var content = GenerateLargeDat(100);
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
		var progressReports = new List<DatParseProgress>();
		var progress = new Progress<DatParseProgress>(p => progressReports.Add(p));

		// Act
		var result = await _parser.ParseAsync(stream, "large.dat", progress);

		// Assert
		Assert.True(progressReports.Count > 0);
		Assert.Equal("Complete", progressReports[^1].Phase);
	}

	private static string GenerateLargeDat(int gameCount) {
		var sb = new StringBuilder();
		sb.AppendLine("clrmamepro (");
		sb.AppendLine("    name \"Large Test DAT\"");
		sb.AppendLine(")");
		sb.AppendLine();

		for (var i = 0; i < gameCount; i++) {
			sb.AppendLine("game (");
			sb.AppendLine($"    name \"Game {i:D5}\"");
			sb.AppendLine($"    rom ( name \"game{i:D5}.rom\" size {i * 100} crc {i:X8} )");
			sb.AppendLine(")");
			sb.AppendLine();
		}

		return sb.ToString();
	}
}
