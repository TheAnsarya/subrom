namespace Subrom.Tray;

/// <summary>
/// Settings dialog form.
/// </summary>
public partial class SettingsForm : Form {
	private readonly TraySettings _settings;
	private TextBox _serverUrlTextBox = null!;
	private TextBox _serverPathTextBox = null!;
	private CheckBox _autoStartCheckBox = null!;
	private CheckBox _minimizeToTrayCheckBox = null!;
	private CheckBox _startWithWindowsCheckBox = null!;
	private CheckBox _showNotificationsCheckBox = null!;
	private NumericUpDown _retainDaysNumeric = null!;
	private NumericUpDown _fileSizeNumeric = null!;

	public SettingsForm(TraySettings settings) {
		_settings = settings;
		InitializeComponent();
		LoadSettings();
	}

	private void InitializeComponent() {
		Text = "Subrom Settings";
		Size = new Size(450, 400);
		StartPosition = FormStartPosition.CenterParent;
		FormBorderStyle = FormBorderStyle.FixedDialog;
		MaximizeBox = false;
		MinimizeBox = false;

		var tabControl = new TabControl {
			Location = new Point(10, 10),
			Size = new Size(410, 300),
			Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
		};

		// General tab
		var generalTab = new TabPage("General");
		var y = 20;

		generalTab.Controls.Add(new Label { Text = "Server URL:", Location = new Point(10, y), AutoSize = true });
		_serverUrlTextBox = new TextBox { Location = new Point(120, y - 3), Width = 260 };
		generalTab.Controls.Add(_serverUrlTextBox);
		y += 30;

		generalTab.Controls.Add(new Label { Text = "Server Path:", Location = new Point(10, y), AutoSize = true });
		_serverPathTextBox = new TextBox { Location = new Point(120, y - 3), Width = 200 };
		generalTab.Controls.Add(_serverPathTextBox);
		var browseButton = new Button { Text = "...", Location = new Point(325, y - 4), Width = 55 };
		browseButton.Click += OnBrowseServerPath;
		generalTab.Controls.Add(browseButton);
		y += 35;

		_autoStartCheckBox = new CheckBox { Text = "Auto-start server", Location = new Point(10, y), AutoSize = true };
		generalTab.Controls.Add(_autoStartCheckBox);
		y += 25;

		_minimizeToTrayCheckBox = new CheckBox { Text = "Minimize to tray on close", Location = new Point(10, y), AutoSize = true };
		generalTab.Controls.Add(_minimizeToTrayCheckBox);
		y += 25;

		_startWithWindowsCheckBox = new CheckBox { Text = "Start with Windows", Location = new Point(10, y), AutoSize = true };
		generalTab.Controls.Add(_startWithWindowsCheckBox);
		y += 25;

		_showNotificationsCheckBox = new CheckBox { Text = "Show notifications", Location = new Point(10, y), AutoSize = true };
		generalTab.Controls.Add(_showNotificationsCheckBox);

		tabControl.TabPages.Add(generalTab);

		// Logging tab
		var loggingTab = new TabPage("Logging");
		y = 20;

		loggingTab.Controls.Add(new Label { Text = "Retain log files (days):", Location = new Point(10, y), AutoSize = true });
		_retainDaysNumeric = new NumericUpDown {
			Location = new Point(150, y - 3),
			Width = 80,
			Minimum = 1,
			Maximum = 365
		};
		loggingTab.Controls.Add(_retainDaysNumeric);
		y += 30;

		loggingTab.Controls.Add(new Label { Text = "Max file size (MB):", Location = new Point(10, y), AutoSize = true });
		_fileSizeNumeric = new NumericUpDown {
			Location = new Point(150, y - 3),
			Width = 80,
			Minimum = 1,
			Maximum = 1000
		};
		loggingTab.Controls.Add(_fileSizeNumeric);

		tabControl.TabPages.Add(loggingTab);

		Controls.Add(tabControl);

		// Buttons
		var okButton = new Button {
			Text = "OK",
			Location = new Point(250, 320),
			Size = new Size(75, 25),
			DialogResult = DialogResult.OK
		};
		okButton.Click += OnOkClick;
		Controls.Add(okButton);

		var cancelButton = new Button {
			Text = "Cancel",
			Location = new Point(335, 320),
			Size = new Size(75, 25),
			DialogResult = DialogResult.Cancel
		};
		Controls.Add(cancelButton);

		AcceptButton = okButton;
		CancelButton = cancelButton;
	}

	private void LoadSettings() {
		_serverUrlTextBox.Text = _settings.ServerUrl;
		_serverPathTextBox.Text = _settings.ServerPath;
		_autoStartCheckBox.Checked = _settings.AutoStartServer;
		_minimizeToTrayCheckBox.Checked = _settings.MinimizeToTray;
		_startWithWindowsCheckBox.Checked = _settings.StartWithWindows;
		_showNotificationsCheckBox.Checked = _settings.ShowNotifications;
		_retainDaysNumeric.Value = _settings.Logging.RetainedFileCountLimit;
		_fileSizeNumeric.Value = _settings.Logging.FileSizeLimitMb;
	}

	private void SaveSettings() {
		_settings.ServerUrl = _serverUrlTextBox.Text.Trim();
		_settings.ServerPath = _serverPathTextBox.Text.Trim();
		_settings.AutoStartServer = _autoStartCheckBox.Checked;
		_settings.MinimizeToTray = _minimizeToTrayCheckBox.Checked;
		_settings.StartWithWindows = _startWithWindowsCheckBox.Checked;
		_settings.ShowNotifications = _showNotificationsCheckBox.Checked;
		_settings.Logging.RetainedFileCountLimit = (int)_retainDaysNumeric.Value;
		_settings.Logging.FileSizeLimitMb = (int)_fileSizeNumeric.Value;

		// Update Windows startup
		UpdateStartupRegistry();
	}

	private void UpdateStartupRegistry() {
		try {
			using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
				@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);

			if (key is null) return;

			if (_settings.StartWithWindows) {
				var exePath = Application.ExecutablePath;
				key.SetValue("SubromTray", $"\"{exePath}\"");
			} else {
				key.DeleteValue("SubromTray", false);
			}
		} catch (Exception) {
			// Registry access may fail without admin rights
		}
	}

	private void OnBrowseServerPath(object? sender, EventArgs e) {
		using var dialog = new OpenFileDialog {
			Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
			Title = "Select Subrom Server Executable"
		};

		if (dialog.ShowDialog() == DialogResult.OK) {
			_serverPathTextBox.Text = dialog.FileName;
		}
	}

	private void OnOkClick(object? sender, EventArgs e) {
		SaveSettings();
	}
}
