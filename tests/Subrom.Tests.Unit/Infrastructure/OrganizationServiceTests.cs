using Microsoft.Extensions.Logging;
using Moq;
using Subrom.Application.Interfaces;
using Subrom.Domain.Aggregates.Organization;
using Subrom.Infrastructure.Services;

namespace Subrom.Tests.Unit.Infrastructure;

/// <summary>
/// Unit tests for OrganizationService.
/// </summary>
public class OrganizationServiceTests : IDisposable {
	private readonly Mock<ILogger<OrganizationService>> _loggerMock;
	private readonly OrganizationService _service;
	private readonly string _testDir;
	private readonly string _sourceDir;
	private readonly string _destDir;

	public OrganizationServiceTests() {
		_loggerMock = new Mock<ILogger<OrganizationService>>();
		_service = new OrganizationService(_loggerMock.Object);

		// Create temp directories for file system tests
		_testDir = Path.Combine(Path.GetTempPath(), $"subrom_test_{Guid.NewGuid():N}");
		_sourceDir = Path.Combine(_testDir, "source");
		_destDir = Path.Combine(_testDir, "dest");

		Directory.CreateDirectory(_sourceDir);
		Directory.CreateDirectory(_destDir);
	}

	public void Dispose() {
		// Clean up temp directories
		try {
			if (Directory.Exists(_testDir)) {
				Directory.Delete(_testDir, recursive: true);
			}
		} catch {
			// Ignore cleanup failures
		}
		GC.SuppressFinalize(this);
	}

	[Fact]
	public async Task PlanAsync_WithNonExistentSource_ThrowsDirectoryNotFoundException() {
		// Arrange
		var request = CreateRequest(
			sourcePath: @"C:\NonExistent\Path\That\Does\Not\Exist",
			destPath: _destDir);

		// Act & Assert
		await Assert.ThrowsAsync<DirectoryNotFoundException>(
			() => _service.PlanAsync(request));
	}

	[Fact]
	public async Task PlanAsync_WithEmptySource_ReturnsEmptyPlan() {
		// Arrange
		var request = CreateRequest(_sourceDir, _destDir);

		// Act
		var plan = await _service.PlanAsync(request);

		// Assert
		Assert.NotNull(plan);
		Assert.Empty(plan.Operations);
		Assert.Equal(0, plan.TotalBytes);
	}

	[Fact]
	public async Task PlanAsync_WithSingleFile_ReturnsCorrectPlan() {
		// Arrange
		var testFile = Path.Combine(_sourceDir, "test.nes");
		await File.WriteAllTextAsync(testFile, "test content");

		var request = CreateRequest(_sourceDir, _destDir);

		// Act
		var plan = await _service.PlanAsync(request);

		// Assert
		Assert.Single(plan.Operations);
		var operation = plan.Operations[0];
		Assert.Equal(testFile, operation.SourcePath);
		Assert.Equal(FileOperationType.Move, operation.Type);
		Assert.True(operation.Size > 0);
	}

	[Fact]
	public async Task PlanAsync_WithCopyMode_CreatesCopyOperations() {
		// Arrange
		var testFile = Path.Combine(_sourceDir, "test.nes");
		await File.WriteAllTextAsync(testFile, "test content");

		var request = CreateRequest(_sourceDir, _destDir, moveFiles: false);

		// Act
		var plan = await _service.PlanAsync(request);

		// Assert
		Assert.Single(plan.Operations);
		Assert.Equal(FileOperationType.Copy, plan.Operations[0].Type);
	}

	[Fact]
	public async Task PlanAsync_WithIncludePattern_FiltersFiles() {
		// Arrange
		await File.WriteAllTextAsync(Path.Combine(_sourceDir, "game.nes"), "nes content");
		await File.WriteAllTextAsync(Path.Combine(_sourceDir, "save.sav"), "save content");
		await File.WriteAllTextAsync(Path.Combine(_sourceDir, "readme.txt"), "txt content");

		var request = CreateRequest(_sourceDir, _destDir, includePatterns: ["*.nes"]);

		// Act
		var plan = await _service.PlanAsync(request);

		// Assert
		Assert.Single(plan.Operations);
		Assert.Contains("game.nes", plan.Operations[0].SourcePath);
	}

	[Fact]
	public async Task PlanAsync_WithExcludePattern_ExcludesFiles() {
		// Arrange
		await File.WriteAllTextAsync(Path.Combine(_sourceDir, "game.nes"), "nes content");
		await File.WriteAllTextAsync(Path.Combine(_sourceDir, "backup.bak"), "backup content");

		var request = CreateRequest(_sourceDir, _destDir, excludePatterns: ["*.bak"]);

		// Act
		var plan = await _service.PlanAsync(request);

		// Assert
		Assert.Single(plan.Operations);
		Assert.Contains("game.nes", plan.Operations[0].SourcePath);
	}

	[Fact]
	public async Task PlanAsync_WithExistingDestination_SetsWouldOverwrite() {
		// Arrange
		var fileName = "test.nes";
		var sourceFile = Path.Combine(_sourceDir, fileName);
		await File.WriteAllTextAsync(sourceFile, "source content");

		// First plan to find out actual destination
		var requestForPath = CreateRequest(_sourceDir, _destDir);
		var planForPath = await _service.PlanAsync(requestForPath);
		var actualDestPath = planForPath.Operations[0].DestinationPath;

		// Create destination file at the actual destination path
		Directory.CreateDirectory(Path.GetDirectoryName(actualDestPath)!);
		await File.WriteAllTextAsync(actualDestPath, "dest content");

		// Create fresh source file (moved by previous plan)
		await File.WriteAllTextAsync(sourceFile, "source content again");

		// Plan again with destination existing
		var request = CreateRequest(_sourceDir, _destDir);

		// Act
		var plan = await _service.PlanAsync(request);

		// Assert
		Assert.Single(plan.Operations);
		Assert.True(plan.Operations[0].WouldOverwrite);
		Assert.Contains(plan.Warnings, w => w.Contains("overwritten"));
	}

	[Fact]
	public async Task ExecuteAsync_WithMoveOperation_MovesFile() {
		// Arrange
		var testFile = Path.Combine(_sourceDir, "game.nes");
		await File.WriteAllTextAsync(testFile, "game content");

		var request = CreateRequest(_sourceDir, _destDir, moveFiles: true);
		var plan = await _service.PlanAsync(request);

		// Act
		var result = await _service.ExecuteAsync(plan);

		// Assert
		Assert.True(result.Success);
		Assert.Equal(1, result.FilesProcessed);
		Assert.False(File.Exists(testFile));
		Assert.True(result.CanRollback);
	}

	[Fact]
	public async Task ExecuteAsync_WithCopyOperation_CopiesFile() {
		// Arrange
		var testFile = Path.Combine(_sourceDir, "game.nes");
		await File.WriteAllTextAsync(testFile, "game content");

		var request = CreateRequest(_sourceDir, _destDir, moveFiles: false);
		var plan = await _service.PlanAsync(request);

		// Act
		var result = await _service.ExecuteAsync(plan);

		// Assert
		Assert.True(result.Success);
		Assert.Equal(1, result.FilesProcessed);
		Assert.True(File.Exists(testFile)); // Source still exists
		Assert.False(result.CanRollback); // Copy operations can't be rolled back
	}

	[Fact]
	public async Task ExecuteAsync_WithMultipleFiles_ProcessesAll() {
		// Arrange
		await File.WriteAllTextAsync(Path.Combine(_sourceDir, "game1.nes"), "content1");
		await File.WriteAllTextAsync(Path.Combine(_sourceDir, "game2.nes"), "content2");
		await File.WriteAllTextAsync(Path.Combine(_sourceDir, "game3.nes"), "content3");

		var request = CreateRequest(_sourceDir, _destDir);
		var plan = await _service.PlanAsync(request);

		// Act
		var result = await _service.ExecuteAsync(plan);

		// Assert
		Assert.True(result.Success);
		Assert.Equal(3, result.FilesProcessed);
	}

	[Fact]
	public async Task OrganizeAsync_CombinesPlanAndExecute() {
		// Arrange
		await File.WriteAllTextAsync(Path.Combine(_sourceDir, "game.nes"), "content");

		var request = CreateRequest(_sourceDir, _destDir);

		// Act
		var result = await _service.OrganizeAsync(request);

		// Assert
		Assert.True(result.Success);
		Assert.Equal(1, result.FilesProcessed);
	}

	[Fact]
	public async Task RollbackAsync_WithMoveOperation_RestoresFile() {
		// Arrange
		var testFile = Path.Combine(_sourceDir, "game.nes");
		await File.WriteAllTextAsync(testFile, "content");

		var request = CreateRequest(_sourceDir, _destDir, moveFiles: true);
		var result = await _service.OrganizeAsync(request);

		Assert.False(File.Exists(testFile));

		// Act
		var rollbackSuccess = await _service.RollbackAsync(result.OperationId);

		// Assert
		Assert.True(rollbackSuccess);
		Assert.True(File.Exists(testFile));
	}

	[Fact]
	public async Task RollbackAsync_WithNonExistentOperation_ReturnsFalse() {
		// Act
		var result = await _service.RollbackAsync(Guid.NewGuid());

		// Assert
		Assert.False(result);
	}

	[Fact]
	public async Task GetHistoryAsync_ReturnsOperationHistory() {
		// Arrange
		await File.WriteAllTextAsync(Path.Combine(_sourceDir, "game.nes"), "content");

		var request = CreateRequest(_sourceDir, _destDir);
		await _service.OrganizeAsync(request);

		// Act
		var history = await _service.GetHistoryAsync();

		// Assert
		Assert.Single(history);
		Assert.Equal(request.SourcePath, history[0].SourcePath);
		Assert.Equal(request.DestinationPath, history[0].DestinationPath);
	}

	[Fact]
	public async Task GetHistoryAsync_WithLimit_ReturnsLimitedResults() {
		// Arrange - perform multiple operations
		for (int i = 0; i < 5; i++) {
			await File.WriteAllTextAsync(Path.Combine(_sourceDir, $"game{i}.nes"), "content");
			var request = CreateRequest(_sourceDir, _destDir);
			await _service.OrganizeAsync(request);
		}

		// Act
		var history = await _service.GetHistoryAsync(limit: 3);

		// Assert
		Assert.Equal(3, history.Count);
	}

	[Fact]
	public async Task PlanAsync_WithCancellation_ThrowsOperationCanceledException() {
		// Arrange
		var cts = new CancellationTokenSource();
		cts.Cancel();

		// Create many files to ensure cancellation has a chance to trigger
		for (int i = 0; i < 100; i++) {
			await File.WriteAllTextAsync(Path.Combine(_sourceDir, $"game{i}.nes"), "content");
		}

		var request = CreateRequest(_sourceDir, _destDir);

		// Act & Assert
		await Assert.ThrowsAsync<OperationCanceledException>(
			() => _service.PlanAsync(request, cts.Token));
	}

	[Fact]
	public async Task ExecuteAsync_DeletesEmptyFolders_WhenRequested() {
		// Arrange
		var subDir = Path.Combine(_sourceDir, "subfolder");
		Directory.CreateDirectory(subDir);
		var testFile = Path.Combine(subDir, "game.nes");
		await File.WriteAllTextAsync(testFile, "content");

		var request = CreateRequest(_sourceDir, _destDir, moveFiles: true, deleteEmptyFolders: true);
		var plan = await _service.PlanAsync(request);

		// Act
		var result = await _service.ExecuteAsync(plan);

		// Assert
		Assert.True(result.Success);
		Assert.False(Directory.Exists(subDir)); // Subfolder should be deleted
	}

	// Helper methods
	private static OrganizationRequest CreateRequest(
		string sourcePath,
		string destPath,
		bool moveFiles = true,
		bool deleteEmptyFolders = true,
		IReadOnlyList<string>? includePatterns = null,
		IReadOnlyList<string>? excludePatterns = null) {
		return new OrganizationRequest {
			SourcePath = sourcePath,
			DestinationPath = destPath,
			Template = OrganizationTemplate.NoIntroStyle,
			MoveFiles = moveFiles,
			DeleteEmptyFolders = deleteEmptyFolders,
			IncludePatterns = includePatterns ?? ["*.*"],
			ExcludePatterns = excludePatterns ?? []
		};
	}
}
