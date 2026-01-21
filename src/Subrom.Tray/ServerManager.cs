using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Subrom.Tray;

/// <summary>
/// Server state enumeration.
/// </summary>
public enum ServerState {
	Stopped,
	Starting,
	Running,
	Stopping,
	Error
}

/// <summary>
/// Manages the Subrom server process lifecycle.
/// </summary>
public sealed class ServerManager : IDisposable {
	private readonly TraySettings _settings;
	private readonly ILogger<ServerManager> _logger;
	private Process? _serverProcess;
	private CancellationTokenSource? _healthCheckCts;
	private bool _disposed;

	public ServerState State { get; private set; } = ServerState.Stopped;

	public event EventHandler<ServerState>? StateChanged;
	public event EventHandler<string>? OutputReceived;
	public event EventHandler<string>? ErrorReceived;

	public ServerManager(TraySettings settings, ILogger<ServerManager> logger) {
		_settings = settings;
		_logger = logger;
	}

	/// <summary>
	/// Starts the server process.
	/// </summary>
	public async Task StartAsync(CancellationToken cancellationToken = default) {
		if (State == ServerState.Running || State == ServerState.Starting) {
			_logger.LogWarning("Server is already running or starting");
			return;
		}

		try {
			SetState(ServerState.Starting);

			var serverPath = GetServerPath();
			if (string.IsNullOrEmpty(serverPath) || !File.Exists(serverPath)) {
				throw new FileNotFoundException("Server executable not found", serverPath);
			}

			var startInfo = new ProcessStartInfo {
				FileName = serverPath,
				WorkingDirectory = Path.GetDirectoryName(serverPath),
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
			};

			_serverProcess = new Process { StartInfo = startInfo };
			_serverProcess.OutputDataReceived += OnOutputDataReceived;
			_serverProcess.ErrorDataReceived += OnErrorDataReceived;
			_serverProcess.Exited += OnProcessExited;
			_serverProcess.EnableRaisingEvents = true;

			if (!_serverProcess.Start()) {
				throw new InvalidOperationException("Failed to start server process");
			}

			_serverProcess.BeginOutputReadLine();
			_serverProcess.BeginErrorReadLine();

			_logger.LogInformation("Server process started with PID {ProcessId}", _serverProcess.Id);

			// Start health check monitoring
			_healthCheckCts = new CancellationTokenSource();
			_ = MonitorHealthAsync(_healthCheckCts.Token);

			// Wait for server to respond
			if (await WaitForServerReadyAsync(cancellationToken)) {
				SetState(ServerState.Running);
				_logger.LogInformation("Server is ready at {ServerUrl}", _settings.ServerUrl);
			} else {
				throw new TimeoutException("Server did not respond in time");
			}
		}
		catch (Exception ex) {
			_logger.LogError(ex, "Failed to start server");
			SetState(ServerState.Error);
			throw;
		}
	}

	/// <summary>
	/// Stops the server process.
	/// </summary>
	public async Task StopAsync(CancellationToken cancellationToken = default) {
		if (State == ServerState.Stopped || State == ServerState.Stopping) {
			return;
		}

		try {
			SetState(ServerState.Stopping);

			// Cancel health check
			_healthCheckCts?.Cancel();
			_healthCheckCts?.Dispose();
			_healthCheckCts = null;

			if (_serverProcess is not null && !_serverProcess.HasExited) {
				_logger.LogInformation("Stopping server process");

				// Try graceful shutdown first
				try {
					using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
					await client.PostAsync($"{_settings.ServerUrl}/api/shutdown", null, cancellationToken);
					await Task.Delay(1000, cancellationToken);
				}
				catch {
					// Ignore shutdown endpoint errors
				}

				// Wait for exit or force kill
				if (!_serverProcess.HasExited) {
					var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
					cts.CancelAfter(TimeSpan.FromSeconds(10));

					try {
						await _serverProcess.WaitForExitAsync(cts.Token);
					}
					catch (OperationCanceledException) {
						_logger.LogWarning("Server did not exit gracefully, killing process");
						_serverProcess.Kill(entireProcessTree: true);
					}
				}

				_serverProcess.Dispose();
				_serverProcess = null;
			}

			SetState(ServerState.Stopped);
			_logger.LogInformation("Server stopped");
		}
		catch (Exception ex) {
			_logger.LogError(ex, "Error stopping server");
			SetState(ServerState.Error);
			throw;
		}
	}

	/// <summary>
	/// Restarts the server process.
	/// </summary>
	public async Task RestartAsync(CancellationToken cancellationToken = default) {
		await StopAsync(cancellationToken);
		await Task.Delay(500, cancellationToken);
		await StartAsync(cancellationToken);
	}

	/// <summary>
	/// Gets the server health status.
	/// </summary>
	public async Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default) {
		try {
			using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
			var response = await client.GetAsync($"{_settings.ServerUrl}/health", cancellationToken);
			return response.IsSuccessStatusCode;
		}
		catch {
			return false;
		}
	}

	private string GetServerPath() {
		if (!string.IsNullOrEmpty(_settings.ServerPath) && File.Exists(_settings.ServerPath)) {
			return _settings.ServerPath;
		}

		// Try common locations
		var candidates = new[] {
			Path.Combine(AppContext.BaseDirectory, "Subrom.Server.exe"),
			Path.Combine(AppContext.BaseDirectory, "..", "Subrom.Server", "Subrom.Server.exe"),
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Subrom", "Subrom.Server.exe"),
		};

		return candidates.FirstOrDefault(File.Exists) ?? "";
	}

	private async Task<bool> WaitForServerReadyAsync(CancellationToken cancellationToken) {
		var timeout = TimeSpan.FromSeconds(30);
		var started = DateTime.UtcNow;

		while (DateTime.UtcNow - started < timeout) {
			cancellationToken.ThrowIfCancellationRequested();

			if (await CheckHealthAsync(cancellationToken)) {
				return true;
			}

			await Task.Delay(500, cancellationToken);
		}

		return false;
	}

	private async Task MonitorHealthAsync(CancellationToken cancellationToken) {
		while (!cancellationToken.IsCancellationRequested) {
			try {
				await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);

				if (State == ServerState.Running) {
					var healthy = await CheckHealthAsync(cancellationToken);
					if (!healthy) {
						_logger.LogWarning("Server health check failed");
						SetState(ServerState.Error);
					}
				}
			}
			catch (OperationCanceledException) {
				break;
			}
			catch (Exception ex) {
				_logger.LogError(ex, "Error in health check");
			}
		}
	}

	private void SetState(ServerState state) {
		if (State != state) {
			var oldState = State;
			State = state;
			_logger.LogDebug("Server state changed: {OldState} -> {NewState}", oldState, state);
			StateChanged?.Invoke(this, state);
		}
	}

	private void OnOutputDataReceived(object sender, DataReceivedEventArgs e) {
		if (!string.IsNullOrWhiteSpace(e.Data)) {
			_logger.LogDebug("[Server] {Output}", e.Data);
			OutputReceived?.Invoke(this, e.Data);
		}
	}

	private void OnErrorDataReceived(object sender, DataReceivedEventArgs e) {
		if (!string.IsNullOrWhiteSpace(e.Data)) {
			_logger.LogWarning("[Server Error] {Error}", e.Data);
			ErrorReceived?.Invoke(this, e.Data);
		}
	}

	private void OnProcessExited(object? sender, EventArgs e) {
		_logger.LogInformation("Server process exited with code {ExitCode}", _serverProcess?.ExitCode);
		if (State != ServerState.Stopping) {
			SetState(ServerState.Error);
		}
	}

	public void Dispose() {
		if (_disposed) return;
		_disposed = true;

		_healthCheckCts?.Cancel();
		_healthCheckCts?.Dispose();

		if (_serverProcess is not null) {
			_serverProcess.OutputDataReceived -= OnOutputDataReceived;
			_serverProcess.ErrorDataReceived -= OnErrorDataReceived;
			_serverProcess.Exited -= OnProcessExited;

			if (!_serverProcess.HasExited) {
				try {
					_serverProcess.Kill(entireProcessTree: true);
				}
				catch { }
			}

			_serverProcess.Dispose();
		}
	}
}
