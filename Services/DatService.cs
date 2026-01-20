using Microsoft.Extensions.Logging;
using Subrom.Domain.Datfiles;
using Subrom.Infrastructure.Parsers;

namespace Subrom.Services;

/// <summary>
/// Service for managing DAT files - importing, parsing, and querying.
/// </summary>
public class DatService : IDatService {
	private readonly ILogger<DatService> _logger;
	private readonly IEnumerable<IDatParser> _parsers;

	public DatService(ILogger<DatService> logger, IEnumerable<IDatParser> parsers) {
		_logger = logger;
		_parsers = parsers;
	}

	/// <summary>
	/// Imports a DAT file from the specified path.
	/// </summary>
	public async Task<Datafile> ImportAsync(string filePath, CancellationToken cancellationToken = default) {
		_logger.LogInformation("Importing DAT file: {FilePath}", filePath);

		await using var stream = File.OpenRead(filePath);
		return await ImportAsync(stream, cancellationToken);
	}

	/// <summary>
	/// Imports a DAT file from a stream.
	/// </summary>
	public async Task<Datafile> ImportAsync(Stream stream, CancellationToken cancellationToken = default) {
		// Try each parser until one succeeds
		foreach (var parser in _parsers) {
			if (parser.CanParse(stream)) {
				_logger.LogDebug("Using parser: {ParserName}", parser.FormatName);
				return await parser.ParseAsync(stream, cancellationToken);
			}

			// Reset stream position for next parser
			stream.Position = 0;
		}

		throw new InvalidOperationException("No parser found that can handle this DAT file format.");
	}

	/// <summary>
	/// Gets statistics about a DAT file.
	/// </summary>
	public DatStatistics GetStatistics(Datafile datafile) {
		var games = datafile.Games.Count + datafile.Machines.Count;
		var roms = datafile.Games.Sum(g => g.Roms.Count) + datafile.Machines.Sum(m => m.Roms.Count);
		var disks = datafile.Games.Sum(g => g.Disks.Count) + datafile.Machines.Sum(m => m.Disks.Count);
		var totalSize = datafile.Games.Sum(g => g.Roms.Sum(r => r.Size)) +
						datafile.Machines.Sum(m => m.Roms.Sum(r => r.Size));

		return new DatStatistics {
			Name = datafile.Header.Name,
			Version = datafile.Header.Version,
			GameCount = games,
			RomCount = roms,
			DiskCount = disks,
			TotalSize = totalSize
		};
	}
}

/// <summary>
/// Statistics about a DAT file.
/// </summary>
public class DatStatistics {
	public string Name { get; set; } = "";
	public string Version { get; set; } = "";
	public int GameCount { get; set; }
	public int RomCount { get; set; }
	public int DiskCount { get; set; }
	public long TotalSize { get; set; }
}

/// <summary>
/// Interface for DAT file service.
/// </summary>
public interface IDatService {
	Task<Datafile> ImportAsync(string filePath, CancellationToken cancellationToken = default);
	Task<Datafile> ImportAsync(Stream stream, CancellationToken cancellationToken = default);
	DatStatistics GetStatistics(Datafile datafile);
}
