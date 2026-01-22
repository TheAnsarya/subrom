using Moq;
using Subrom.Application.Interfaces;
using Subrom.Application.Services;
using Subrom.Domain.Aggregates.Settings;

namespace Subrom.Tests.Unit.Application;

/// <summary>
/// Unit tests for SettingsService.
/// </summary>
public class SettingsServiceTests {
	private readonly Mock<ISettingsRepository> _mockSettingsRepo;
	private readonly Mock<IUnitOfWork> _mockUnitOfWork;
	private readonly SettingsService _service;
	private AppSettings _testSettings;

	public SettingsServiceTests() {
		_mockSettingsRepo = new Mock<ISettingsRepository>();
		_mockUnitOfWork = new Mock<IUnitOfWork>();
		_testSettings = new AppSettings();

		_mockSettingsRepo
			.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(() => _testSettings);

		_service = new SettingsService(_mockSettingsRepo.Object, _mockUnitOfWork.Object);
	}

	[Fact]
	public async Task GetAsync_ReturnsSettings() {
		// Act
		var settings = await _service.GetAsync();

		// Assert
		Assert.NotNull(settings);
		_mockSettingsRepo.Verify(r => r.GetAsync(It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task UpdateAsync_ModifiesAndSavesSettings() {
		// Act
		var settings = await _service.UpdateAsync(s => {
			s.ScanningParallelThreads = 8;
		});

		// Assert
		Assert.Equal(8, settings.ScanningParallelThreads);
		_mockSettingsRepo.Verify(r => r.SaveAsync(It.IsAny<AppSettings>(), It.IsAny<CancellationToken>()), Times.Once);
		_mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task UpdateAsync_UpdatesLastModified() {
		// Arrange
		var beforeUpdate = DateTime.UtcNow.AddMinutes(-1);
		_testSettings.LastModified = beforeUpdate;

		// Act
		var settings = await _service.UpdateAsync(s => {
			s.UiTheme = "light";
		});

		// Assert
		Assert.True(settings.LastModified > beforeUpdate);
	}

	[Fact]
	public async Task UpdateScanningSettingsAsync_UpdatesOnlySpecifiedSettings() {
		// Arrange
		_testSettings.ScanningParallelThreads = 4;
		_testSettings.ScanningSkipHiddenFiles = true;
		_testSettings.ScanningScanArchives = true;

		// Act
		var settings = await _service.UpdateScanningSettingsAsync(
			parallelThreads: 8,
			skipHiddenFiles: false);

		// Assert
		Assert.Equal(8, settings.ScanningParallelThreads);
		Assert.False(settings.ScanningSkipHiddenFiles);
		Assert.True(settings.ScanningScanArchives); // unchanged
	}

	[Fact]
	public async Task UpdateScanningSettingsAsync_ClampsParallelThreads() {
		// Act - should clamp to max
		var settings = await _service.UpdateScanningSettingsAsync(parallelThreads: 100);

		// Assert
		Assert.True(settings.ScanningParallelThreads <= Environment.ProcessorCount * 2);
		Assert.True(settings.ScanningParallelThreads >= 1);

		// Act - should clamp to min
		settings = await _service.UpdateScanningSettingsAsync(parallelThreads: 0);
		Assert.Equal(1, settings.ScanningParallelThreads);
	}

	[Fact]
	public async Task UpdateOrganizationSettingsAsync_UpdatesRegionPriority() {
		// Act
		var settings = await _service.UpdateOrganizationSettingsAsync(
			regionPriority: new[] { "Japan", "USA", "Europe" });

		// Assert
		var regions = settings.GetRegionPriority();
		Assert.Equal(3, regions.Count);
		Assert.Equal("Japan", regions[0]);
		Assert.Equal("USA", regions[1]);
		Assert.Equal("Europe", regions[2]);
	}

	[Fact]
	public async Task UpdateOrganizationSettingsAsync_UpdatesLanguagePriority() {
		// Act
		var settings = await _service.UpdateOrganizationSettingsAsync(
			languagePriority: new[] { "Ja", "En", "De" });

		// Assert
		var languages = settings.GetLanguagePriority();
		Assert.Equal(3, languages.Count);
		Assert.Equal("Ja", languages[0]);
	}

	[Fact]
	public async Task UpdateOrganizationSettingsAsync_UpdatesDefaultTemplate() {
		// Act
		var settings = await _service.UpdateOrganizationSettingsAsync(
			defaultTemplate: "custom-template");

		// Assert
		Assert.Equal("custom-template", settings.OrganizationDefaultTemplate);
	}

	[Fact]
	public async Task UpdateUiSettingsAsync_UpdatesTheme() {
		// Act
		var settings = await _service.UpdateUiSettingsAsync(theme: "light");

		// Assert
		Assert.Equal("light", settings.UiTheme);
	}

	[Fact]
	public async Task UpdateUiSettingsAsync_RejectsInvalidTheme() {
		// Arrange
		_testSettings.UiTheme = "dark";

		// Act
		var settings = await _service.UpdateUiSettingsAsync(theme: "invalid");

		// Assert - should remain unchanged for invalid theme
		Assert.Equal("dark", settings.UiTheme);
	}

	[Fact]
	public async Task UpdateUiSettingsAsync_ClampsPageSize() {
		// Act - should clamp to min
		var settings = await _service.UpdateUiSettingsAsync(pageSize: 1);
		Assert.Equal(10, settings.UiPageSize);

		// Act - should clamp to max
		settings = await _service.UpdateUiSettingsAsync(pageSize: 5000);
		Assert.Equal(1000, settings.UiPageSize);

		// Act - should allow valid
		settings = await _service.UpdateUiSettingsAsync(pageSize: 50);
		Assert.Equal(50, settings.UiPageSize);
	}

	[Fact]
	public async Task UpdateStorageSettingsAsync_UpdatesLowSpaceWarning() {
		// Act
		var settings = await _service.UpdateStorageSettingsAsync(lowSpaceWarningMb: 2048);

		// Assert
		Assert.Equal(2048, settings.StorageLowSpaceWarningMb);
	}

	[Fact]
	public async Task UpdateStorageSettingsAsync_ClampsNegativeValue() {
		// Act
		var settings = await _service.UpdateStorageSettingsAsync(lowSpaceWarningMb: -100);

		// Assert
		Assert.Equal(0, settings.StorageLowSpaceWarningMb);
	}

	[Fact]
	public async Task ResetToDefaultsAsync_ResetsAllSettings() {
		// Arrange
		_testSettings.ScanningParallelThreads = 16;
		_testSettings.UiTheme = "light";
		_testSettings.OrganizationUse1G1R = true;

		// Act
		var settings = await _service.ResetToDefaultsAsync();

		// Assert - should be new default settings
		Assert.Equal(4, settings.ScanningParallelThreads);
		Assert.Equal("dark", settings.UiTheme);
		Assert.False(settings.OrganizationUse1G1R);
		_mockSettingsRepo.Verify(r => r.SaveAsync(It.IsAny<AppSettings>(), It.IsAny<CancellationToken>()), Times.Once);
	}
}
