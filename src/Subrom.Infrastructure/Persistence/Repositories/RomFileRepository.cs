using Microsoft.EntityFrameworkCore;
using Subrom.Application.Interfaces;
using Subrom.Domain.Aggregates.Storage;
using Subrom.Domain.ValueObjects;

namespace Subrom.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of IRomFileRepository.
/// </summary>
public sealed class RomFileRepository : IRomFileRepository {
	private readonly SubromDbContext _context;

	public RomFileRepository(SubromDbContext context) {
		_context = context;
	}

	public async Task<RomFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
		await _context.RomFiles.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

	public async Task<IReadOnlyList<RomFile>> GetByDriveAsync(Guid driveId, CancellationToken cancellationToken = default) =>
		await _context.RomFiles
			.Where(r => r.DriveId == driveId)
			.OrderBy(r => r.RelativePath)
			.ToListAsync(cancellationToken);

	public async Task<IReadOnlyList<RomFile>> GetByHashAsync(RomHashes hashes, CancellationToken cancellationToken = default) =>
		await _context.RomFiles
			.Where(r =>
				(r.Crc != null && r.Crc == hashes.Crc.Value) ||
				(r.Md5 != null && r.Md5 == hashes.Md5.Value) ||
				(r.Sha1 != null && r.Sha1 == hashes.Sha1.Value))
			.ToListAsync(cancellationToken);

	public async Task<IReadOnlyList<RomFile>> GetByCrcAsync(Crc crc, CancellationToken cancellationToken = default) =>
		await _context.RomFiles
			.Where(r => r.Crc == crc.Value)
			.ToListAsync(cancellationToken);

	public async Task<IReadOnlyList<RomFile>> GetBySha1Async(Sha1 sha1, CancellationToken cancellationToken = default) =>
		await _context.RomFiles
			.Where(r => r.Sha1 == sha1.Value)
			.ToListAsync(cancellationToken);

	public async Task<bool> ExistsByPathAsync(Guid driveId, string relativePath, CancellationToken cancellationToken = default) =>
		await _context.RomFiles.AnyAsync(r => r.DriveId == driveId && r.RelativePath == relativePath, cancellationToken);

	public async Task AddAsync(RomFile romFile, CancellationToken cancellationToken = default) {
		await _context.RomFiles.AddAsync(romFile, cancellationToken);
	}

	public async Task AddRangeAsync(IEnumerable<RomFile> romFiles, CancellationToken cancellationToken = default) {
		await _context.RomFiles.AddRangeAsync(romFiles, cancellationToken);
	}

	public Task UpdateAsync(RomFile romFile, CancellationToken cancellationToken = default) {
		_context.RomFiles.Update(romFile);
		return Task.CompletedTask;
	}

	public Task UpdateRangeAsync(IEnumerable<RomFile> romFiles, CancellationToken cancellationToken = default) {
		_context.RomFiles.UpdateRange(romFiles);
		return Task.CompletedTask;
	}

	public Task RemoveAsync(RomFile romFile, CancellationToken cancellationToken = default) {
		_context.RomFiles.Remove(romFile);
		return Task.CompletedTask;
	}

	public async Task RemoveByDriveAsync(Guid driveId, CancellationToken cancellationToken = default) {
		await _context.RomFiles
			.Where(r => r.DriveId == driveId)
			.ExecuteDeleteAsync(cancellationToken);
	}

	public async Task<int> GetCountByDriveAsync(Guid driveId, CancellationToken cancellationToken = default) =>
		await _context.RomFiles.CountAsync(r => r.DriveId == driveId, cancellationToken);

	public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default) =>
		await _context.RomFiles.CountAsync(cancellationToken);

	public async Task<(IReadOnlyList<RomFile> Files, string? NextCursor)> GetPagedAsync(
		int pageSize,
		string? cursor = null,
		CancellationToken cancellationToken = default) {
		var query = _context.RomFiles.OrderBy(r => r.Id);

		if (!string.IsNullOrEmpty(cursor) && Guid.TryParse(cursor, out var cursorId)) {
			query = (IOrderedQueryable<RomFile>)query.Where(r => r.Id.CompareTo(cursorId) > 0);
		}

		var files = await query
			.Take(pageSize + 1)
			.ToListAsync(cancellationToken);

		string? nextCursor = null;
		if (files.Count > pageSize) {
			nextCursor = files[pageSize].Id.ToString();
			files = files.Take(pageSize).ToList();
		}

		return (files, nextCursor);
	}

	public async Task<IReadOnlyList<RomFile>> GetUnhashedAsync(int limit, CancellationToken cancellationToken = default) =>
		await _context.RomFiles
			.Where(r => r.Crc == null && r.Md5 == null && r.Sha1 == null)
			.Take(limit)
			.ToListAsync(cancellationToken);

	public async Task<IReadOnlyList<RomFile>> GetUnverifiedAsync(int limit, CancellationToken cancellationToken = default) =>
		await _context.RomFiles
			.Where(r => r.VerificationStatus == VerificationStatus.Unknown)
			.Take(limit)
			.ToListAsync(cancellationToken);
}
