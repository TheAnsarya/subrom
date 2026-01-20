using Microsoft.Extensions.Logging;
using Subrom.Domain.Hash;
using Subrom.Services.Interfaces;

namespace Subrom.Services;

/// <summary>
/// Service for scanning files and computing hashes.
/// </summary>
public class FileScannerService : IFileScannerService {
	private readonly ILogger<FileScannerService> _logger;
	private readonly IHashService _hashService;

	private static readonly HashSet<string> DefaultExtensions = new(StringComparer.OrdinalIgnoreCase) {
		".nes", ".sfc", ".smc", ".gba", ".gbc", ".gb", ".n64", ".z64", ".v64",
		".gen", ".md", ".smd", ".gg", ".sms", ".32x",
		".pce", ".sgx", ".cue", ".iso", ".bin", ".img",
		".a26", ".a52", ".a78", ".lnx", ".jag",
		".zip", ".7z", ".rar"
	};

	public FileScannerService(ILogger<FileScannerService> logger, IHashService hashService) {
		_logger = logger;
		_hashService = hashService;
	}

	/// <summary>
	/// Scans a directory for ROM files.
	/// </summary>
	public async IAsyncEnumerable<ScannedFile> ScanDirectoryAsync(
		string path,
		ScanOptions? options = null,
		IProgress<ScanProgress>? progress = null,
		[System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default) {

		options ??= new ScanOptions();

		_logger.LogInformation("Starting scan of {Path}", path);

		var extensions = options.Extensions ?? DefaultExtensions;
		var searchOption = options.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

		var files = Directory.EnumerateFiles(path, "*", searchOption)
			.Where(f => extensions.Contains(Path.GetExtension(f)))
			.Where(f => !options.SkipHidden || !IsHidden(f))
			.ToList();

		var totalFiles = files.Count;
		var processedFiles = 0;

		_logger.LogInformation("Found {Count} files to scan", totalFiles);

		foreach (var filePath in files) {
			cancellationToken.ThrowIfCancellationRequested();

			ScannedFile? scannedFile = null;

			try {
				var fileInfo = new FileInfo(filePath);
				await using var stream = File.OpenRead(filePath);

				var hashes = await _hashService.GetAllAsync(stream);

				scannedFile = new ScannedFile {
					Path = filePath,
					FileName = Path.GetFileName(filePath),
					Size = fileInfo.Length,
					ModifiedAt = fileInfo.LastWriteTimeUtc,
					Hashes = hashes,
					Status = ScanStatus.Success
				};

				_logger.LogDebug("Scanned: {File} CRC:{Crc}", filePath, hashes.Crc32);
			} catch (Exception ex) {
				_logger.LogWarning(ex, "Failed to scan file: {File}", filePath);
				scannedFile = new ScannedFile {
					Path = filePath,
					FileName = Path.GetFileName(filePath),
					Status = ScanStatus.Error,
					ErrorMessage = ex.Message
				};
			}

			processedFiles++;
			progress?.Report(new ScanProgress {
				TotalFiles = totalFiles,
				ProcessedFiles = processedFiles,
				CurrentFile = filePath,
				Percentage = (double)processedFiles / totalFiles * 100
			});

			yield return scannedFile;
		}

		_logger.LogInformation("Scan complete. Processed {Count} files", processedFiles);
	}

	/// <summary>
	/// Scans a single file.
	/// </summary>
	public async Task<ScannedFile> ScanFileAsync(string filePath, CancellationToken cancellationToken = default) {
		try {
			var fileInfo = new FileInfo(filePath);
			await using var stream = File.OpenRead(filePath);

			var hashes = await _hashService.GetAllAsync(stream);

			return new ScannedFile {
				Path = filePath,
				FileName = Path.GetFileName(filePath),
				Size = fileInfo.Length,
				ModifiedAt = fileInfo.LastWriteTimeUtc,
				Hashes = hashes,
				Status = ScanStatus.Success
			};
		} catch (Exception ex) {
			_logger.LogWarning(ex, "Failed to scan file: {File}", filePath);
			return new ScannedFile {
				Path = filePath,
				FileName = Path.GetFileName(filePath),
				Status = ScanStatus.Error,
				ErrorMessage = ex.Message
			};
		}
	}

	private static bool IsHidden(string path) {
		try {
			var attributes = File.GetAttributes(path);
			return (attributes & FileAttributes.Hidden) == FileAttributes.Hidden;
		} catch {
			return false;
		}
	}
}

/// <summary>
/// Options for file scanning.
/// </summary>
public class ScanOptions {
	public bool Recursive { get; set; } = true;
	public bool SkipHidden { get; set; } = true;
	public HashSet<string>? Extensions { get; set; }
	public bool ComputeAllHashes { get; set; } = true;
}

/// <summary>
/// Progress report for scanning operations.
/// </summary>
public class ScanProgress {
	public int TotalFiles { get; set; }
	public int ProcessedFiles { get; set; }
	public string CurrentFile { get; set; } = "";
	public double Percentage { get; set; }
}

/// <summary>
/// Result of scanning a single file.
/// </summary>
public class ScannedFile {
	public string Path { get; set; } = "";
	public string FileName { get; set; } = "";
	public long Size { get; set; }
	public DateTime ModifiedAt { get; set; }
	public Hashes? Hashes { get; set; }
	public ScanStatus Status { get; set; }
	public string? ErrorMessage { get; set; }
}

/// <summary>
/// Status of a scanned file.
/// </summary>
public enum ScanStatus {
	Success,
	Error,
	Skipped
}

/// <summary>
/// Interface for file scanner service.
/// </summary>
public interface IFileScannerService {
	IAsyncEnumerable<ScannedFile> ScanDirectoryAsync(
		string path,
		ScanOptions? options = null,
		IProgress<ScanProgress>? progress = null,
		CancellationToken cancellationToken = default);

	Task<ScannedFile> ScanFileAsync(string filePath, CancellationToken cancellationToken = default);
}
