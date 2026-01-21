using Microsoft.EntityFrameworkCore;
using Subrom.Application.Interfaces;
using Subrom.Domain.Aggregates.DatFiles;
using Subrom.Domain.ValueObjects;

namespace Subrom.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of IDatFileRepository.
/// </summary>
public sealed class DatFileRepository : IDatFileRepository {
	private readonly SubromDbContext _context;

	public DatFileRepository(SubromDbContext context) {
		_context = context;
	}

	public async Task<DatFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
		await _context.DatFiles
			.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

	public async Task<DatFile?> GetByIdWithGamesAsync(Guid id, CancellationToken cancellationToken = default) =>
		await _context.DatFiles
			.Include(d => d.Games)
				.ThenInclude(g => g.Roms)
			.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

	public async Task<IReadOnlyList<DatFile>> GetAllAsync(CancellationToken cancellationToken = default) =>
		await _context.DatFiles
			.OrderBy(d => d.CategoryPath)
			.ThenBy(d => d.Name)
			.ToListAsync(cancellationToken);

	public async Task<IReadOnlyList<DatFile>> GetByCategoryAsync(string categoryPath, CancellationToken cancellationToken = default) =>
		await _context.DatFiles
			.Where(d => d.CategoryPath != null && d.CategoryPath.StartsWith(categoryPath))
			.OrderBy(d => d.CategoryPath)
			.ThenBy(d => d.Name)
			.ToListAsync(cancellationToken);

	public async Task<IReadOnlyList<DatFile>> GetByProviderAsync(DatProvider provider, CancellationToken cancellationToken = default) =>
		await _context.DatFiles
			.Where(d => d.Provider == provider)
			.OrderBy(d => d.Name)
			.ToListAsync(cancellationToken);

	public async Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default) =>
		await _context.DatFiles.AnyAsync(d => d.Name == name, cancellationToken);

	public async Task AddAsync(DatFile datFile, CancellationToken cancellationToken = default) {
		await _context.DatFiles.AddAsync(datFile, cancellationToken);
	}

	public Task UpdateAsync(DatFile datFile, CancellationToken cancellationToken = default) {
		_context.DatFiles.Update(datFile);
		return Task.CompletedTask;
	}

	public Task RemoveAsync(DatFile datFile, CancellationToken cancellationToken = default) {
		_context.DatFiles.Remove(datFile);
		return Task.CompletedTask;
	}

	public async Task<int> GetCountAsync(CancellationToken cancellationToken = default) =>
		await _context.DatFiles.CountAsync(cancellationToken);

	public async Task<IReadOnlyList<string>> GetCategoryPathsAsync(CancellationToken cancellationToken = default) =>
		await _context.DatFiles
			.Where(d => d.CategoryPath != null)
			.Select(d => d.CategoryPath!)
			.Distinct()
			.OrderBy(c => c)
			.ToListAsync(cancellationToken);

	public async Task<int> GetGameCountAsync(Guid datFileId, CancellationToken cancellationToken = default) =>
		await _context.Games.CountAsync(g => g.DatFileId == datFileId, cancellationToken);

	public async Task<int> GetRomCountAsync(Guid datFileId, CancellationToken cancellationToken = default) =>
		await _context.Roms
			.Where(r => _context.Games.Any(g => g.DatFileId == datFileId && g.Id == r.GameId))
			.CountAsync(cancellationToken);

	public async Task<IReadOnlyList<DatRomMatch>> FindRomsByHashAsync(RomHashes hashes, CancellationToken cancellationToken = default) {
		// Search by any matching hash (CRC32, MD5, or SHA1)
		var query = _context.Roms.AsQueryable();

		var crc = hashes.Crc.Value;
		var md5 = hashes.Md5.Value;
		var sha1 = hashes.Sha1.Value;

		// Build OR condition for any matching hash
		if (!string.IsNullOrEmpty(crc)) {
			query = query.Where(r =>
				r.Crc == crc ||
				r.Md5 == md5 ||
				r.Sha1 == sha1);
		} else if (!string.IsNullOrEmpty(md5)) {
			query = query.Where(r =>
				r.Md5 == md5 ||
				r.Sha1 == sha1);
		} else if (!string.IsNullOrEmpty(sha1)) {
			query = query.Where(r => r.Sha1 == sha1);
		} else {
			return [];
		}

		var roms = await query.ToListAsync(cancellationToken);
		if (roms.Count == 0) return [];

		// Get the game IDs for the matched ROMs
		var gameIds = roms.Select(r => r.GameId).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
		var games = await _context.Games
			.Where(g => gameIds.Contains(g.Id))
			.ToDictionaryAsync(g => g.Id, cancellationToken);

		// Get the DAT file IDs for the matched games
		var datFileIds = games.Values.Select(g => g.DatFileId).Distinct().ToList();
		var datFiles = await _context.DatFiles
			.Where(d => datFileIds.Contains(d.Id))
			.ToDictionaryAsync(d => d.Id, cancellationToken);

		// Build the result with full context
		var results = new List<DatRomMatch>();
		foreach (var rom in roms) {
			if (rom.GameId is not { } gameId || !games.TryGetValue(gameId, out var game))
				continue;

			if (!datFiles.TryGetValue(game.DatFileId, out var datFile))
				continue;

			results.Add(new DatRomMatch(rom, game, datFile));
		}

		return results;
	}

	public async Task<IReadOnlyList<DatRomMatch>> FindRomsByHashesAsync(IEnumerable<RomHashes> hashes, CancellationToken cancellationToken = default) {
		var hashList = hashes.ToList();
		if (hashList.Count == 0) return [];

		// Extract all unique hash values as strings
		var crc32s = hashList.Select(h => h.Crc.Value).Where(h => !string.IsNullOrEmpty(h)).Distinct().ToList();
		var md5s = hashList.Select(h => h.Md5.Value).Where(h => !string.IsNullOrEmpty(h)).Distinct().ToList();
		var sha1s = hashList.Select(h => h.Sha1.Value).Where(h => !string.IsNullOrEmpty(h)).Distinct().ToList();

		// Query ROMs matching any of the hashes
		var query = _context.Roms.AsQueryable();

		// EF Core limitation: Can't use Contains with nullable - need to check each hash type
		if (crc32s.Count > 0 || md5s.Count > 0 || sha1s.Count > 0) {
			query = query.Where(r =>
				(r.Crc != null && crc32s.Contains(r.Crc)) ||
				(r.Md5 != null && md5s.Contains(r.Md5)) ||
				(r.Sha1 != null && sha1s.Contains(r.Sha1)));
		} else {
			return [];
		}

		var roms = await query.ToListAsync(cancellationToken);
		if (roms.Count == 0) return [];

		// Get the game IDs for the matched ROMs
		var gameIds = roms.Select(r => r.GameId).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
		var games = await _context.Games
			.Where(g => gameIds.Contains(g.Id))
			.ToDictionaryAsync(g => g.Id, cancellationToken);

		// Get the DAT file IDs for the matched games
		var datFileIds = games.Values.Select(g => g.DatFileId).Distinct().ToList();
		var datFiles = await _context.DatFiles
			.Where(d => datFileIds.Contains(d.Id))
			.ToDictionaryAsync(d => d.Id, cancellationToken);

		// Build the result with full context
		var results = new List<DatRomMatch>();
		foreach (var rom in roms) {
			if (rom.GameId is not { } gameId || !games.TryGetValue(gameId, out var game))
				continue;

			if (!datFiles.TryGetValue(game.DatFileId, out var datFile))
				continue;

			results.Add(new DatRomMatch(rom, game, datFile));
		}

		return results;
	}
}
