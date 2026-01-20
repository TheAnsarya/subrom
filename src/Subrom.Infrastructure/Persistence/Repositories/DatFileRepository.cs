using Microsoft.EntityFrameworkCore;
using Subrom.Application.Interfaces;
using Subrom.Domain.Aggregates.DatFiles;

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
}
