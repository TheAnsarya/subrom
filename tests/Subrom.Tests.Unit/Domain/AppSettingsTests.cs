using Subrom.Domain.Aggregates.Settings;

namespace Subrom.Tests.Unit.Domain;

/// <summary>
/// Unit tests for AppSettings entity.
/// </summary>
public class AppSettingsTests {
	[Fact]
	public void Constructor_SetsDefaultValues() {
		// Act
		var settings = new AppSettings();

		// Assert - Scanning defaults
		Assert.Equal(4, settings.ScanningParallelThreads);
		Assert.True(settings.ScanningSkipHiddenFiles);
		Assert.True(settings.ScanningScanArchives);
		Assert.True(settings.ScanningCalculateMd5);
		Assert.True(settings.ScanningCalculateSha1);
		Assert.True(settings.ScanningDetectHeaders);

		// Assert - Organization defaults
		Assert.Equal("system-game", settings.OrganizationDefaultTemplate);
		Assert.False(settings.OrganizationUse1G1R);
		Assert.True(settings.OrganizationPreferParent);

		// Assert - UI defaults
		Assert.Equal("dark", settings.UiTheme);
		Assert.Equal(100, settings.UiPageSize);
		Assert.True(settings.UiShowHumanSizes);

		// Assert - Storage defaults
		Assert.Equal(1024, settings.StorageLowSpaceWarningMb);
		Assert.True(settings.StorageMonitorDrives);
	}

	[Fact]
	public void SingletonId_IsOne() {
		Assert.Equal(1, AppSettings.SingletonId);
	}

	[Fact]
	public void GetRegionPriority_ReturnsListFromCommaSeparatedString() {
		// Arrange
		var settings = new AppSettings {
			OrganizationRegionPriority = "USA,Japan,Europe"
		};

		// Act
		var regions = settings.GetRegionPriority();

		// Assert
		Assert.Equal(3, regions.Count);
		Assert.Equal("USA", regions[0]);
		Assert.Equal("Japan", regions[1]);
		Assert.Equal("Europe", regions[2]);
	}

	[Fact]
	public void GetRegionPriority_TrimsWhitespace() {
		// Arrange
		var settings = new AppSettings {
			OrganizationRegionPriority = " USA , Japan , Europe "
		};

		// Act
		var regions = settings.GetRegionPriority();

		// Assert
		Assert.Equal("USA", regions[0]);
		Assert.Equal("Japan", regions[1]);
		Assert.Equal("Europe", regions[2]);
	}

	[Fact]
	public void GetRegionPriority_HandlesEmptyEntries() {
		// Arrange
		var settings = new AppSettings {
			OrganizationRegionPriority = "USA,,Japan,,"
		};

		// Act
		var regions = settings.GetRegionPriority();

		// Assert
		Assert.Equal(2, regions.Count);
		Assert.Equal("USA", regions[0]);
		Assert.Equal("Japan", regions[1]);
	}

	[Fact]
	public void GetRegionPriority_HandlesEmptyString() {
		// Arrange
		var settings = new AppSettings {
			OrganizationRegionPriority = ""
		};

		// Act
		var regions = settings.GetRegionPriority();

		// Assert
		Assert.Empty(regions);
	}

	[Fact]
	public void SetRegionPriority_JoinsListWithCommas() {
		// Arrange
		var settings = new AppSettings();
		var regions = new[] { "Japan", "USA", "Europe" };

		// Act
		settings.SetRegionPriority(regions);

		// Assert
		Assert.Equal("Japan,USA,Europe", settings.OrganizationRegionPriority);
	}

	[Fact]
	public void SetRegionPriority_HandlesEmptyList() {
		// Arrange
		var settings = new AppSettings();

		// Act
		settings.SetRegionPriority(Array.Empty<string>());

		// Assert
		Assert.Equal("", settings.OrganizationRegionPriority);
	}

	[Fact]
	public void GetLanguagePriority_ReturnsListFromCommaSeparatedString() {
		// Arrange
		var settings = new AppSettings {
			OrganizationLanguagePriority = "En,Ja,De"
		};

		// Act
		var languages = settings.GetLanguagePriority();

		// Assert
		Assert.Equal(3, languages.Count);
		Assert.Equal("En", languages[0]);
		Assert.Equal("Ja", languages[1]);
		Assert.Equal("De", languages[2]);
	}

	[Fact]
	public void SetLanguagePriority_JoinsListWithCommas() {
		// Arrange
		var settings = new AppSettings();
		var languages = new[] { "Ja", "En" };

		// Act
		settings.SetLanguagePriority(languages);

		// Assert
		Assert.Equal("Ja,En", settings.OrganizationLanguagePriority);
	}

	[Fact]
	public void DefaultRegionPriority_ContainsExpectedRegions() {
		// Arrange
		var settings = new AppSettings();

		// Act
		var regions = settings.GetRegionPriority();

		// Assert
		Assert.Contains("USA", regions);
		Assert.Contains("Europe", regions);
		Assert.Contains("Japan", regions);
		Assert.Contains("World", regions);
	}

	[Fact]
	public void DefaultLanguagePriority_ContainsExpectedLanguages() {
		// Arrange
		var settings = new AppSettings();

		// Act
		var languages = settings.GetLanguagePriority();

		// Assert
		Assert.Contains("En", languages);
		Assert.Contains("Ja", languages);
		Assert.Contains("De", languages);
		Assert.Contains("Fr", languages);
	}

	[Fact]
	public void LastModified_CanBeSet() {
		// Arrange
		var settings = new AppSettings();
		var timestamp = new DateTime(2026, 1, 22, 12, 0, 0, DateTimeKind.Utc);

		// Act
		settings.LastModified = timestamp;

		// Assert
		Assert.Equal(timestamp, settings.LastModified);
	}
}
