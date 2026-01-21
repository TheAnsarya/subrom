namespace Subrom.Tray;

/// <summary>
/// Application settings for the tray application.
/// </summary>
public class TraySettings {
	/// <summary>
	/// URL of the Subrom server.
	/// </summary>
	public string ServerUrl { get; set; } = "http://localhost:52100";

	/// <summary>
	/// Path to the Subrom.Server executable.
	/// </summary>
	public string ServerPath { get; set; } = "";

	/// <summary>
	/// Whether to start the server automatically when the tray app starts.
	/// </summary>
	public bool AutoStartServer { get; set; } = true;

	/// <summary>
	/// Whether to minimize to tray on close.
	/// </summary>
	public bool MinimizeToTray { get; set; } = true;

	/// <summary>
	/// Whether to start with Windows.
	/// </summary>
	public bool StartWithWindows { get; set; } = false;

	/// <summary>
	/// Whether to show notifications.
	/// </summary>
	public bool ShowNotifications { get; set; } = true;

	/// <summary>
	/// Logging configuration.
	/// </summary>
	public LoggingSettings Logging { get; set; } = new();
}

/// <summary>
/// Logging settings.
/// </summary>
public class LoggingSettings {
	/// <summary>
	/// Path to log files directory.
	/// </summary>
	public string LogPath { get; set; } = "logs";

	/// <summary>
	/// Rolling interval for log files.
	/// </summary>
	public string RollingInterval { get; set; } = "Day";

	/// <summary>
	/// Number of days to retain log files.
	/// </summary>
	public int RetainedFileCountLimit { get; set; } = 7;

	/// <summary>
	/// Maximum log file size in MB.
	/// </summary>
	public int FileSizeLimitMb { get; set; } = 10;
}
