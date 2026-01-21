using Subrom.Domain.Aggregates.DatFiles;
using Subrom.Domain.ValueObjects;

namespace Subrom.Application.Interfaces;

/// <summary>
/// Repository interface for DAT file persistence.
/// </summary>
public interface IDatFileRepository {
	/// <summary>
	/// Gets a DAT file by ID.
	/// </summary>
	Task<DatFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets a DAT file by ID with all games and ROMs loaded.
	/// </summary>
	Task<DatFile?> GetByIdWithGamesAsync(Guid id, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets all DAT files (without games loaded).
	/// </summary>
	Task<IReadOnlyList<DatFile>> GetAllAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets DAT files by category path prefix.
	/// </summary>
	Task<IReadOnlyList<DatFile>> GetByCategoryAsync(string categoryPath, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets DAT files by provider.
	/// </summary>
	Task<IReadOnlyList<DatFile>> GetByProviderAsync(DatProvider provider, CancellationToken cancellationToken = default);

	/// <summary>
	/// Checks if a DAT file with the same name exists.
	/// </summary>
	Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);

	/// <summary>
	/// Adds a new DAT file.
	/// </summary>
	Task AddAsync(DatFile datFile, CancellationToken cancellationToken = default);

	/// <summary>
	/// Updates an existing DAT file.
	/// </summary>
	Task UpdateAsync(DatFile datFile, CancellationToken cancellationToken = default);

	/// <summary>
	/// Removes a DAT file.
	/// </summary>
	Task RemoveAsync(DatFile datFile, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets the total count of DAT files.
	/// </summary>
	Task<int> GetCountAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets all unique category paths for tree navigation.
	/// </summary>
	Task<IReadOnlyList<string>> GetCategoryPathsAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Finds ROM entries matching the given hashes across all DAT files.
	/// Returns matches with their parent game and DAT file information.
	/// </summary>
	Task<IReadOnlyList<DatRomMatch>> FindRomsByHashAsync(RomHashes hashes, CancellationToken cancellationToken = default);

	/// <summary>
	/// Finds ROM entries matching any of the given hashes (batch lookup).
	/// Returns matches with their parent game and DAT file information.
	/// </summary>
	Task<IReadOnlyList<DatRomMatch>> FindRomsByHashesAsync(IEnumerable<RomHashes> hashes, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a ROM match found in a DAT file.
/// </summary>
/// <param name="RomEntry">The matched ROM entry</param>
/// <param name="GameEntry">The parent game containing this ROM</param>
/// <param name="DatFile">The DAT file containing this entry</param>
public sealed record DatRomMatch(
	RomEntry RomEntry,
	GameEntry GameEntry,
	DatFile DatFile);

