using Subrom.Application.Interfaces;
using Subrom.Domain.Aggregates.DatFiles;

namespace Subrom.Application.Services;

/// <summary>
/// Application service for DAT file operations.
/// </summary>
public sealed class DatFileService {
	private readonly IDatFileRepository _datFileRepository;
	private readonly IDatParserFactory _parserFactory;
	private readonly IUnitOfWork _unitOfWork;

	public DatFileService(
		IDatFileRepository datFileRepository,
		IDatParserFactory parserFactory,
		IUnitOfWork unitOfWork) {
		_datFileRepository = datFileRepository;
		_parserFactory = parserFactory;
		_unitOfWork = unitOfWork;
	}

	/// <summary>
	/// Imports a DAT file from the given path.
	/// </summary>
	public async Task<DatFile> ImportAsync(
		string filePath,
		string? categoryPath = null,
		IProgress<DatParseProgress>? progress = null,
		CancellationToken cancellationToken = default) {
		ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

		// Get appropriate parser
		var parser = _parserFactory.GetParser(filePath)
			?? throw new NotSupportedException($"No parser found for file: {Path.GetFileName(filePath)}");

		// Parse the file
		var datFile = await parser.ParseAsync(filePath, progress, cancellationToken);

		// Set category if provided
		if (!string.IsNullOrWhiteSpace(categoryPath)) {
			datFile.CategoryPath = categoryPath;
		}

		// Check for duplicates
		if (await _datFileRepository.ExistsByNameAsync(datFile.Name, cancellationToken)) {
			throw new InvalidOperationException($"DAT file '{datFile.Name}' already exists.");
		}

		// Save
		await _datFileRepository.AddAsync(datFile, cancellationToken);
		await _unitOfWork.SaveChangesAsync(cancellationToken);

		return datFile;
	}

	/// <summary>
	/// Gets all DAT files, optionally filtered by category.
	/// </summary>
	public async Task<IReadOnlyList<DatFile>> GetAllAsync(
		string? categoryFilter = null,
		CancellationToken cancellationToken = default) {
		if (string.IsNullOrWhiteSpace(categoryFilter)) {
			return await _datFileRepository.GetAllAsync(cancellationToken);
		}

		return await _datFileRepository.GetByCategoryAsync(categoryFilter, cancellationToken);
	}

	/// <summary>
	/// Gets a DAT file by ID.
	/// </summary>
	public Task<DatFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) {
		return _datFileRepository.GetByIdAsync(id, cancellationToken);
	}

	/// <summary>
	/// Gets a DAT file with all games loaded.
	/// </summary>
	public Task<DatFile?> GetByIdWithGamesAsync(Guid id, CancellationToken cancellationToken = default) {
		return _datFileRepository.GetByIdWithGamesAsync(id, cancellationToken);
	}

	/// <summary>
	/// Gets all unique category paths in the repository.
	/// </summary>
	public Task<IReadOnlyList<string>> GetCategoryPathsAsync(CancellationToken cancellationToken = default) {
		return _datFileRepository.GetCategoryPathsAsync(cancellationToken);
	}

	/// <summary>
	/// Deletes a DAT file.
	/// </summary>
	public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) {
		var datFile = await _datFileRepository.GetByIdAsync(id, cancellationToken)
			?? throw new KeyNotFoundException($"DAT file {id} not found.");

		await _datFileRepository.RemoveAsync(datFile, cancellationToken);
		await _unitOfWork.SaveChangesAsync(cancellationToken);
	}

	/// <summary>
	/// Toggles the enabled state of a DAT file.
	/// </summary>
	public async Task<DatFile> ToggleEnabledAsync(Guid id, CancellationToken cancellationToken = default) {
		var datFile = await _datFileRepository.GetByIdAsync(id, cancellationToken)
			?? throw new KeyNotFoundException($"DAT file {id} not found.");

		datFile.IsEnabled = !datFile.IsEnabled;

		await _datFileRepository.UpdateAsync(datFile, cancellationToken);
		await _unitOfWork.SaveChangesAsync(cancellationToken);

		return datFile;
	}

	/// <summary>
	/// Updates the category of a DAT file.
	/// </summary>
	public async Task<DatFile> SetCategoryAsync(
		Guid id,
		string categoryPath,
		CancellationToken cancellationToken = default) {
		var datFile = await _datFileRepository.GetByIdAsync(id, cancellationToken)
			?? throw new KeyNotFoundException($"DAT file {id} not found.");

		datFile.CategoryPath = categoryPath;

		await _datFileRepository.UpdateAsync(datFile, cancellationToken);
		await _unitOfWork.SaveChangesAsync(cancellationToken);

		return datFile;
	}
}
