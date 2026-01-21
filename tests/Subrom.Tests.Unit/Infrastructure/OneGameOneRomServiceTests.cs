using Microsoft.Extensions.Logging.Abstractions;
using Subrom.Application.Interfaces;
using Subrom.Infrastructure.Services;

namespace Subrom.Tests.Unit.Infrastructure;

/// <summary>
/// Unit tests for OneGameOneRomService.
/// </summary>
public class OneGameOneRomServiceTests {
	private readonly IOneGameOneRomService _service;
	private readonly OneGameOneRomOptions _defaultOptions = new();

	public OneGameOneRomServiceTests() {
		_service = new OneGameOneRomService(NullLogger<OneGameOneRomService>.Instance);
	}

	[Fact]
	public void Filter_WithSingleRom_ReturnsIt() {
		// Arrange
		var roms = new[] {
			CreateRom("Super Mario Bros (USA).nes", "USA")
		};

		// Act
		var result = _service.Filter(roms, _defaultOptions);

		// Assert
		Assert.Single(result);
		Assert.Equal("Super Mario Bros (USA).nes", result[0].Name);
	}

	[Fact]
	public void Filter_WithMultipleRegions_SelectsPreferredRegion() {
		// Arrange
		var roms = new[] {
			CreateRom("Super Mario Bros (Japan).nes", "Japan", cleanName: "Super Mario Bros"),
			CreateRom("Super Mario Bros (USA).nes", "USA", cleanName: "Super Mario Bros"),
			CreateRom("Super Mario Bros (Europe).nes", "Europe", cleanName: "Super Mario Bros")
		};

		// Act
		var result = _service.Filter(roms, _defaultOptions);

		// Assert
		Assert.Single(result);
		Assert.Equal("USA", result[0].Region);
	}

	[Fact]
	public void Filter_WithWorldRegion_PrefersWorldOverJapan() {
		// Arrange
		var roms = new[] {
			CreateRom("Tetris (Japan).nes", "Japan", cleanName: "Tetris"),
			CreateRom("Tetris (World).nes", "World", cleanName: "Tetris")
		};

		// Act
		var result = _service.Filter(roms, _defaultOptions);

		// Assert
		Assert.Single(result);
		Assert.Equal("World", result[0].Region);
	}

	[Fact]
	public void Filter_ExcludesDemosAndBetas() {
		// Arrange
		var roms = new[] {
			CreateRom("Game (USA) (Demo).nes", "USA", cleanName: "Game", categories: ["Demo"]),
			CreateRom("Game (USA) (Beta).nes", "USA", cleanName: "Game", categories: ["Beta"]),
			CreateRom("Game (Europe).nes", "Europe", cleanName: "Game")
		};

		// Act
		var result = _service.Filter(roms, _defaultOptions);

		// Assert
		Assert.Single(result);
		Assert.Equal("Europe", result[0].Region); // USA demos/betas excluded
	}

	[Fact]
	public void Filter_PrefersVerifiedDumps() {
		// Arrange
		var options = new OneGameOneRomOptions { PreferVerified = true };
		var roms = new[] {
			CreateRom("Game (USA).nes", "USA", cleanName: "Game", isVerified: false),
			CreateRom("Game (USA) [!].nes", "USA", cleanName: "Game", isVerified: true)
		};

		// Act
		var result = _service.Filter(roms, options);

		// Assert
		Assert.Single(result);
		Assert.True(result[0].IsVerified);
	}

	[Fact]
	public void Filter_PrefersLatestRevision() {
		// Arrange
		var options = new OneGameOneRomOptions { PreferLatestRevision = true };
		var roms = new[] {
			CreateRom("Game (USA).nes", "USA", cleanName: "Game", revision: 0),
			CreateRom("Game (USA) (Rev 1).nes", "USA", cleanName: "Game", revision: 1),
			CreateRom("Game (USA) (Rev 2).nes", "USA", cleanName: "Game", revision: 2)
		};

		// Act
		var result = _service.Filter(roms, options);

		// Assert
		Assert.Single(result);
		Assert.Equal(2, result[0].Revision);
	}

	[Fact]
	public void Filter_PrefersParentOverClone() {
		// Arrange
		var options = new OneGameOneRomOptions { PreferParent = true };
		var roms = new[] {
			CreateRom("Game (USA).nes", "USA", cleanName: "Game", parent: null),
			CreateRom("Game - Special Edition (USA).nes", "USA", cleanName: "Game - Special Edition", parent: "Game")
		};

		// Act
		var result = _service.Filter(roms, options);

		// Assert
		Assert.Single(result);
		Assert.Null(result[0].Parent);
	}

	[Fact]
	public void Filter_WithDifferentGames_ReturnsOneEach() {
		// Arrange
		var roms = new[] {
			CreateRom("Mario (USA).nes", "USA", cleanName: "Mario"),
			CreateRom("Mario (Japan).nes", "Japan", cleanName: "Mario"),
			CreateRom("Zelda (USA).nes", "USA", cleanName: "Zelda"),
			CreateRom("Zelda (Europe).nes", "Europe", cleanName: "Zelda")
		};

		// Act
		var result = _service.Filter(roms, _defaultOptions);

		// Assert
		Assert.Equal(2, result.Count);
		Assert.Contains(result, r => r.CleanName == "Mario" && r.Region == "USA");
		Assert.Contains(result, r => r.CleanName == "Zelda" && r.Region == "USA");
	}

	[Fact]
	public void ScoreRom_WithUsaRegion_HasHighScore() {
		// Arrange
		var rom = CreateRom("Game (USA).nes", "USA");

		// Act
		var score = _service.ScoreRom(rom, _defaultOptions);

		// Assert
		Assert.True(score > 0);
	}

	[Fact]
	public void ScoreRom_WithExcludedCategory_ReturnsNegative() {
		// Arrange
		var rom = CreateRom("Game (USA) (Demo).nes", "USA", categories: ["Demo"]);

		// Act
		var score = _service.ScoreRom(rom, _defaultOptions);

		// Assert
		Assert.True(score < 0);
	}

	[Fact]
	public void GroupAndSelect_ReturnsGroupInfo() {
		// Arrange
		var roms = new[] {
			CreateRom("Game (USA).nes", "USA", cleanName: "Game"),
			CreateRom("Game (Japan).nes", "Japan", cleanName: "Game"),
			CreateRom("Game (Europe).nes", "Europe", cleanName: "Game")
		};

		// Act
		var groups = _service.GroupAndSelect(roms, _defaultOptions);

		// Assert
		Assert.Single(groups);
		var group = groups[0];
		Assert.Equal("Game", group.GameName);
		Assert.Equal("USA", group.Selected.Region);
		Assert.Equal(3, group.AllRoms.Count);
		Assert.NotNull(group.SelectionReason);
	}

	[Fact]
	public void FromFilePath_ParsesNameCorrectly() {
		// Act
		var rom = OneGameOneRomService.FromFilePath("C:\\ROMs\\Super Mario Bros (USA) (Rev A) [!].nes");

		// Assert
		Assert.Equal("Super Mario Bros (USA) (Rev A) [!]", rom.Name);
		Assert.Equal("Super Mario Bros", rom.CleanName);
		Assert.Equal("USA", rom.Region);
		Assert.Equal(1, rom.Revision); // Rev A = 1
		Assert.True(rom.IsVerified);
	}

	[Fact]
	public void FromFilePath_DetectsDemoCategory() {
		// Act
		var rom = OneGameOneRomService.FromFilePath("C:\\ROMs\\Game (USA) (Demo).nes");

		// Assert
		Assert.Contains("Demo", rom.Categories);
	}

	[Fact]
	public void FromFilePath_DetectsBetaCategory() {
		// Act
		var rom = OneGameOneRomService.FromFilePath("C:\\ROMs\\Game (USA) (Beta).nes");

		// Assert
		Assert.Contains("Beta", rom.Categories);
	}

	[Theory]
	[InlineData("(Rev 1)", 1)]
	[InlineData("(Rev 2)", 2)]
	[InlineData("(Rev A)", 1)]
	[InlineData("(Rev B)", 2)]
	[InlineData("(v1.0)", 10)]
	[InlineData("(v1.1)", 11)]
	[InlineData("", 0)]
	public void FromFilePath_ParsesRevision(string revisionText, int expectedRevision) {
		// Act
		var rom = OneGameOneRomService.FromFilePath($"C:\\ROMs\\Game (USA) {revisionText}.nes");

		// Assert
		Assert.Equal(expectedRevision, rom.Revision);
	}

	private static RomCandidate CreateRom(
		string name,
		string? region = null,
		string? cleanName = null,
		string? parent = null,
		bool isVerified = true,
		int revision = 0,
		List<string>? categories = null) {
		return new RomCandidate {
			FilePath = $"C:\\ROMs\\{name}",
			Name = name,
			CleanName = cleanName ?? name.Split('(')[0].Trim(),
			Region = region,
			Parent = parent,
			IsVerified = isVerified,
			Revision = revision,
			Categories = categories ?? []
		};
	}
}
