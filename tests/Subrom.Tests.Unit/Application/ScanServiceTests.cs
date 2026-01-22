using Moq;
using Subrom.Application.Interfaces;
using Subrom.Application.Services;
using Subrom.Domain.Aggregates.Scanning;
using Subrom.Domain.Aggregates.Storage;

using AppDriveType = Subrom.Domain.Aggregates.Storage.DriveType;

namespace Subrom.Tests.Unit.Application;

/// <summary>
/// Unit tests for ScanService.
/// </summary>
public class ScanServiceTests {
	private readonly Mock<IDriveRepository> _driveRepoMock;
	private readonly Mock<IRomFileRepository> _romFileRepoMock;
	private readonly Mock<IScanJobRepository> _scanJobRepoMock;
	private readonly Mock<IHashService> _hashServiceMock;
	private readonly Mock<IUnitOfWork> _unitOfWorkMock;
	private readonly ScanService _service;

	public ScanServiceTests() {
		_driveRepoMock = new Mock<IDriveRepository>();
		_romFileRepoMock = new Mock<IRomFileRepository>();
		_scanJobRepoMock = new Mock<IScanJobRepository>();
		_hashServiceMock = new Mock<IHashService>();
		_unitOfWorkMock = new Mock<IUnitOfWork>();

		_service = new ScanService(
			_driveRepoMock.Object,
			_romFileRepoMock.Object,
			_scanJobRepoMock.Object,
			_hashServiceMock.Object,
			_unitOfWorkMock.Object);
	}

	[Fact]
	public async Task StartScanAsync_WithValidDrive_CreatesScanJob() {
		// Arrange
		var driveId = Guid.NewGuid();
		var drive = CreateDrive(driveId, isOnline: true);

		_driveRepoMock.Setup(r => r.GetByIdAsync(driveId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(drive);
		_scanJobRepoMock.Setup(r => r.HasActiveJobForDriveAsync(driveId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(false);

		// Act
		var result = await _service.StartScanAsync(driveId);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(driveId, result.DriveId);
		Assert.Equal(ScanStatus.Queued, result.Status);
		_scanJobRepoMock.Verify(r => r.AddAsync(It.IsAny<ScanJob>(), It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task StartScanAsync_WithNonExistentDrive_ThrowsKeyNotFoundException() {
		// Arrange
		var driveId = Guid.NewGuid();
		_driveRepoMock.Setup(r => r.GetByIdAsync(driveId, It.IsAny<CancellationToken>()))
			.ReturnsAsync((Drive?)null);

		// Act & Assert
		await Assert.ThrowsAsync<KeyNotFoundException>(
			() => _service.StartScanAsync(driveId));
	}

	[Fact]
	public async Task StartScanAsync_WithOfflineDrive_ThrowsInvalidOperationException() {
		// Arrange
		var driveId = Guid.NewGuid();
		var drive = CreateDrive(driveId, isOnline: false);

		_driveRepoMock.Setup(r => r.GetByIdAsync(driveId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(drive);

		// Act & Assert
		await Assert.ThrowsAsync<InvalidOperationException>(
			() => _service.StartScanAsync(driveId));
	}

	[Fact]
	public async Task StartScanAsync_WithActiveScan_ThrowsInvalidOperationException() {
		// Arrange
		var driveId = Guid.NewGuid();
		var drive = CreateDrive(driveId, isOnline: true);

		_driveRepoMock.Setup(r => r.GetByIdAsync(driveId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(drive);
		_scanJobRepoMock.Setup(r => r.HasActiveJobForDriveAsync(driveId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		// Act & Assert
		await Assert.ThrowsAsync<InvalidOperationException>(
			() => _service.StartScanAsync(driveId));
	}

	[Fact]
	public async Task StartScanAsync_WithTargetPath_SetsScanTargetPath() {
		// Arrange
		var driveId = Guid.NewGuid();
		var targetPath = "ROMs/Nintendo";
		var drive = CreateDrive(driveId, isOnline: true);

		_driveRepoMock.Setup(r => r.GetByIdAsync(driveId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(drive);
		_scanJobRepoMock.Setup(r => r.HasActiveJobForDriveAsync(driveId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(false);

		// Act
		var result = await _service.StartScanAsync(driveId, targetPath: targetPath);

		// Assert
		Assert.Equal(targetPath, result.TargetPath);
	}

	[Fact]
	public async Task StartScanAsync_WithScanType_SetsScanType() {
		// Arrange
		var driveId = Guid.NewGuid();
		var drive = CreateDrive(driveId, isOnline: true);

		_driveRepoMock.Setup(r => r.GetByIdAsync(driveId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(drive);
		_scanJobRepoMock.Setup(r => r.HasActiveJobForDriveAsync(driveId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(false);

		// Act
		var result = await _service.StartScanAsync(driveId, scanType: ScanType.Hashing);

		// Assert
		Assert.Equal(ScanType.Hashing, result.Type);
	}

	[Fact]
	public async Task CancelScanAsync_WithActiveJob_CancelsJob() {
		// Arrange
		var jobId = Guid.NewGuid();
		var job = ScanJob.Create(ScanType.Full, Guid.NewGuid());

		_scanJobRepoMock.Setup(r => r.GetByIdAsync(jobId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(job);

		// Act
		await _service.CancelScanAsync(jobId);

		// Assert
		_scanJobRepoMock.Verify(r => r.UpdateAsync(It.IsAny<ScanJob>(), It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task GetJobsAsync_ReturnsAllJobs() {
		// Arrange
		var jobs = new List<ScanJob> {
			ScanJob.Create(ScanType.Full, Guid.NewGuid()),
			ScanJob.Create(ScanType.Full, Guid.NewGuid())
		};

		_scanJobRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(jobs);

		// Act
		var result = await _service.GetJobsAsync();

		// Assert
		Assert.Equal(2, result.Count);
	}

	[Fact]
	public async Task GetJobsAsync_WithDriveId_FiltersByDrive() {
		// Arrange
		var driveId = Guid.NewGuid();
		var jobs = new List<ScanJob> {
			ScanJob.Create(ScanType.Full, driveId)
		};

		_scanJobRepoMock.Setup(r => r.GetByDriveAsync(driveId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(jobs);

		// Act
		var result = await _service.GetJobsAsync(driveId);

		// Assert
		Assert.Single(result);
		_scanJobRepoMock.Verify(r => r.GetByDriveAsync(driveId, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task GetActiveJobsAsync_ReturnsOnlyActiveJobs() {
		// Arrange
		var activeJobs = new List<ScanJob> {
			ScanJob.Create(ScanType.Full, Guid.NewGuid())
		};

		_scanJobRepoMock.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(activeJobs);

		// Act
		var result = await _service.GetActiveJobsAsync();

		// Assert
		Assert.Single(result);
	}

	[Theory]
	[InlineData(ScanType.Full)]
	[InlineData(ScanType.FileDiscovery)]
	[InlineData(ScanType.Hashing)]
	[InlineData(ScanType.Verification)]
	public async Task StartScanAsync_WithDifferentScanTypes_CreatesCorrectJobType(ScanType scanType) {
		// Arrange
		var driveId = Guid.NewGuid();
		var drive = CreateDrive(driveId, isOnline: true);

		_driveRepoMock.Setup(r => r.GetByIdAsync(driveId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(drive);
		_scanJobRepoMock.Setup(r => r.HasActiveJobForDriveAsync(driveId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(false);

		// Act
		var result = await _service.StartScanAsync(driveId, scanType: scanType);

		// Assert
		Assert.Equal(scanType, result.Type);
	}

	// Helper methods
	private static Drive CreateDrive(Guid id, bool isOnline) {
		var drive = Drive.Create(
			label: "Test Drive",
			rootPath: @"C:\Test",
			driveType: AppDriveType.Fixed);
		drive.IsOnline = isOnline;
		return drive;
	}
}
