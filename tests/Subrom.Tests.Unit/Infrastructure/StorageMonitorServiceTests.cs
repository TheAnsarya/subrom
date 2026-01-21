using Microsoft.Extensions.Logging;
using Moq;
using Subrom.Application.Interfaces;
using Subrom.Domain.Aggregates.Storage;
using Subrom.Domain.ValueObjects;
using Subrom.Infrastructure.Services;
using DriveType = Subrom.Domain.Aggregates.Storage.DriveType;

namespace Subrom.Tests.Unit.Infrastructure;

public class StorageMonitorServiceTests {
	private readonly Mock<ILogger<StorageMonitorService>> _mockLogger;
	private readonly Mock<IDriveRepository> _mockDriveRepo;
	private readonly Mock<IRomFileRepository> _mockRomFileRepo;
	private readonly Mock<IUnitOfWork> _mockUnitOfWork;
	private readonly StorageMonitorService _service;

	public StorageMonitorServiceTests() {
		_mockLogger = new Mock<ILogger<StorageMonitorService>>();
		_mockDriveRepo = new Mock<IDriveRepository>();
		_mockRomFileRepo = new Mock<IRomFileRepository>();
		_mockUnitOfWork = new Mock<IUnitOfWork>();

		_service = new StorageMonitorService(
			_mockLogger.Object,
			_mockDriveRepo.Object,
			_mockRomFileRepo.Object,
			_mockUnitOfWork.Object);
	}

	[Fact]
	public async Task GetSummaryAsync_WithNoDrives_ReturnsEmptySummary() {
		// Arrange
		_mockDriveRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);
		_mockRomFileRepo.Setup(r => r.GetTotalCountAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(0);

		// Act
		var summary = await _service.GetSummaryAsync();

		// Assert
		Assert.Equal(0, summary.TotalDrives);
		Assert.Equal(0, summary.OnlineDrives);
		Assert.Equal(0, summary.OfflineDrives);
		Assert.Equal(0, summary.TotalRoms);
	}

	[Fact]
	public async Task GetSummaryAsync_WithMixedDrives_ReturnCorrectCounts() {
		// Arrange
		var onlineDrive = CreateDrive("Online", true, 1000000000, 500000000);
		var offlineDrive = CreateDrive("Offline", false, null, null);

		_mockDriveRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync([onlineDrive, offlineDrive]);
		_mockRomFileRepo.Setup(r => r.GetTotalCountAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(100);
		_mockRomFileRepo.Setup(r => r.GetByDriveAsync(onlineDrive.Id, It.IsAny<CancellationToken>()))
			.ReturnsAsync(CreateRomFiles(60, onlineDrive.Id));
		_mockRomFileRepo.Setup(r => r.GetByDriveAsync(offlineDrive.Id, It.IsAny<CancellationToken>()))
			.ReturnsAsync(CreateRomFiles(40, offlineDrive.Id));

		// Act
		var summary = await _service.GetSummaryAsync();

		// Assert
		Assert.Equal(2, summary.TotalDrives);
		Assert.Equal(1, summary.OnlineDrives);
		Assert.Equal(1, summary.OfflineDrives);
		Assert.Equal(1000000000, summary.TotalCapacity);
		Assert.Equal(500000000, summary.TotalFreeSpace);
		Assert.Equal(60, summary.OnlineRoms);
		Assert.Equal(40, summary.OfflineRoms);
	}

	[Fact]
	public async Task GetDriveStatusAsync_WithValidId_ReturnsStatus() {
		// Arrange
		var drive = CreateDrive("Test Drive", true, 1000000000, 500000000);
		var romFiles = CreateRomFiles(10, drive.Id);

		_mockDriveRepo.Setup(r => r.GetByIdAsync(drive.Id, It.IsAny<CancellationToken>()))
			.ReturnsAsync(drive);
		_mockRomFileRepo.Setup(r => r.GetCountByDriveAsync(drive.Id, It.IsAny<CancellationToken>()))
			.ReturnsAsync(10);
		_mockRomFileRepo.Setup(r => r.GetByDriveAsync(drive.Id, It.IsAny<CancellationToken>()))
			.ReturnsAsync(romFiles);

		// Act
		var status = await _service.GetDriveStatusAsync(drive.Id);

		// Assert
		Assert.Equal(drive.Id, status.DriveId);
		Assert.Equal("Test Drive", status.Label);
		Assert.True(status.IsOnline);
		Assert.Equal(10, status.RomCount);
		Assert.Equal(10000, status.TotalRomSize); // 10 * 1000 bytes each
	}

	[Fact]
	public async Task GetDriveStatusAsync_WithInvalidId_ThrowsKeyNotFound() {
		// Arrange
		var id = Guid.NewGuid();
		_mockDriveRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
			.ReturnsAsync((Drive?)null);

		// Act & Assert
		await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.GetDriveStatusAsync(id));
	}

	[Fact]
	public async Task GetOfflineRomsAsync_WithOfflineDrives_ReturnsRoms() {
		// Arrange
		var onlineDrive = CreateDrive("Online", true);
		var offlineDrive = CreateDrive("Offline", false);
		var offlineRoms = CreateRomFiles(5, offlineDrive.Id);

		_mockDriveRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync([onlineDrive, offlineDrive]);
		_mockRomFileRepo.Setup(r => r.GetByDriveAsync(offlineDrive.Id, It.IsAny<CancellationToken>()))
			.ReturnsAsync(offlineRoms);

		// Act
		var result = await _service.GetOfflineRomsAsync();

		// Assert
		Assert.Equal(5, result.Count);
	}

	[Fact]
	public async Task GetOfflineRomsAsync_WithNoOfflineDrives_ReturnsEmpty() {
		// Arrange
		var onlineDrive = CreateDrive("Online", true);

		_mockDriveRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync([onlineDrive]);

		// Act
		var result = await _service.GetOfflineRomsAsync();

		// Assert
		Assert.Empty(result);
	}

	[Fact]
	public async Task FindDuplicatesAsync_WithDuplicates_ReturnsGroups() {
		// Arrange
		var drive1 = CreateDrive("Drive 1", true);
		var drive2 = CreateDrive("Drive 2", true);

		var rom1 = CreateRomFile(drive1.Id, "game.rom", "12345678", 1000);
		var rom2 = CreateRomFile(drive2.Id, "game_copy.rom", "12345678", 1000);
		var rom3 = CreateRomFile(drive1.Id, "other.rom", "abcdef00", 2000);

		_mockDriveRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync([drive1, drive2]);
		_mockRomFileRepo.Setup(r => r.GetByDriveAsync(drive1.Id, It.IsAny<CancellationToken>()))
			.ReturnsAsync([rom1, rom3]);
		_mockRomFileRepo.Setup(r => r.GetByDriveAsync(drive2.Id, It.IsAny<CancellationToken>()))
			.ReturnsAsync([rom2]);

		// Act
		var duplicates = await _service.FindDuplicatesAsync();

		// Assert
		Assert.Single(duplicates);
		Assert.Equal("12345678", duplicates[0].Crc);
		Assert.Equal(2, duplicates[0].Files.Count);
		Assert.Equal(1000, duplicates[0].WastedSpace);
	}

	[Fact]
	public async Task FindDuplicatesAsync_WithNoDuplicates_ReturnsEmpty() {
		// Arrange
		var drive = CreateDrive("Drive", true);
		var rom1 = CreateRomFile(drive.Id, "game1.rom", "11111111", 1000);
		var rom2 = CreateRomFile(drive.Id, "game2.rom", "22222222", 2000);

		_mockDriveRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync([drive]);
		_mockRomFileRepo.Setup(r => r.GetByDriveAsync(drive.Id, It.IsAny<CancellationToken>()))
			.ReturnsAsync([rom1, rom2]);

		// Act
		var duplicates = await _service.FindDuplicatesAsync();

		// Assert
		Assert.Empty(duplicates);
	}

	[Fact]
	public async Task GetRelocationSuggestionsAsync_WithValidDrives_ReturnsSuggestions() {
		// Arrange
		var sourceDrive = CreateDrive("Source", false);
		var targetDrive = CreateDrive("Target", true, 1000000000, 500000000);
		var rom = CreateRomFile(sourceDrive.Id, "game.rom", "12345678", 1000);

		_mockDriveRepo.Setup(r => r.GetByIdAsync(sourceDrive.Id, It.IsAny<CancellationToken>()))
			.ReturnsAsync(sourceDrive);
		_mockDriveRepo.Setup(r => r.GetOnlineAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync([targetDrive]);
		_mockRomFileRepo.Setup(r => r.GetByDriveAsync(sourceDrive.Id, It.IsAny<CancellationToken>()))
			.ReturnsAsync([rom]);
		_mockRomFileRepo.Setup(r => r.ExistsByPathAsync(targetDrive.Id, It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(false);

		// Act
		var suggestions = await _service.GetRelocationSuggestionsAsync(sourceDrive.Id);

		// Assert
		Assert.Single(suggestions);
		Assert.Equal("Source drive is offline", suggestions[0].Reason);
	}

	[Fact]
	public async Task GetRelocationSuggestionsAsync_WhenFileExistsOnTarget_SkipsSuggestion() {
		// Arrange
		var sourceDrive = CreateDrive("Source", true);
		var targetDrive = CreateDrive("Target", true, 1000000000, 500000000);
		var rom = CreateRomFile(sourceDrive.Id, "game.rom", "12345678", 1000);

		_mockDriveRepo.Setup(r => r.GetByIdAsync(sourceDrive.Id, It.IsAny<CancellationToken>()))
			.ReturnsAsync(sourceDrive);
		_mockDriveRepo.Setup(r => r.GetOnlineAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync([targetDrive]);
		_mockRomFileRepo.Setup(r => r.GetByDriveAsync(sourceDrive.Id, It.IsAny<CancellationToken>()))
			.ReturnsAsync([rom]);
		_mockRomFileRepo.Setup(r => r.ExistsByPathAsync(targetDrive.Id, rom.RelativePath, It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		// Act
		var suggestions = await _service.GetRelocationSuggestionsAsync(sourceDrive.Id);

		// Assert
		Assert.Empty(suggestions);
	}

	[Fact]
	public void DuplicateRomGroup_WastedSpace_CalculatesCorrectly() {
		// Arrange
		var drive = CreateDrive("Drive", true);
		var rom1 = CreateRomFile(drive.Id, "game1.rom", "12345678", 1000);
		var rom2 = CreateRomFile(drive.Id, "game2.rom", "12345678", 1000);
		var rom3 = CreateRomFile(drive.Id, "game3.rom", "12345678", 1000);

		var group = new DuplicateRomGroup("12345678", null, [rom1, rom2, rom3]);

		// Act
		var wastedSpace = group.WastedSpace;

		// Assert
		Assert.Equal(2000, wastedSpace); // 2 copies * 1000 bytes
	}

	private static Drive CreateDrive(string label, bool isOnline, long? totalSize = null, long? freeSpace = null) {
		var drive = Drive.Create(label, $@"C:\Test\{label}", DriveType.Fixed);
		if (isOnline) {
			drive.MarkOnline(totalSize, freeSpace);
		} else {
			drive.MarkOffline();
		}
		return drive;
	}

	private static List<RomFile> CreateRomFiles(int count, Guid driveId) {
		return Enumerable.Range(1, count)
			.Select(i => CreateRomFile(driveId, $"game{i}.rom", $"{i:x8}", 1000))
			.ToList();
	}

	private static RomFile CreateRomFile(Guid driveId, string fileName, string crc, long size) {
		var romFile = RomFile.Create(
			driveId,
			fileName,
			size,
			DateTime.UtcNow,
			false);
		// Set hashes so the CRC is populated for duplicate detection
		var hashes = RomHashes.Create(crc, crc.PadLeft(32, '0'), crc.PadLeft(40, '0'));
		romFile.SetHashes(hashes);
		return romFile;
	}
}
