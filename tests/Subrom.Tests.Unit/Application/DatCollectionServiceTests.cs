using Moq;
using Subrom.Application.Interfaces;
using Subrom.Application.Services;
using Subrom.Domain.Aggregates.DatFiles;

namespace Subrom.Tests.Unit.Application;

/// <summary>
/// Unit tests for DatCollectionService.
/// </summary>
public class DatCollectionServiceTests {
	private readonly Mock<IDatProvider> _mockProvider;
	private readonly Mock<IDatFileRepository> _mockDatFileRepo;
	private readonly Mock<IDatParserFactory> _mockParserFactory;
	private readonly Mock<IUnitOfWork> _mockUnitOfWork;
	private readonly DatCollectionService _service;

	public DatCollectionServiceTests() {
		_mockProvider = new Mock<IDatProvider>();
		_mockProvider.Setup(p => p.ProviderType).Returns(DatProvider.NoIntro);

		_mockDatFileRepo = new Mock<IDatFileRepository>();
		_mockParserFactory = new Mock<IDatParserFactory>();
		_mockUnitOfWork = new Mock<IUnitOfWork>();

		_service = new DatCollectionService(
			[_mockProvider.Object],
			_mockDatFileRepo.Object,
			_mockParserFactory.Object,
			_mockUnitOfWork.Object);
	}

	[Fact]
	public async Task SyncProviderAsync_WithUnregisteredProvider_ThrowsInvalidOperationException() {
		// Arrange - create service with empty providers
		var service = new DatCollectionService(
			[],
			_mockDatFileRepo.Object,
			_mockParserFactory.Object,
			_mockUnitOfWork.Object);

		// Act & Assert
		await Assert.ThrowsAsync<InvalidOperationException>(
			() => service.SyncProviderAsync(DatProvider.NoIntro));
	}

	[Fact]
	public async Task SyncProviderAsync_WithNoDats_ReturnsZero() {
		// Arrange
		_mockProvider.Setup(p => p.ListAvailableAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);
		_mockDatFileRepo.Setup(r => r.GetByProviderAsync(DatProvider.NoIntro, It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);

		// Act
		var result = await _service.SyncProviderAsync(DatProvider.NoIntro);

		// Assert
		Assert.Equal(0, result);
	}

	[Fact]
	public async Task SyncProviderAsync_WithExistingUpToDateDat_SkipsDownload() {
		// Arrange
		var metadata = new DatMetadata {
			Identifier = "nes",
			Name = "Nintendo - NES",
			Version = "2024-01-01"
		};

		var existingDat = DatFile.Create("nes.dat", "Nintendo - NES");
		existingDat.Version = "2024-01-01";

		_mockProvider.Setup(p => p.ListAvailableAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync([metadata]);
		_mockDatFileRepo.Setup(r => r.GetByProviderAsync(DatProvider.NoIntro, It.IsAny<CancellationToken>()))
			.ReturnsAsync([existingDat]);

		// Act
		var result = await _service.SyncProviderAsync(DatProvider.NoIntro);

		// Assert
		Assert.Equal(0, result);
		_mockProvider.Verify(p => p.DownloadDatAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task SyncProviderAsync_WithForceRefresh_DownloadsAllDats() {
		// Arrange
		var metadata = new DatMetadata {
			Identifier = "nes",
			Name = "Nintendo - NES",
			Version = "2024-01-01"
		};

		var existingDat = DatFile.Create("nes.dat", "Nintendo - NES");
		existingDat.Version = "2024-01-01";

		var datStream = new MemoryStream();

		_mockProvider.Setup(p => p.ListAvailableAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync([metadata]);
		_mockDatFileRepo.Setup(r => r.GetByProviderAsync(DatProvider.NoIntro, It.IsAny<CancellationToken>()))
			.ReturnsAsync([existingDat]);
		_mockProvider.Setup(p => p.DownloadDatAsync("nes", It.IsAny<CancellationToken>()))
			.ReturnsAsync(datStream);
		_mockParserFactory.Setup(f => f.GetParser(It.IsAny<Stream>()))
			.Returns((IDatParser?)null); // No parser, will be skipped

		// Act
		var result = await _service.SyncProviderAsync(DatProvider.NoIntro, forceRefresh: true);

		// Assert
		_mockProvider.Verify(p => p.DownloadDatAsync("nes", It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task SyncProviderAsync_ReportsProgress() {
		// Arrange
		var metadata = new DatMetadata {
			Identifier = "nes",
			Name = "Nintendo - NES",
			Version = "2024-01-01"
		};

		var progressReports = new List<DatSyncProgress>();
		var progress = new Progress<DatSyncProgress>(p => progressReports.Add(p));

		_mockProvider.Setup(p => p.ListAvailableAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync([metadata]);
		_mockDatFileRepo.Setup(r => r.GetByProviderAsync(DatProvider.NoIntro, It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);
		_mockProvider.Setup(p => p.DownloadDatAsync("nes", It.IsAny<CancellationToken>()))
			.ReturnsAsync(new MemoryStream());
		_mockParserFactory.Setup(f => f.GetParser(It.IsAny<Stream>()))
			.Returns((IDatParser?)null); // No parser

		// Act
		await _service.SyncProviderAsync(DatProvider.NoIntro, progress: progress);

		// Small delay for async progress
		await Task.Delay(100);

		// Assert - should have reported at least discovering and downloading phases
		Assert.True(progressReports.Count > 0);
	}

	[Fact]
	public async Task SyncAllAsync_SyncsAllProviders() {
		// Arrange
		_mockProvider.Setup(p => p.ListAvailableAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);
		_mockDatFileRepo.Setup(r => r.GetByProviderAsync(It.IsAny<DatProvider>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);

		// Act
		var report = await _service.SyncAllAsync();

		// Assert
		Assert.Equal(1, report.ProvidersProcessed);
		Assert.Equal(0, report.Errors);
	}

	[Fact]
	public async Task SyncAllAsync_CapturesErrors() {
		// Arrange
		_mockProvider.Setup(p => p.ListAvailableAsync(It.IsAny<CancellationToken>()))
			.ThrowsAsync(new Exception("Test error"));

		// Act
		var report = await _service.SyncAllAsync();

		// Assert
		Assert.Equal(1, report.Errors);
		Assert.Contains(report.ErrorMessages, m => m.Contains("Test error"));
	}

	[Fact]
	public async Task GetOutdatedDatsAsync_WithDefaultAge_Returns30DaysOldDats() {
		// Arrange
		var oldDat = DatFile.Create("old.dat", "Old DAT");
		oldDat.UpdatedAt = DateTime.UtcNow.AddDays(-45);

		var recentDat = DatFile.Create("recent.dat", "Recent DAT");
		recentDat.UpdatedAt = DateTime.UtcNow.AddDays(-10);

		_mockDatFileRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync([oldDat, recentDat]);

		// Act
		var outdated = await _service.GetOutdatedDatsAsync();

		// Assert
		Assert.Single(outdated);
		Assert.Equal("old.dat", outdated[0].FileName);
	}

	[Fact]
	public async Task GetOutdatedDatsAsync_WithCustomAge_RespectsParameter() {
		// Arrange
		var dat = DatFile.Create("test.dat", "Test DAT");
		dat.UpdatedAt = DateTime.UtcNow.AddDays(-5);

		_mockDatFileRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync([dat]);

		// Act
		var outdated = await _service.GetOutdatedDatsAsync(TimeSpan.FromDays(3));

		// Assert
		Assert.Single(outdated); // DAT is 5 days old, threshold is 3 days
	}

	[Fact]
	public async Task GetOutdatedDatsAsync_WithNullUpdatedAt_IncludesInResult() {
		// Arrange
		var dat = DatFile.Create("never-updated.dat", "Never Updated");
		// UpdatedAt defaults to null

		_mockDatFileRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync([dat]);

		// Act
		var outdated = await _service.GetOutdatedDatsAsync();

		// Assert
		Assert.Single(outdated);
	}

	[Fact]
	public async Task GetAvailableProvidersAsync_ReturnsRegisteredProviders() {
		// Act
		var providers = await _service.GetAvailableProvidersAsync();

		// Assert
		Assert.Single(providers);
		Assert.Contains(DatProvider.NoIntro, providers);
	}
}
