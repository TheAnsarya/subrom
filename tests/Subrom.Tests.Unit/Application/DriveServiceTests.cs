using Moq;
using Subrom.Application.Interfaces;
using Subrom.Application.Services;
using Subrom.Domain.Aggregates.Storage;

using AppDriveType = Subrom.Domain.Aggregates.Storage.DriveType;

namespace Subrom.Tests.Unit.Application;

/// <summary>
/// Unit tests for DriveService.
/// </summary>
public class DriveServiceTests : IDisposable {
	private readonly Mock<IDriveRepository> _mockDriveRepo;
	private readonly Mock<IUnitOfWork> _mockUnitOfWork;
	private readonly DriveService _service;
	private readonly string _testDir;

	public DriveServiceTests() {
		_mockDriveRepo = new Mock<IDriveRepository>();
		_mockUnitOfWork = new Mock<IUnitOfWork>();
		_service = new DriveService(_mockDriveRepo.Object, _mockUnitOfWork.Object);

		// Create temp directories for file system tests
		_testDir = Path.Combine(Path.GetTempPath(), $"subrom_drive_test_{Guid.NewGuid():N}");
		Directory.CreateDirectory(_testDir);
	}

	public void Dispose() {
		try {
			if (Directory.Exists(_testDir)) {
				Directory.Delete(_testDir, recursive: true);
			}
		} catch {
			// Ignore cleanup failures
		}

		GC.SuppressFinalize(this);
	}

	[Fact]
	public async Task RegisterAsync_WithValidPath_CreatesDrive() {
		// Arrange
		var label = "Test Drive";
		var rootPath = _testDir;

		// Act
		var drive = await _service.RegisterAsync(label, rootPath);

		// Assert
		Assert.NotNull(drive);
		Assert.Equal(label, drive.Label);
		Assert.Equal(rootPath, drive.RootPath);
		Assert.True(drive.IsOnline);
		_mockDriveRepo.Verify(r => r.AddAsync(It.IsAny<Drive>(), It.IsAny<CancellationToken>()), Times.Once);
		_mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task RegisterAsync_WithNonExistentPath_ThrowsDirectoryNotFoundException() {
		// Arrange
		var label = "Test";
		var rootPath = @"C:\This\Path\Does\Not\Exist";

		// Act & Assert
		await Assert.ThrowsAsync<DirectoryNotFoundException>(
			() => _service.RegisterAsync(label, rootPath));
	}

	[Fact]
	public async Task RegisterAsync_WithEmptyLabel_ThrowsArgumentException() {
		// Act & Assert
		await Assert.ThrowsAsync<ArgumentException>(
			() => _service.RegisterAsync("", _testDir));
	}

	[Fact]
	public async Task RegisterAsync_WithEmptyPath_ThrowsArgumentException() {
		// Act & Assert
		await Assert.ThrowsAsync<ArgumentException>(
			() => _service.RegisterAsync("Test", ""));
	}

	[Fact]
	public async Task RegisterAsync_WithNullLabel_ThrowsArgumentException() {
		// Act & Assert
		await Assert.ThrowsAsync<ArgumentNullException>(
			() => _service.RegisterAsync(null!, _testDir));
	}

	[Fact]
	public async Task RegisterAsync_WithNetworkPath_AllowsOfflineRegistration() {
		// Arrange
		var uncPath = @"\\nonexistent\share";

		// Act
		var drive = await _service.RegisterAsync("Network", uncPath, AppDriveType.Network);

		// Assert
		Assert.NotNull(drive);
		Assert.False(drive.IsOnline);
		_mockDriveRepo.Verify(r => r.AddAsync(It.IsAny<Drive>(), It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task RegisterNetworkDriveAsync_CreatesNetworkDrive() {
		// Arrange
		var label = "NAS";
		var uncPath = @"\\server\share";

		// Act
		var drive = await _service.RegisterNetworkDriveAsync(label, uncPath);

		// Assert
		Assert.NotNull(drive);
		Assert.Equal(label, drive.Label);
		Assert.Equal(uncPath, drive.RootPath);
		Assert.False(drive.IsOnline); // Network path doesn't exist
		_mockDriveRepo.Verify(r => r.AddAsync(It.IsAny<Drive>(), It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task GetAllAsync_ReturnsAllDrives() {
		// Arrange
		var drives = new List<Drive> {
			Drive.Create("Drive1", @"C:\Path1"),
			Drive.Create("Drive2", @"C:\Path2")
		};
		_mockDriveRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(drives);

		// Act
		var result = await _service.GetAllAsync();

		// Assert
		Assert.Equal(2, result.Count);
	}

	[Fact]
	public async Task GetOnlineAsync_ReturnsOnlyOnlineDrives() {
		// Arrange
		var onlineDrives = new List<Drive> { Drive.Create("Online", @"C:\Online") };
		_mockDriveRepo.Setup(r => r.GetOnlineAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(onlineDrives);

		// Act
		var result = await _service.GetOnlineAsync();

		// Assert
		Assert.Single(result);
	}

	[Fact]
	public async Task GetByIdAsync_WithExistingId_ReturnsDrive() {
		// Arrange
		var id = Guid.NewGuid();
		var drive = Drive.Create("Test", _testDir);
		_mockDriveRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
			.ReturnsAsync(drive);

		// Act
		var result = await _service.GetByIdAsync(id);

		// Assert
		Assert.NotNull(result);
		Assert.Equal("Test", result.Label);
	}

	[Fact]
	public async Task GetByIdAsync_WithNonExistentId_ReturnsNull() {
		// Arrange
		var id = Guid.NewGuid();
		_mockDriveRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
			.ReturnsAsync((Drive?)null);

		// Act
		var result = await _service.GetByIdAsync(id);

		// Assert
		Assert.Null(result);
	}

	[Fact]
	public async Task RefreshStatusAsync_WithNonExistentDrive_ThrowsKeyNotFoundException() {
		// Arrange
		var id = Guid.NewGuid();
		_mockDriveRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
			.ReturnsAsync((Drive?)null);

		// Act & Assert
		await Assert.ThrowsAsync<KeyNotFoundException>(
			() => _service.RefreshStatusAsync(id));
	}

	[Fact]
	public async Task RefreshStatusAsync_WithOnlineDrive_UpdatesStatus() {
		// Arrange
		var id = Guid.NewGuid();
		var drive = Drive.Create("Test", _testDir);
		drive.IsOnline = false; // Initially offline

		_mockDriveRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
			.ReturnsAsync(drive);

		// Act
		var result = await _service.RefreshStatusAsync(id);

		// Assert
		Assert.True(result.IsOnline);
		_mockDriveRepo.Verify(r => r.UpdateAsync(It.IsAny<Drive>(), It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task RefreshAllStatusAsync_UpdatesAllDrives() {
		// Arrange
		var drives = new List<Drive> {
			Drive.Create("Online", _testDir),
			Drive.Create("Offline", @"C:\NonExistent")
		};
		_mockDriveRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(drives);

		// Act
		var result = await _service.RefreshAllStatusAsync();

		// Assert
		Assert.Equal(2, result.Count);
		_mockDriveRepo.Verify(r => r.UpdateAsync(It.IsAny<Drive>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
		_mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task AddScanPathAsync_WithValidPath_AddsScanPath() {
		// Arrange
		var id = Guid.NewGuid();
		var subPath = "ROMs";
		Directory.CreateDirectory(Path.Combine(_testDir, subPath));

		var drive = Drive.Create("Test", _testDir);
		_mockDriveRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
			.ReturnsAsync(drive);

		// Act
		var result = await _service.AddScanPathAsync(id, subPath);

		// Assert
		Assert.Contains(subPath, result.ScanPaths);
		_mockDriveRepo.Verify(r => r.UpdateAsync(It.IsAny<Drive>(), It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task AddScanPathAsync_WithNonExistentPath_ThrowsDirectoryNotFoundException() {
		// Arrange
		var id = Guid.NewGuid();
		var drive = Drive.Create("Test", _testDir);
		_mockDriveRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
			.ReturnsAsync(drive);

		// Act & Assert
		await Assert.ThrowsAsync<DirectoryNotFoundException>(
			() => _service.AddScanPathAsync(id, "NonExistent"));
	}

	[Fact]
	public async Task AddScanPathAsync_WithNonExistentDrive_ThrowsKeyNotFoundException() {
		// Arrange
		var id = Guid.NewGuid();
		_mockDriveRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
			.ReturnsAsync((Drive?)null);

		// Act & Assert
		await Assert.ThrowsAsync<KeyNotFoundException>(
			() => _service.AddScanPathAsync(id, "path"));
	}

	[Fact]
	public async Task RemoveScanPathAsync_RemovesScanPath() {
		// Arrange
		var id = Guid.NewGuid();
		var drive = Drive.Create("Test", _testDir);
		drive.ScanPaths.Add("ROMs");
		drive.ScanPaths.Add("ISOs");

		_mockDriveRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
			.ReturnsAsync(drive);

		// Act
		var result = await _service.RemoveScanPathAsync(id, "ROMs");

		// Assert
		Assert.DoesNotContain("ROMs", result.ScanPaths);
		Assert.Contains("ISOs", result.ScanPaths);
		_mockDriveRepo.Verify(r => r.UpdateAsync(It.IsAny<Drive>(), It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task DeleteAsync_RemovesDrive() {
		// Arrange
		var id = Guid.NewGuid();
		var drive = Drive.Create("Test", _testDir);
		_mockDriveRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
			.ReturnsAsync(drive);

		// Act
		await _service.DeleteAsync(id);

		// Assert
		_mockDriveRepo.Verify(r => r.RemoveAsync(It.IsAny<Drive>(), It.IsAny<CancellationToken>()), Times.Once);
		_mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task DeleteAsync_WithNonExistentDrive_ThrowsKeyNotFoundException() {
		// Arrange
		var id = Guid.NewGuid();
		_mockDriveRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
			.ReturnsAsync((Drive?)null);

		// Act & Assert
		await Assert.ThrowsAsync<KeyNotFoundException>(
			() => _service.DeleteAsync(id));
	}
}
