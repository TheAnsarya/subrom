using Subrom.Domain.Aggregates.DatFiles;

namespace Subrom.Tests.Unit.Domain.Aggregates;

public class DatFileTests {
	[Fact]
	public void Create_ValidParameters_Success() {
		// Arrange & Act
		var datFile = DatFile.Create("Nintendo - SNES.dat", "Nintendo - Super Nintendo Entertainment System");

		// Assert
		Assert.NotNull(datFile);
		Assert.Equal("Nintendo - SNES.dat", datFile.FileName);
		Assert.Equal("Nintendo - Super Nintendo Entertainment System", datFile.Name);
		Assert.Equal(DatFormat.LogiqxXml, datFile.Format);
		Assert.Equal(0, datFile.GameCount);
	}

	[Fact]
	public void AddGame_SingleGame_IncrementsCount() {
		// Arrange
		var datFile = DatFile.Create("test.dat", "Test DAT");
		var game = new GameEntry {
			Name = "Super Mario World",
			Description = "Super Mario World (USA)"
		};

		// Act
		datFile.AddGame(game);

		// Assert
		Assert.Equal(1, datFile.GameCount);
	}

	[Fact]
	public void AddGames_MultipleGames_IncrementsCount() {
		// Arrange
		var datFile = DatFile.Create("test.dat", "Test DAT");
		var games = new[] {
			new GameEntry { Name = "Game1", Description = "Game 1" },
			new GameEntry { Name = "Game2", Description = "Game 2" },
			new GameEntry { Name = "Game3", Description = "Game 3" }
		};

		// Act
		datFile.AddGames(games);

		// Assert
		Assert.Equal(3, datFile.GameCount);
	}

	[Fact]
	public void SetCategoryPath_UpdatesProperty() {
		// Arrange
		var datFile = DatFile.Create("test.dat", "Test DAT");

		// Act
		datFile.CategoryPath = "Nintendo/SNES/Games";

		// Assert
		Assert.Equal("Nintendo/SNES/Games", datFile.CategoryPath);
	}

	[Fact]
	public void SetProvider_UpdatesProperty() {
		// Arrange
		var datFile = DatFile.Create("test.dat", "Test DAT");

		// Act
		datFile.Provider = DatProvider.NoIntro;

		// Assert
		Assert.Equal(DatProvider.NoIntro, datFile.Provider);
	}
}
