using System.Text;
using System.Text.Json;
using Subrom.Application.Interfaces;
using Subrom.Domain.Aggregates.DatFiles;
using Subrom.Domain.Aggregates.Storage;

namespace Subrom.Application.Services;

/// <summary>
/// Export format options.
/// </summary>
public enum ExportFormat {
	Csv,
	Json,
	JsonPretty
}

/// <summary>
/// Options for exporting data.
/// </summary>
public sealed class ExportOptions {
	public ExportFormat Format { get; set; } = ExportFormat.Csv;
	public bool IncludeHashes { get; set; } = true;
	public bool IncludeVerificationStatus { get; set; } = true;
	public string? DateFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";
}

/// <summary>
/// DTO for exported ROM data.
/// </summary>
public sealed record RomExportDto {
	public required string FileName { get; init; }
	public required string RelativePath { get; init; }
	public required string DriveLabel { get; init; }
	public required long Size { get; init; }
	public required string SizeFormatted { get; init; }
	public string? Crc { get; init; }
	public string? Md5 { get; init; }
	public string? Sha1 { get; init; }
	public string? VerificationStatus { get; init; }
	public string? MatchedDat { get; init; }
	public DateTime LastModified { get; init; }
	public DateTime? HashedAt { get; init; }
}

/// <summary>
/// Service for exporting ROM data and verification results.
/// </summary>
public sealed class ExportService {
	private readonly IRomFileRepository _romFileRepository;
	private readonly IDriveRepository _driveRepository;
	private readonly IDatFileRepository _datFileRepository;

	public ExportService(
		IRomFileRepository romFileRepository,
		IDriveRepository driveRepository,
		IDatFileRepository datFileRepository) {
		_romFileRepository = romFileRepository;
		_driveRepository = driveRepository;
		_datFileRepository = datFileRepository;
	}

	/// <summary>
	/// Exports all ROM files with optional filters.
	/// </summary>
	public async Task<string> ExportRomFilesAsync(
		ExportOptions? options = null,
		Guid? driveId = null,
		CancellationToken cancellationToken = default) {
		options ??= new ExportOptions();

		// Get all drives for label lookup
		var drives = await _driveRepository.GetAllAsync(cancellationToken);
		var driveMap = drives.ToDictionary(d => d.Id, d => d.Label);

		// Get DAT files for name lookup if we need verification status
		Dictionary<Guid, DatFile>? datMap = null;
		if (options.IncludeVerificationStatus) {
			var datFiles = await _datFileRepository.GetAllAsync(cancellationToken);
			datMap = datFiles.ToDictionary(d => d.Id);
		}

		// Get ROM files
		IReadOnlyList<RomFile> romFiles;
		if (driveId.HasValue) {
			romFiles = await _romFileRepository.GetByDriveAsync(driveId.Value, cancellationToken);
		} else {
			// Get all drives and collect all files
			var allFiles = new List<RomFile>();
			foreach (var drive in drives) {
				var driveFiles = await _romFileRepository.GetByDriveAsync(drive.Id, cancellationToken);
				allFiles.AddRange(driveFiles);
			}
			romFiles = allFiles;
		}

		// Build export DTOs
		var exportData = romFiles.Select(rom => {
			var driveLabel = driveMap.GetValueOrDefault(rom.DriveId, "Unknown");

			string? verificationStatus = null;
			string? matchedDat = null;

			if (options.IncludeVerificationStatus) {
				verificationStatus = rom.VerificationStatus.ToString();
				if (rom.MatchedDatFileId.HasValue && datMap is not null) {
					if (datMap.TryGetValue(rom.MatchedDatFileId.Value, out var datFile)) {
						matchedDat = datFile.FileName;
					}
				}
			}

			return new RomExportDto {
				FileName = rom.FileName,
				RelativePath = rom.RelativePath,
				DriveLabel = driveLabel,
				Size = rom.Size,
				SizeFormatted = FormatSize(rom.Size),
				Crc = options.IncludeHashes ? rom.Crc : null,
				Md5 = options.IncludeHashes ? rom.Md5 : null,
				Sha1 = options.IncludeHashes ? rom.Sha1 : null,
				VerificationStatus = verificationStatus,
				MatchedDat = matchedDat,
				LastModified = rom.LastModified,
				HashedAt = rom.HashedAt
			};
		}).ToList();

		return options.Format switch {
			ExportFormat.Csv => ExportToCsv(exportData, options),
			ExportFormat.Json => ExportToJson(exportData, false),
			ExportFormat.JsonPretty => ExportToJson(exportData, true),
			_ => throw new ArgumentOutOfRangeException(nameof(options.Format))
		};
	}

	/// <summary>
	/// Exports ROM files filtered by verification status.
	/// </summary>
	public async Task<string> ExportByVerificationStatusAsync(
		VerificationStatus statusFilter,
		ExportOptions? options = null,
		CancellationToken cancellationToken = default) {
		options ??= new ExportOptions();

		// Get all drives
		var drives = await _driveRepository.GetAllAsync(cancellationToken);
		var driveMap = drives.ToDictionary(d => d.Id, d => d.Label);

		// Get all ROM files and filter by status
		var allFiles = new List<RomFile>();
		foreach (var drive in drives) {
			var driveFiles = await _romFileRepository.GetByDriveAsync(drive.Id, cancellationToken);
			allFiles.AddRange(driveFiles.Where(r => r.VerificationStatus == statusFilter));
		}

		var exportData = allFiles.Select(rom => new RomExportDto {
			FileName = rom.FileName,
			RelativePath = rom.RelativePath,
			DriveLabel = driveMap.GetValueOrDefault(rom.DriveId, "Unknown"),
			Size = rom.Size,
			SizeFormatted = FormatSize(rom.Size),
			Crc = options.IncludeHashes ? rom.Crc : null,
			Md5 = options.IncludeHashes ? rom.Md5 : null,
			Sha1 = options.IncludeHashes ? rom.Sha1 : null,
			VerificationStatus = rom.VerificationStatus.ToString(),
			MatchedDat = null,
			LastModified = rom.LastModified,
			HashedAt = rom.HashedAt
		}).ToList();

		return options.Format switch {
			ExportFormat.Csv => ExportToCsv(exportData, options),
			ExportFormat.Json => ExportToJson(exportData, false),
			ExportFormat.JsonPretty => ExportToJson(exportData, true),
			_ => throw new ArgumentOutOfRangeException(nameof(options.Format))
		};
	}

	/// <summary>
	/// Exports a collection statistics summary.
	/// </summary>
	public async Task<string> ExportCollectionSummaryAsync(
		ExportOptions? options = null,
		CancellationToken cancellationToken = default) {
		options ??= new ExportOptions();

		var drives = (await _driveRepository.GetAllAsync(cancellationToken)).ToList();
		var datFiles = (await _datFileRepository.GetAllAsync(cancellationToken)).ToList();

		// Collect all ROM files
		var romFiles = new List<RomFile>();
		foreach (var drive in drives) {
			var driveFiles = await _romFileRepository.GetByDriveAsync(drive.Id, cancellationToken);
			romFiles.AddRange(driveFiles);
		}

		var summary = new {
			GeneratedAt = DateTime.UtcNow,
			TotalRoms = romFiles.Count,
			TotalSize = romFiles.Sum(r => r.Size),
			TotalSizeFormatted = FormatSize(romFiles.Sum(r => r.Size)),
			TotalDatFiles = datFiles.Count,
			DriveCount = drives.Count,
			OnlineDrives = drives.Count(d => d.IsOnline),
			VerificationStats = new {
				Verified = romFiles.Count(r => r.VerificationStatus == VerificationStatus.Verified),
				Unknown = romFiles.Count(r => r.VerificationStatus == VerificationStatus.Unknown),
				BadDump = romFiles.Count(r => r.VerificationStatus == VerificationStatus.BadDump),
				NotHashed = romFiles.Count(r => !r.HasHashes)
			},
			DriveBreakdown = drives.Select(d => new {
				Label = d.Label,
				Path = d.RootPath,
				IsOnline = d.IsOnline,
				TotalSize = d.TotalSize,
				FreeSpace = d.FreeSpace,
				RomCount = romFiles.Count(r => r.DriveId == d.Id)
			}).ToList()
		};

		return options.Format switch {
			ExportFormat.Csv => throw new NotSupportedException("CSV format not supported for summary export. Use JSON."),
			ExportFormat.Json => ExportToJson(summary, false),
			ExportFormat.JsonPretty => ExportToJson(summary, true),
			_ => throw new ArgumentOutOfRangeException(nameof(options.Format))
		};
	}

	private static string ExportToCsv<T>(IEnumerable<T> data, ExportOptions options) {
		var sb = new StringBuilder();
		var dataList = data.ToList();

		if (dataList.Count == 0) {
			return string.Empty;
		}

		// Get properties from first item
		var properties = typeof(T).GetProperties();

		// Header row
		sb.AppendLine(string.Join(",", properties.Select(p => EscapeCsvField(p.Name))));

		// Data rows
		foreach (var item in dataList) {
			var values = properties.Select(p => {
				var value = p.GetValue(item);
				if (value is null) {
					return "";
				}
				if (value is DateTime dt) {
					return dt.ToString(options.DateFormat);
				}
				return value.ToString() ?? "";
			});

			sb.AppendLine(string.Join(",", values.Select(EscapeCsvField)));
		}

		return sb.ToString();
	}

	private static string ExportToJson<T>(T data, bool indented) {
		return JsonSerializer.Serialize(data, new JsonSerializerOptions {
			WriteIndented = indented,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		});
	}

	private static string EscapeCsvField(string field) {
		if (field.Contains(',') || field.Contains('"') || field.Contains('\n')) {
			return $"\"{field.Replace("\"", "\"\"")}\"";
		}
		return field;
	}

	private static string FormatSize(long bytes) {
		string[] suffixes = ["B", "KB", "MB", "GB", "TB"];
		var counter = 0;
		var number = (decimal)bytes;

		while (Math.Round(number / 1024) >= 1 && counter < suffixes.Length - 1) {
			number /= 1024;
			counter++;
		}

		return $"{number:N2} {suffixes[counter]}";
	}
}
