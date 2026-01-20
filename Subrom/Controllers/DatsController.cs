using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Subrom.Domain.Datfiles;
using Subrom.Infrastructure.Parsers;
using Subrom.SubromAPI.Data;

namespace Subrom.SubromAPI.Controllers;

/// <summary>
/// API controller for managing DAT files.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class DatsController(
	SubromDbContext db,
	ILogger<DatsController> logger,
	IEnumerable<IDatParser> parsers
) : ControllerBase {
	/// <summary>Imports a DAT file (XML/LogiqX or ClrMamePro format).</summary>
	[HttpPost("import")]
	[Consumes("multipart/form-data")]
	public async Task<ActionResult<DatFileDto>> ImportDatFile(
		IFormFile file,
		[FromQuery] string? provider = null,
		CancellationToken ct = default
	) {
		if (file.Length == 0) return BadRequest("File is empty");

		await using var stream = file.OpenReadStream();

		// Try each parser to find one that can handle this format
		IDatParser? selectedParser = null;
		foreach (var parser in parsers) {
			if (parser.CanParse(stream)) {
				selectedParser = parser;
				stream.Position = 0; // Reset after probing
				break;
			}
			stream.Position = 0; // Reset for next parser
		}

		if (selectedParser is null) {
			return BadRequest("Unsupported DAT file format. Supported formats: XML/LogiqX, ClrMamePro");
		}

		logger.LogInformation("Parsing DAT file {FileName} with {Parser}", file.FileName, selectedParser.FormatName);

		Datafile datafile;
		try {
			datafile = await selectedParser.ParseAsync(stream, ct);
		} catch (Exception ex) {
			logger.LogError(ex, "Failed to parse DAT file {FileName}", file.FileName);
			return BadRequest($"Failed to parse DAT file: {ex.Message}");
		}

		// Create database entities
		var datEntity = new DatFileEntity {
			Id = Guid.CreateVersion7(),
			Name = datafile.Header?.Name ?? Path.GetFileNameWithoutExtension(file.FileName),
			Description = datafile.Header?.Description,
			Provider = provider ?? datafile.Header?.Name?.Split(' ').FirstOrDefault() ?? "Unknown",
			Version = datafile.Header?.Version,
			Author = datafile.Header?.Author,
			FilePath = file.FileName,
			ImportedAt = DateTime.UtcNow,
			IsEnabled = true,
		};

		// Process games
		foreach (var game in datafile.Games) {
			var gameEntity = new GameEntity {
				Id = Guid.CreateVersion7(),
				DatFileId = datEntity.Id,
				Name = game.Name ?? "Unknown",
				Description = game.Description,
				CloneOf = game.CloneOf,
				RomOf = game.RomOf,
				Year = game.Year?.Value,
				Manufacturer = game.Manufacturer,
			};

			foreach (var rom in game.Roms) {
				var romEntity = new RomEntryEntity {
					Id = Guid.CreateVersion7(),
					GameId = gameEntity.Id,
					Name = rom.Name ?? "Unknown",
					Size = rom.Size,
					Crc32 = rom.Crc?.Value,
					Md5 = rom.Md5?.Value,
					Sha1 = rom.Sha1?.Value,
					Status = rom.Status?.Value,
				};
				gameEntity.Roms.Add(romEntity);
			}

			datEntity.Games.Add(gameEntity);
		}

		// Process machines (similar structure to games)
		foreach (var machine in datafile.Machines) {
			var gameEntity = new GameEntity {
				Id = Guid.CreateVersion7(),
				DatFileId = datEntity.Id,
				Name = machine.Name ?? "Unknown",
				Description = machine.Description,
				CloneOf = machine.CloneOf,
				RomOf = machine.RomOf,
				Year = machine.Year?.Value,
				Manufacturer = machine.Manufacturer,
			};

			foreach (var rom in machine.Roms) {
				var romEntity = new RomEntryEntity {
					Id = Guid.CreateVersion7(),
					GameId = gameEntity.Id,
					Name = rom.Name ?? "Unknown",
					Size = rom.Size,
					Crc32 = rom.Crc?.Value,
					Md5 = rom.Md5?.Value,
					Sha1 = rom.Sha1?.Value,
					Status = rom.Status?.Value,
				};
				gameEntity.Roms.Add(romEntity);
			}

			datEntity.Games.Add(gameEntity);
		}

		datEntity.GameCount = datEntity.Games.Count;
		datEntity.RomCount = datEntity.Games.Sum(g => g.Roms.Count);

		db.DatFiles.Add(datEntity);
		await db.SaveChangesAsync(ct);

		logger.LogInformation("Imported DAT file {Name} with {GameCount} games and {RomCount} ROMs",
			datEntity.Name, datEntity.GameCount, datEntity.RomCount);

		return CreatedAtAction(nameof(GetDatFile), new { id = datEntity.Id }, new DatFileDto(
			datEntity.Id,
			datEntity.Name,
			datEntity.Description,
			datEntity.Provider,
			datEntity.Version,
			datEntity.ImportedAt,
			datEntity.GameCount,
			datEntity.RomCount,
			datEntity.IsEnabled
		));
	}

	/// <summary>Gets all imported DAT files.</summary>
	[HttpGet]
	public async Task<ActionResult<IEnumerable<DatFileDto>>> GetDatFiles(
		[FromQuery] string? provider = null,
		[FromQuery] bool? enabled = null,
		CancellationToken ct = default
	) {
		var query = db.DatFiles.AsQueryable();

		if (!string.IsNullOrEmpty(provider))
			query = query.Where(d => d.Provider == provider);

		if (enabled.HasValue)
			query = query.Where(d => d.IsEnabled == enabled.Value);

		var dats = await query
			.OrderBy(d => d.Provider)
			.ThenBy(d => d.Name)
			.Select(d => new DatFileDto(
				d.Id,
				d.Name,
				d.Description,
				d.Provider,
				d.Version,
				d.ImportedAt,
				d.GameCount,
				d.RomCount,
				d.IsEnabled
			))
			.ToListAsync(ct);

		return Ok(dats);
	}

	/// <summary>Gets a DAT file by ID.</summary>
	[HttpGet("{id:guid}")]
	public async Task<ActionResult<DatFileDetailDto>> GetDatFile(Guid id, CancellationToken ct) {
		var dat = await db.DatFiles
			.Include(d => d.Games)
			.FirstOrDefaultAsync(d => d.Id == id, ct);

		if (dat is null) return NotFound();

		return Ok(new DatFileDetailDto(
			dat.Id,
			dat.Name,
			dat.Description,
			dat.Provider,
			dat.Version,
			dat.Author,
			dat.FilePath,
			dat.ImportedAt,
			dat.UpdatedAt,
			dat.GameCount,
			dat.RomCount,
			dat.IsEnabled,
			dat.Games.Select(g => new GameSummaryDto(g.Id, g.Name, g.Description, g.Roms.Count)).ToList()
		));
	}

	/// <summary>Gets games from a DAT file.</summary>
	[HttpGet("{id:guid}/games")]
	public async Task<ActionResult<PagedResult<GameDto>>> GetDatGames(
		Guid id,
		[FromQuery] string? search = null,
		[FromQuery] int page = 1,
		[FromQuery] int pageSize = 50,
		CancellationToken ct = default
	) {
		var datExists = await db.DatFiles.AnyAsync(d => d.Id == id, ct);
		if (!datExists) return NotFound();

		var query = db.Games.Where(g => g.DatFileId == id);

		if (!string.IsNullOrEmpty(search))
			query = query.Where(g => g.Name.Contains(search) || (g.Description != null && g.Description.Contains(search)));

		var totalCount = await query.CountAsync(ct);

		var games = await query
			.OrderBy(g => g.Name)
			.Skip((page - 1) * pageSize)
			.Take(pageSize)
			.Select(g => new GameDto(
				g.Id,
				g.Name,
				g.Description,
				g.CloneOf,
				g.RomOf,
				g.Year,
				g.Manufacturer,
				g.Roms.Count
			))
			.ToListAsync(ct);

		return Ok(new PagedResult<GameDto>(games, totalCount, page, pageSize));
	}

	/// <summary>Deletes a DAT file.</summary>
	[HttpDelete("{id:guid}")]
	public async Task<ActionResult> DeleteDatFile(Guid id, CancellationToken ct) {
		var dat = await db.DatFiles.FindAsync([id], ct);
		if (dat is null) return NotFound();

		// Cascade delete games and roms
		db.DatFiles.Remove(dat);
		await db.SaveChangesAsync(ct);

		logger.LogInformation("Deleted DAT file {Name}", dat.Name);

		return NoContent();
	}

	/// <summary>Toggles DAT file enabled status.</summary>
	[HttpPost("{id:guid}/toggle")]
	public async Task<ActionResult> ToggleDatFile(Guid id, CancellationToken ct) {
		var dat = await db.DatFiles.FindAsync([id], ct);
		if (dat is null) return NotFound();

		dat.IsEnabled = !dat.IsEnabled;
		await db.SaveChangesAsync(ct);

		return NoContent();
	}

	/// <summary>Gets DAT providers summary.</summary>
	[HttpGet("providers")]
	public async Task<ActionResult<IEnumerable<ProviderSummaryDto>>> GetProviders(CancellationToken ct) {
		var providers = await db.DatFiles
			.GroupBy(d => d.Provider)
			.Select(g => new ProviderSummaryDto(
				g.Key ?? "Unknown",
				g.Count(),
				g.Sum(d => d.GameCount),
				g.Sum(d => d.RomCount)
			))
			.ToListAsync(ct);

		return Ok(providers);
	}
}

public sealed record DatFileDto(
	Guid Id,
	string Name,
	string? Description,
	string? Provider,
	string? Version,
	DateTime ImportedAt,
	int GameCount,
	int RomCount,
	bool IsEnabled
);

public sealed record DatFileDetailDto(
	Guid Id,
	string Name,
	string? Description,
	string? Provider,
	string? Version,
	string? Author,
	string? FilePath,
	DateTime ImportedAt,
	DateTime? UpdatedAt,
	int GameCount,
	int RomCount,
	bool IsEnabled,
	IReadOnlyList<GameSummaryDto> Games
);

public sealed record GameSummaryDto(Guid Id, string Name, string? Description, int RomCount);

public sealed record GameDto(
	Guid Id,
	string Name,
	string? Description,
	string? CloneOf,
	string? RomOf,
	string? Year,
	string? Manufacturer,
	int RomCount
);

public sealed record ProviderSummaryDto(string Provider, int DatCount, int GameCount, int RomCount);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize) {
	public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
	public bool HasNext => Page < TotalPages;
	public bool HasPrevious => Page > 1;
}
