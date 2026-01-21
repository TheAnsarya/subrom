using Moq;
using Subrom.Application.Interfaces;
using Subrom.Application.Services;
using Subrom.Domain.Aggregates.DatFiles;
using Subrom.Domain.ValueObjects;

namespace Subrom.Tests.Unit.Application.Services;

public class DatFileServiceTests {
	private readonly Mock<IDatFileRepository> _mockRepo;
	private readonly Mock<IDatParserFactory> _mockParserFactory;
	private readonly Mock<IUnitOfWork> _mockUnitOfWork;
	private readonly DatFileService _service;

	public DatFileServiceTests() {
		_mockRepo = new Mock<IDatFileRepository>();
		_mockParserFactory = new Mock<IDatParserFactory>();
		_mockUnitOfWork = new Mock<IUnitOfWork>();
		_service = new DatFileService(_mockRepo.Object, _mockParserFactory.Object, _mockUnitOfWork.Object);
	}

	[Fact]
	public async Task GetAllAsync_ReturnsAllDatFiles() {
		// Arrange
		var datFiles = new List<DatFile> {
			DatFile.Create("test1.dat", "Test 1"),
			DatFile.Create("test2.dat", "Test 2")
		};
		_mockRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(datFiles);

		// Act
		var result = await _service.GetAllAsync();

		// Assert
		Assert.Equal(2, result.Count);
		_mockRepo.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task GetByIdAsync_ExistingId_ReturnsDatFile() {
		// Arrange
		var id = Guid.NewGuid();
		var datFile = DatFile.Create("test.dat", "Test");
		_mockRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
			.ReturnsAsync(datFile);

		// Act
		var result = await _service.GetByIdAsync(id);

		// Assert
		Assert.NotNull(result);
		Assert.Equal("test.dat", result.FileName);
	}

	[Fact]
	public async Task GetByIdAsync_NonExistingId_ReturnsNull() {
		// Arrange
		var id = Guid.NewGuid();
		_mockRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
			.ReturnsAsync((DatFile?)null);

		// Act
		var result = await _service.GetByIdAsync(id);

		// Assert
		Assert.Null(result);
	}

	[Fact]
	public async Task SetCategoryAsync_UpdatesCategoryPath() {
		// Arrange
		var id = Guid.NewGuid();
		var datFile = DatFile.Create("test.dat", "Test");
		_mockRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
			.ReturnsAsync(datFile);

		// Act
		var result = await _service.SetCategoryAsync(id, "Nintendo/SNES");

		// Assert
		Assert.Equal("Nintendo/SNES", result.CategoryPath);
		_mockRepo.Verify(r => r.UpdateAsync(datFile, It.IsAny<CancellationToken>()), Times.Once);
		_mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task ToggleEnabledAsync_TogglesIsEnabledFlag() {
		// Arrange
		var id = Guid.NewGuid();
		var datFile = DatFile.Create("test.dat", "Test");
		var initialState = datFile.IsEnabled;

		_mockRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
			.ReturnsAsync(datFile);

		// Act
		var result = await _service.ToggleEnabledAsync(id);

		// Assert
		Assert.NotEqual(initialState, result.IsEnabled);
		_mockRepo.Verify(r => r.UpdateAsync(datFile, It.IsAny<CancellationToken>()), Times.Once);
		_mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
	}
}
