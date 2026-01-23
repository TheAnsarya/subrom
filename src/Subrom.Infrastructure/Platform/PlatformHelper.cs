namespace Subrom.Infrastructure.Platform;

/// <summary>
/// Cross-platform helper utilities for directory resolution and OS detection.
/// </summary>
public static class PlatformHelper {
	/// <summary>
	/// Gets whether the current OS is Windows.
	/// </summary>
	public static bool IsWindows => OperatingSystem.IsWindows();

	/// <summary>
	/// Gets whether the current OS is macOS.
	/// </summary>
	public static bool IsMacOS => OperatingSystem.IsMacOS();

	/// <summary>
	/// Gets whether the current OS is Linux.
	/// </summary>
	public static bool IsLinux => OperatingSystem.IsLinux();

	/// <summary>
	/// Gets the current platform name.
	/// </summary>
	public static string PlatformName {
		get {
			if (IsWindows) return "Windows";
			if (IsMacOS) return "macOS";
			if (IsLinux) return "Linux";
			return "Unknown";
		}
	}

	/// <summary>
	/// Gets the application data directory for Subrom based on the current platform.
	/// </summary>
	/// <remarks>
	/// Windows: %LOCALAPPDATA%\Subrom (e.g., C:\Users\name\AppData\Local\Subrom)
	/// macOS: ~/Library/Application Support/Subrom
	/// Linux: ~/.config/subrom
	/// </remarks>
	public static string GetDataDirectory() {
		if (IsWindows) {
			return Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				"Subrom");
		}

		if (IsMacOS) {
			return Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
				"Library",
				"Application Support",
				"Subrom");
		}

		// Linux and others: XDG Base Directory Specification
		var xdgConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
		if (!string.IsNullOrEmpty(xdgConfigHome)) {
			return Path.Combine(xdgConfigHome, "subrom");
		}

		return Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
			".config",
			"subrom");
	}

	/// <summary>
	/// Gets the log directory for Subrom based on the current platform.
	/// </summary>
	/// <remarks>
	/// Windows: %LOCALAPPDATA%\Subrom\logs
	/// macOS: ~/Library/Logs/Subrom
	/// Linux: ~/.config/subrom/logs (or $XDG_STATE_HOME/subrom/logs)
	/// </remarks>
	public static string GetLogDirectory() {
		if (IsWindows) {
			return Path.Combine(GetDataDirectory(), "logs");
		}

		if (IsMacOS) {
			return Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
				"Library",
				"Logs",
				"Subrom");
		}

		// Linux: XDG_STATE_HOME or fallback to config directory
		var xdgStateHome = Environment.GetEnvironmentVariable("XDG_STATE_HOME");
		if (!string.IsNullOrEmpty(xdgStateHome)) {
			return Path.Combine(xdgStateHome, "subrom", "logs");
		}

		return Path.Combine(GetDataDirectory(), "logs");
	}

	/// <summary>
	/// Gets the default database file path for Subrom.
	/// </summary>
	public static string GetDefaultDatabasePath() {
		return Path.Combine(GetDataDirectory(), "subrom.db");
	}

	/// <summary>
	/// Gets the default configuration file path for Subrom.
	/// </summary>
	public static string GetDefaultConfigPath() {
		return Path.Combine(GetDataDirectory(), "appsettings.json");
	}

	/// <summary>
	/// Ensures the data directory exists.
	/// </summary>
	public static void EnsureDataDirectoryExists() {
		var dataDir = GetDataDirectory();
		if (!Directory.Exists(dataDir)) {
			Directory.CreateDirectory(dataDir);
		}
	}

	/// <summary>
	/// Ensures the log directory exists.
	/// </summary>
	public static void EnsureLogDirectoryExists() {
		var logDir = GetLogDirectory();
		if (!Directory.Exists(logDir)) {
			Directory.CreateDirectory(logDir);
		}
	}

	/// <summary>
	/// Gets a platform-appropriate path separator.
	/// </summary>
	public static char PathSeparator => Path.DirectorySeparatorChar;

	/// <summary>
	/// Normalizes a path for the current platform.
	/// </summary>
	public static string NormalizePath(string path) {
		if (string.IsNullOrEmpty(path)) {
			return path;
		}

		// Replace wrong separators
		if (IsWindows) {
			return path.Replace('/', '\\');
		}

		return path.Replace('\\', '/');
	}
}
