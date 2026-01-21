using System.Diagnostics;

namespace Subrom.Service;

/// <summary>
/// Subrom Windows Service worker that hosts the web server.
/// </summary>
public sealed class SubromServiceWorker : BackgroundService {
	private readonly ILogger<SubromServiceWorker> _logger;
	private readonly IConfiguration _configuration;
	private Process? _serverProcess;

	public SubromServiceWorker(
		ILogger<SubromServiceWorker> logger,
		IConfiguration configuration) {
		_logger = logger;
		_configuration = configuration;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		_logger.LogInformation("Subrom Service starting");

		try {
			await StartServerAsync(stoppingToken);

			// Keep monitoring while service is running
			while (!stoppingToken.IsCancellationRequested) {
				if (_serverProcess is null || _serverProcess.HasExited) {
					_logger.LogWarning("Server process has stopped, restarting...");
					await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
					await StartServerAsync(stoppingToken);
				}

				await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
			}
		}
		catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
			_logger.LogInformation("Subrom Service stopping");
		}
		catch (Exception ex) {
			_logger.LogError(ex, "Error in Subrom Service");
			throw;
		}
	}

	private async Task StartServerAsync(CancellationToken cancellationToken) {
		var serverPath = GetServerPath();
		if (string.IsNullOrEmpty(serverPath)) {
			_logger.LogError("Server executable not found");
			return;
		}

		_logger.LogInformation("Starting server from {ServerPath}", serverPath);

		var startInfo = new ProcessStartInfo {
			FileName = serverPath,
			WorkingDirectory = Path.GetDirectoryName(serverPath),
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
		};

		_serverProcess = new Process { StartInfo = startInfo };
		_serverProcess.OutputDataReceived += (_, e) => {
			if (!string.IsNullOrEmpty(e.Data)) {
				_logger.LogDebug("[Server] {Output}", e.Data);
			}
		};
		_serverProcess.ErrorDataReceived += (_, e) => {
			if (!string.IsNullOrEmpty(e.Data)) {
				_logger.LogWarning("[Server] {Error}", e.Data);
			}
		};

		if (!_serverProcess.Start()) {
			throw new InvalidOperationException("Failed to start server process");
		}

		_serverProcess.BeginOutputReadLine();
		_serverProcess.BeginErrorReadLine();

		_logger.LogInformation("Server process started with PID {ProcessId}", _serverProcess.Id);

		// Wait for server to be ready
		var healthy = await WaitForHealthyAsync(cancellationToken);
		if (healthy) {
			_logger.LogInformation("Server is healthy and ready");
		} else {
			_logger.LogWarning("Server may not be fully ready");
		}
	}

	private async Task<bool> WaitForHealthyAsync(CancellationToken cancellationToken) {
		var serverUrl = _configuration["ServerUrl"] ?? "http://localhost:52100";
		using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

		for (var i = 0; i < 30; i++) {
			cancellationToken.ThrowIfCancellationRequested();

			try {
				var response = await client.GetAsync($"{serverUrl}/health", cancellationToken);
				if (response.IsSuccessStatusCode) {
					return true;
				}
			}
			catch {
				// Server not ready yet
			}

			await Task.Delay(1000, cancellationToken);
		}

		return false;
	}

	private string GetServerPath() {
		var configuredPath = _configuration["ServerPath"];
		if (!string.IsNullOrEmpty(configuredPath) && File.Exists(configuredPath)) {
			return configuredPath;
		}

		// Try common locations
		var candidates = new[] {
			Path.Combine(AppContext.BaseDirectory, "Subrom.Server.exe"),
			Path.Combine(AppContext.BaseDirectory, "..", "Subrom.Server", "Subrom.Server.exe"),
		};

		return candidates.FirstOrDefault(File.Exists) ?? "";
	}

	public override async Task StopAsync(CancellationToken cancellationToken) {
		_logger.LogInformation("Stopping Subrom Service");

		if (_serverProcess is not null && !_serverProcess.HasExited) {
			try {
				// Try graceful shutdown
				var serverUrl = _configuration["ServerUrl"] ?? "http://localhost:52100";
				using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
				await client.PostAsync($"{serverUrl}/api/shutdown", null, cancellationToken);
				await Task.Delay(1000, cancellationToken);
			}
			catch {
				// Ignore shutdown errors
			}

			if (!_serverProcess.HasExited) {
				_logger.LogWarning("Force killing server process");
				_serverProcess.Kill(entireProcessTree: true);
			}

			_serverProcess.Dispose();
			_serverProcess = null;
		}

		await base.StopAsync(cancellationToken);
	}

	public override void Dispose() {
		_serverProcess?.Dispose();
		base.Dispose();
	}
}
