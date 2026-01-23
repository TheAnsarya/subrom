using Subrom.Infrastructure.Platform;

namespace Subrom.Tests.Unit.Infrastructure;

/// <summary>
/// Tests for the PlatformHelper cross-platform utility class.
/// </summary>
public sealed class PlatformHelperTests {
	[Fact]
	public void PlatformName_ReturnsValidPlatform() {
		// Act
		var platformName = PlatformHelper.PlatformName;

		// Assert
		Assert.Contains(platformName, new[] { "Windows", "Linux", "macOS", "Unknown" });
	}

	[Fact]
	public void IsWindows_Or_IsLinux_Or_IsMacOS_AtLeastOneIsTrue() {
		// Act & Assert
		var isKnownPlatform = PlatformHelper.IsWindows ||
							 PlatformHelper.IsLinux ||
							 PlatformHelper.IsMacOS;

		Assert.True(isKnownPlatform, "At least one platform detection should return true");
	}

	[Fact]
	public void GetDataDirectory_ReturnsNonEmptyPath() {
		// Act
		var dataDir = PlatformHelper.GetDataDirectory();

		// Assert
		Assert.False(string.IsNullOrWhiteSpace(dataDir));
	}

	[Fact]
	public void GetDataDirectory_ContainsSubromFolder() {
		// Act
		var dataDir = PlatformHelper.GetDataDirectory().ToLowerInvariant();

		// Assert
		Assert.Contains("subrom", dataDir);
	}

	[Fact]
	public void GetLogDirectory_ReturnsNonEmptyPath() {
		// Act
		var logDir = PlatformHelper.GetLogDirectory();

		// Assert
		Assert.False(string.IsNullOrWhiteSpace(logDir));
	}

	[Fact]
	public void GetDefaultDatabasePath_EndsWithSubromDb() {
		// Act
		var dbPath = PlatformHelper.GetDefaultDatabasePath();

		// Assert
		Assert.EndsWith("subrom.db", dbPath);
	}

	[Fact]
	public void GetDefaultConfigPath_EndsWithAppsettingsJson() {
		// Act
		var configPath = PlatformHelper.GetDefaultConfigPath();

		// Assert
		Assert.EndsWith("appsettings.json", configPath);
	}

	[Fact]
	public void PathSeparator_IsValidCharacter() {
		// Act
		var separator = PlatformHelper.PathSeparator;

		// Assert
		Assert.True(separator == '/' || separator == '\\');
	}

	[Fact]
	public void NormalizePath_WithNullInput_ReturnsNull() {
		// Act
		var result = PlatformHelper.NormalizePath(null!);

		// Assert
		Assert.Null(result);
	}

	[Fact]
	public void NormalizePath_WithEmptyInput_ReturnsEmpty() {
		// Act
		var result = PlatformHelper.NormalizePath(string.Empty);

		// Assert
		Assert.Equal(string.Empty, result);
	}

	[Fact]
	public void NormalizePath_NormalizesPathSeparators() {
		// Arrange
		var mixedPath = "some/path\\with/mixed\\separators";

		// Act
		var result = PlatformHelper.NormalizePath(mixedPath);

		// Assert
		// On Windows, should convert / to \
		// On Linux/macOS, should convert \ to /
		if (PlatformHelper.IsWindows) {
			Assert.DoesNotContain("/", result);
		} else {
			Assert.DoesNotContain("\\", result);
		}
	}

	[Fact]
	public void EnsureDataDirectoryExists_CreatesDirectory() {
		// Act
		PlatformHelper.EnsureDataDirectoryExists();

		// Assert
		Assert.True(Directory.Exists(PlatformHelper.GetDataDirectory()));
	}

	[Fact]
	public void EnsureLogDirectoryExists_CreatesDirectory() {
		// Act
		PlatformHelper.EnsureLogDirectoryExists();

		// Assert
		Assert.True(Directory.Exists(PlatformHelper.GetLogDirectory()));
	}

	[Fact]
	public void GetDataDirectory_OnWindows_ContainsAppData() {
		// Skip if not Windows
		if (!PlatformHelper.IsWindows) {
			return;
		}

		// Act
		var dataDir = PlatformHelper.GetDataDirectory();

		// Assert
		Assert.Contains("AppData", dataDir);
	}

	[Fact]
	public void GetDataDirectory_OnLinux_ContainsConfigOrXdg() {
		// Skip if not Linux
		if (!PlatformHelper.IsLinux) {
			return;
		}

		// Act
		var dataDir = PlatformHelper.GetDataDirectory();

		// Assert
		// Should contain .config or respect XDG_CONFIG_HOME
		var xdgConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
		if (!string.IsNullOrEmpty(xdgConfigHome)) {
			Assert.StartsWith(xdgConfigHome, dataDir);
		} else {
			Assert.Contains(".config", dataDir);
		}
	}

	[Fact]
	public void GetDataDirectory_OnMacOS_ContainsApplicationSupport() {
		// Skip if not macOS
		if (!PlatformHelper.IsMacOS) {
			return;
		}

		// Act
		var dataDir = PlatformHelper.GetDataDirectory();

		// Assert
		Assert.Contains("Application Support", dataDir);
	}
}
