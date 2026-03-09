using System.Text;
using Subrom.Domain.Aggregates.DatFiles;
using Subrom.Infrastructure.Parsing;

namespace Subrom.Tests.Unit.Infrastructure;

/// <summary>
/// Unit tests for <see cref="StreamingLogiqxParser"/>.
/// </summary>
public class StreamingLogiqxParserTests {
	private readonly StreamingLogiqxParser _parser = new();

	[Fact]
	public async Task ParseAsync_WithHeaderAndGames_ParsesAndLinksGamesToDatFile() {
		var xml = """
			<?xml version="1.0"?>
			<datafile>
				<header>
					<name>No-Intro Test DAT</name>
					<description>Sample DAT</description>
					<author>Unit Test</author>
					<homepage>https://no-intro.org</homepage>
					<category>NES</category>
				</header>
				<game name="Game A">
					<rom name="a.bin" size="123" crc="abcdef12"/>
				</game>
				<game name="Game B">
					<rom name="b.bin" size="456" crc="1234abcd"/>
				</game>
			</datafile>
			""";

		await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

		var result = await _parser.ParseAsync(stream, "test.dat");

		Assert.Equal("No-Intro Test DAT", result.Name);
		Assert.Equal(DatProvider.NoIntro, result.Provider);
		Assert.Equal(2, result.GameCount);
		Assert.All(result.Games, game => Assert.Equal(result.Id, game.DatFileId));
	}

	[Theory]
	[InlineData("sample.DAT")]
	[InlineData("sample.Xml")]
	public async Task CanParse_RecognizesDatAndXmlExtensions_CaseInsensitive(string fileName) {
		var xml = """
			<?xml version="1.0"?>
			<datafile>
				<header><name>Test</name></header>
			</datafile>
			""";
		var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-{fileName}");
		await File.WriteAllTextAsync(path, xml);

		try {
			Assert.True(_parser.CanParse(path));
		} finally {
			File.Delete(path);
		}
	}
}
