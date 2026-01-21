using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Subrom.Application.Interfaces;
using Subrom.Domain.Aggregates.DatFiles;
using Subrom.Infrastructure.Services;

namespace Subrom.Tests.Unit.Infrastructure;

/// <summary>
/// Unit tests for ParentCloneService.
/// </summary>
public class ParentCloneServiceTests {
	private readonly Mock<IDatFileRepository> _mockDatRepo;
	private readonly ParentCloneService _service;

	public ParentCloneServiceTests() {
		_mockDatRepo = new Mock<IDatFileRepository>();
		_service = new ParentCloneService(
			NullLogger<ParentCloneService>.Instance,
			_mockDatRepo.Object);
	}

	[Fact]
	public void InferRelationships_GroupsByCleanName() {
		// Arrange
		var names = new[] {
			"Super Mario Bros (USA)",
			"Super Mario Bros (Japan)",
			"Super Mario Bros (Europe)"
		};

		// Act
		var groups = _service.InferRelationships(names);

		// Assert
		Assert.Single(groups);
		Assert.Equal(3, groups[0].TotalCount);
	}

	[Fact]
	public void InferRelationships_DifferentGamesAreSeparate() {
		// Arrange
		var names = new[] {
			"Mario (USA)",
			"Zelda (USA)"
		};

		// Act
		var groups = _service.InferRelationships(names);

		// Assert
		Assert.Equal(2, groups.Count);
	}

	[Fact]
	public void InferRelationships_RevisionsGroupedWithParent() {
		// Arrange
		var names = new[] {
			"Game (USA)",
			"Game (USA) (Rev A)",
			"Game (USA) (Rev B)"
		};

		// Act
		var groups = _service.InferRelationships(names);

		// Assert
		Assert.Single(groups);
		var group = groups[0];
		Assert.Equal("Game (USA)", group.Parent);
		Assert.Contains("Game (USA) (Rev A)", group.Clones);
		Assert.Contains("Game (USA) (Rev B)", group.Clones);
	}

	[Fact]
	public void InferRelationships_PrefersSimplestNameAsParent() {
		// Arrange
		var names = new[] {
			"Game (USA) (Rev A)",
			"Game (USA)"
		};

		// Act
		var groups = _service.InferRelationships(names);

		// Assert
		Assert.Single(groups);
		Assert.Equal("Game (USA)", groups[0].Parent);
	}

	[Fact]
	public void InferRelationships_VersionMarkersAreClones() {
		// Arrange
		var names = new[] {
			"Game (USA)",
			"Game (USA) (v1.1)"
		};

		// Act
		var groups = _service.InferRelationships(names);

		// Assert
		Assert.Single(groups);
		Assert.Equal("Game (USA)", groups[0].Parent);
	}

	[Fact]
	public async Task BuildIndexFromDatAsync_CreatesIndex() {
		// Arrange
		var datId = Guid.NewGuid();
		var datFile = CreateTestDatFile(datId, [
			new GameEntry { Name = "Parent Game", CloneOf = null },
			new GameEntry { Name = "Clone 1", CloneOf = "Parent Game" },
			new GameEntry { Name = "Clone 2", CloneOf = "Parent Game" }
		]);

		_mockDatRepo.Setup(r => r.GetByIdWithGamesAsync(datId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(datFile);

		// Act
		var index = await _service.BuildIndexFromDatAsync(datId);

		// Assert
		Assert.Equal(1, index.ParentCount);
		Assert.Equal(2, index.CloneCount);
		Assert.True(index.IsParent("Parent Game"));
		Assert.True(index.IsClone("Clone 1"));
		Assert.True(index.IsClone("Clone 2"));
		Assert.Equal("Parent Game", index.GetParent("Clone 1"));
	}

	[Fact]
	public async Task GetParentAsync_WithDat_ReturnsFromIndex() {
		// Arrange
		var datId = Guid.NewGuid();
		var datFile = CreateTestDatFile(datId, [
			new GameEntry { Name = "Parent", CloneOf = null },
			new GameEntry { Name = "Clone", CloneOf = "Parent" }
		]);

		_mockDatRepo.Setup(r => r.GetByIdWithGamesAsync(datId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(datFile);

		// Act
		var parent = await _service.GetParentAsync("Clone", datId);

		// Assert
		Assert.Equal("Parent", parent);
	}

	[Fact]
	public async Task GetClonesAsync_ReturnsCloneList() {
		// Arrange
		var datId = Guid.NewGuid();
		var datFile = CreateTestDatFile(datId, [
			new GameEntry { Name = "Parent", CloneOf = null },
			new GameEntry { Name = "Clone1", CloneOf = "Parent" },
			new GameEntry { Name = "Clone2", CloneOf = "Parent" }
		]);

		_mockDatRepo.Setup(r => r.GetByIdWithGamesAsync(datId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(datFile);

		// Act
		var clones = await _service.GetClonesAsync("Parent", datId);

		// Assert
		Assert.Equal(2, clones.Count);
		Assert.Contains("Clone1", clones);
		Assert.Contains("Clone2", clones);
	}

	[Fact]
	public void ParentCloneIndex_TracksRelationships() {
		// Arrange
		var index = new ParentCloneIndex();

		// Act
		index.AddRelationship("Parent1", "Clone1A");
		index.AddRelationship("Parent1", "Clone1B");
		index.AddRelationship("Parent2", "Clone2A");

		// Assert
		Assert.Equal(2, index.ParentCount);
		Assert.Equal(3, index.CloneCount);
		Assert.Equal("Parent1", index.GetParent("Clone1A"));
		Assert.Null(index.GetParent("Parent1")); // Parent has no parent
	}

	[Fact]
	public void ParentCloneIndex_GetAllGroups_ReturnsAllParents() {
		// Arrange
		var index = new ParentCloneIndex();
		index.AddRelationship("Game1", "Game1 Clone");
		index.AddRelationship("Game2", "Game2 Clone");

		// Act
		var groups = index.GetAllGroups();

		// Assert
		Assert.Equal(2, groups.Count);
		Assert.Contains(groups, g => g.Parent == "Game1");
		Assert.Contains(groups, g => g.Parent == "Game2");
	}

	private static DatFile CreateTestDatFile(Guid id, IEnumerable<GameEntry> games) {
		var datFile = new DatFile {
			FileName = "test.dat",
			Name = "Test DAT"
		};

		// Use reflection to set the Id since it's protected
		typeof(DatFile).GetProperty("Id")!.SetValue(datFile, id);

		// Add games through the proper method
		foreach (var game in games) {
			datFile.AddGame(game);
		}

		return datFile;
	}
}
