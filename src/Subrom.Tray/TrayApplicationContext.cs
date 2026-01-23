using System.ComponentModel;
using Microsoft.Extensions.Logging;

namespace Subrom.Tray;

/// <summary>
/// System tray application context with NotifyIcon and context menu.
/// </summary>
public sealed class TrayApplicationContext : ApplicationContext {
	private readonly NotifyIcon _notifyIcon;
	private readonly ServerManager _serverManager;
	private readonly TraySettings _settings;
	private readonly ILogger<TrayApplicationContext> _logger;
	private readonly ContextMenuStrip _contextMenu;

	// Menu items that need state updates
	private readonly ToolStripMenuItem _startMenuItem;
	private readonly ToolStripMenuItem _stopMenuItem;
	private readonly ToolStripMenuItem _restartMenuItem;
	private readonly ToolStripMenuItem _statusMenuItem;

	// Single instance mutex
	private static Mutex? _instanceMutex;
	private const string MutexName = "Global\\SubromTray_SingleInstance";

	public TrayApplicationContext(
		ServerManager serverManager,
		TraySettings settings,
		ILogger<TrayApplicationContext> logger) {
		_serverManager = serverManager;
		_settings = settings;
		_logger = logger;

		// Create context menu
		_contextMenu = new ContextMenuStrip();

		_statusMenuItem = new ToolStripMenuItem("Status: Stopped") { Enabled = false };
		_startMenuItem = new ToolStripMenuItem("Start Server", null, OnStartClick);
		_stopMenuItem = new ToolStripMenuItem("Stop Server", null, OnStopClick) { Enabled = false };
		_restartMenuItem = new ToolStripMenuItem("Restart Server", null, OnRestartClick) { Enabled = false };

		_contextMenu.Items.AddRange([
			_statusMenuItem,
			new ToolStripSeparator(),
			_startMenuItem,
			_stopMenuItem,
			_restartMenuItem,
			new ToolStripSeparator(),
			new ToolStripMenuItem("Open Web UI", null, OnOpenWebUiClick),
			new ToolStripMenuItem("View Logs", null, OnViewLogsClick),
			new ToolStripSeparator(),
			new ToolStripMenuItem("Settings...", null, OnSettingsClick),
			new ToolStripMenuItem("About", null, OnAboutClick),
			new ToolStripSeparator(),
			new ToolStripMenuItem("Exit", null, OnExitClick)
		]);

		// Create notify icon
		_notifyIcon = new NotifyIcon {
			Icon = LoadIcon(),
			Text = "Subrom ROM Manager",
			ContextMenuStrip = _contextMenu,
			Visible = true
		};

		_notifyIcon.DoubleClick += OnNotifyIconDoubleClick;

		// Subscribe to server state changes
		_serverManager.StateChanged += OnServerStateChanged;

		// Auto-start server if configured
		if (_settings.AutoStartServer) {
			_ = StartServerAsync();
		}
	}

	/// <summary>
	/// Ensures only one instance of the application is running.
	/// </summary>
	public static bool EnsureSingleInstance() {
		_instanceMutex = new Mutex(true, MutexName, out var createdNew);
		if (!createdNew) {
			_instanceMutex.Dispose();
			_instanceMutex = null;
			return false;
		}

		return true;
	}

	private static Icon LoadIcon() {
		// Try to load from resources, fall back to default
		try {
			var iconPath = Path.Combine(AppContext.BaseDirectory, "Resources", "subrom.ico");
			if (File.Exists(iconPath)) {
				return new Icon(iconPath);
			}
		} catch { }

		// Use system default application icon
		return SystemIcons.Application;
	}

	private async Task StartServerAsync() {
		try {
			await _serverManager.StartAsync();
			if (_settings.ShowNotifications) {
				ShowNotification("Server Started", "Subrom server is now running", ToolTipIcon.Info);
			}
		} catch (Exception ex) {
			_logger.LogError(ex, "Failed to start server");
			ShowNotification("Server Error", $"Failed to start: {ex.Message}", ToolTipIcon.Error);
		}
	}

	private void OnServerStateChanged(object? sender, ServerState state) {
		// Update UI on UI thread
		if (_contextMenu.InvokeRequired) {
			_contextMenu.BeginInvoke(() => UpdateUiForState(state));
		} else {
			UpdateUiForState(state);
		}
	}

	private void UpdateUiForState(ServerState state) {
		_statusMenuItem.Text = $"Status: {state}";

		switch (state) {
			case ServerState.Stopped:
				_notifyIcon.Icon = LoadIcon();
				_notifyIcon.Text = "Subrom - Stopped";
				_startMenuItem.Enabled = true;
				_stopMenuItem.Enabled = false;
				_restartMenuItem.Enabled = false;
				break;

			case ServerState.Starting:
				_notifyIcon.Text = "Subrom - Starting...";
				_startMenuItem.Enabled = false;
				_stopMenuItem.Enabled = false;
				_restartMenuItem.Enabled = false;
				break;

			case ServerState.Running:
				_notifyIcon.Text = "Subrom - Running";
				_startMenuItem.Enabled = false;
				_stopMenuItem.Enabled = true;
				_restartMenuItem.Enabled = true;
				break;

			case ServerState.Stopping:
				_notifyIcon.Text = "Subrom - Stopping...";
				_startMenuItem.Enabled = false;
				_stopMenuItem.Enabled = false;
				_restartMenuItem.Enabled = false;
				break;

			case ServerState.Error:
				_notifyIcon.Text = "Subrom - Error";
				_startMenuItem.Enabled = true;
				_stopMenuItem.Enabled = false;
				_restartMenuItem.Enabled = false;
				if (_settings.ShowNotifications) {
					ShowNotification("Server Error", "The server has stopped unexpectedly", ToolTipIcon.Error);
				}

				break;
		}
	}

	private void ShowNotification(string title, string message, ToolTipIcon icon) {
		_notifyIcon.ShowBalloonTip(3000, title, message, icon);
	}

	private async void OnStartClick(object? sender, EventArgs e) {
		await StartServerAsync();
	}

	private async void OnStopClick(object? sender, EventArgs e) {
		try {
			await _serverManager.StopAsync();
			if (_settings.ShowNotifications) {
				ShowNotification("Server Stopped", "Subrom server has been stopped", ToolTipIcon.Info);
			}
		} catch (Exception ex) {
			_logger.LogError(ex, "Failed to stop server");
			ShowNotification("Error", $"Failed to stop server: {ex.Message}", ToolTipIcon.Error);
		}
	}

	private async void OnRestartClick(object? sender, EventArgs e) {
		try {
			await _serverManager.RestartAsync();
			if (_settings.ShowNotifications) {
				ShowNotification("Server Restarted", "Subrom server has been restarted", ToolTipIcon.Info);
			}
		} catch (Exception ex) {
			_logger.LogError(ex, "Failed to restart server");
			ShowNotification("Error", $"Failed to restart server: {ex.Message}", ToolTipIcon.Error);
		}
	}

	private void OnOpenWebUiClick(object? sender, EventArgs e) {
		try {
			var url = _settings.ServerUrl;
			System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
				FileName = url,
				UseShellExecute = true
			});
		} catch (Exception ex) {
			_logger.LogError(ex, "Failed to open browser");
			MessageBox.Show($"Failed to open browser: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
	}

	private void OnViewLogsClick(object? sender, EventArgs e) {
		try {
			var logPath = Path.Combine(AppContext.BaseDirectory, _settings.Logging.LogPath);
			if (Directory.Exists(logPath)) {
				System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
					FileName = logPath,
					UseShellExecute = true
				});
			} else {
				MessageBox.Show("Log directory does not exist yet.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
		} catch (Exception ex) {
			_logger.LogError(ex, "Failed to open logs folder");
		}
	}

	private void OnSettingsClick(object? sender, EventArgs e) {
		using var settingsForm = new SettingsForm(_settings);
		if (settingsForm.ShowDialog() == DialogResult.OK) {
			// Settings updated, save them
			SaveSettings();
		}
	}

	private void OnAboutClick(object? sender, EventArgs e) {
		var version = typeof(TrayApplicationContext).Assembly.GetName().Version?.ToString() ?? "Unknown";
		MessageBox.Show(
			$"Subrom ROM Manager\nVersion: {version}\n\nA ROM management and verification toolkit.",
			"About Subrom",
			MessageBoxButtons.OK,
			MessageBoxIcon.Information);
	}

	private async void OnExitClick(object? sender, EventArgs e) {
		await ExitApplicationAsync();
	}

	private void OnNotifyIconDoubleClick(object? sender, EventArgs e) {
		OnOpenWebUiClick(sender, e);
	}

	private async Task ExitApplicationAsync() {
		try {
			if (_serverManager.State == ServerState.Running) {
				await _serverManager.StopAsync();
			}
		} catch (Exception ex) {
			_logger.LogError(ex, "Error stopping server during exit");
		}

		_notifyIcon.Visible = false;
		_instanceMutex?.ReleaseMutex();
		_instanceMutex?.Dispose();
		Application.Exit();
	}

	private void SaveSettings() {
		try {
			var json = System.Text.Json.JsonSerializer.Serialize(_settings, new System.Text.Json.JsonSerializerOptions {
				WriteIndented = true
			});
			var settingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
			File.WriteAllText(settingsPath, json);
			_logger.LogInformation("Settings saved");
		} catch (Exception ex) {
			_logger.LogError(ex, "Failed to save settings");
		}
	}

	protected override void Dispose(bool disposing) {
		if (disposing) {
			_serverManager.StateChanged -= OnServerStateChanged;
			_serverManager.Dispose();
			_notifyIcon.Dispose();
			_contextMenu.Dispose();
		}

		base.Dispose(disposing);
	}
}
