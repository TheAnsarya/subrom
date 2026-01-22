using Moq;
using Subrom.Application.Interfaces;
using Subrom.Application.Services;
using Subrom.Domain.Aggregates.DatFiles;
using Subrom.Domain.Aggregates.Storage;
using Subrom.Domain.ValueObjects;
using AppMatchType = Subrom.Application.Services.MatchType;

namespace Subrom.Tests.Unit.Application;

/// <summary>
/// Unit tests for VerificationService.
/// </summary>
public class VerificationServiceTests {
	private readonly Mock<IDatFileRepository> _datFileRepoMock;
	private readonly Mock<IRomFileRepository> _romFileRepoMock;
	private readonly Mock<IHashService> _hashServiceMock;
	private readonly Mock<IUnitOfWork> _unitOfWorkMock;
	private readonly VerificationService _service;

	public VerificationServiceTests() {
		_datFileRepoMock = new Mock<IDatFileRepository>();
		_romFileRepoMock = new Mock<IRomFileRepository>();
		_hashServiceMock = new Mock<IHashService>();
		_unitOfWorkMock = new Mock<IUnitOfWork>();

		_service = new VerificationService(
			_datFileRepoMock.Object,
			_romFileRepoMock.Object,
			_hashServiceMock.Object,
			_unitOfWorkMock.Object);
	}

	[Fact]
	public async Task LookupByHashesAsync_WithMatchingSha1_ReturnsMatch() {
		// Arrange
		var hashes = new RomHashes(
			Crc.Create("12345678"),
			Md5.Create("12345678901234567890123456789012"),
			Sha1.Create("1234567890123456789012345678901234567890"));

		var datFile = CreateDatFileWithGame("Test DAT", "Test Game", "test.rom", hashes);
		_datFileRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(new List<DatFile> { datFile });
		_datFileRepoMock.Setup(r => r.GetByIdWithGamesAsync(datFile.Id, It.IsAny<CancellationToken>()))
			.ReturnsAsync(datFile);

		// Act
		var result = await _service.LookupByHashesAsync(hashes);

		// Assert
		Assert.NotNull(result);
		Assert.Equal("Test DAT", result.DatFileName);
		Assert.Equal("Test Game", result.GameName);
		Assert.Equal("test.rom", result.RomName);
		Assert.Equal(AppMatchType.Sha1, result.MatchType);
	}

	[Fact]
	public async Task LookupByHashesAsync_WithMatchingCrc_ReturnsMatch() {
		// Arrange
		var hashes = new RomHashes(
			Crc.Create("12345678"),
			Md5.Create("00000000000000000000000000000000"),
			Sha1.Create("0000000000000000000000000000000000000000"));

		var datHashes = new RomHashes(
			Crc.Create("12345678"),
			Md5.Create("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
			Sha1.Create("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"));

		var datFile = CreateDatFileWithGame("Test DAT", "Test Game", "test.rom", datHashes);
		_datFileRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(new List<DatFile> { datFile });
		_datFileRepoMock.Setup(r => r.GetByIdWithGamesAsync(datFile.Id, It.IsAny<CancellationToken>()))
			.ReturnsAsync(datFile);

		// Act
		var result = await _service.LookupByHashesAsync(hashes);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(AppMatchType.Crc32, result.MatchType);
	}

	[Fact]
	public async Task LookupByHashesAsync_WithNoMatch_ReturnsNull() {
		// Arrange
		var hashes = new RomHashes(
			Crc.Create("ffffffff"),
			Md5.Create("ffffffffffffffffffffffffffffffff"),
			Sha1.Create("ffffffffffffffffffffffffffffffffffffffff"));

		var datHashes = new RomHashes(
			Crc.Create("12345678"),
			Md5.Create("12345678901234567890123456789012"),
			Sha1.Create("1234567890123456789012345678901234567890"));

		var datFile = CreateDatFileWithGame("Test DAT", "Test Game", "test.rom", datHashes);
		_datFileRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(new List<DatFile> { datFile });
		_datFileRepoMock.Setup(r => r.GetByIdWithGamesAsync(datFile.Id, It.IsAny<CancellationToken>()))
			.ReturnsAsync(datFile);

		// Act
		var result = await _service.LookupByHashesAsync(hashes);

		// Assert
		Assert.Null(result);
	}

	[Fact]
	public async Task LookupByHashesAsync_SkipsDisabledDatFiles() {
		// Arrange
		var hashes = new RomHashes(
			Crc.Create("12345678"),
			Md5.Create("12345678901234567890123456789012"),
			Sha1.Create("1234567890123456789012345678901234567890"));

		var datFile = CreateDatFileWithGame("Test DAT", "Test Game", "test.rom", hashes);
		datFile.IsEnabled = false;

		_datFileRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(new List<DatFile> { datFile });

		// Act
		var result = await _service.LookupByHashesAsync(hashes);

		// Assert
		Assert.Null(result);
		_datFileRepoMock.Verify(r => r.GetByIdWithGamesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task VerifyRomFileAsync_WithHashedFile_ReturnsResult() {
		// Arrange
		var romFileId = Guid.NewGuid();
		var hashes = new RomHashes(
			Crc.Create("12345678"),
			Md5.Create("12345678901234567890123456789012"),
			Sha1.Create("1234567890123456789012345678901234567890"));

		var romFile = CreateRomFile(hashes);
		var datFile = CreateDatFileWithGame("Test DAT", "Test Game", "test.rom", hashes);

		_romFileRepoMock.Setup(r => r.GetByIdAsync(romFileId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(romFile);
		_datFileRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(new List<DatFile> { datFile });
		_datFileRepoMock.Setup(r => r.GetByIdWithGamesAsync(datFile.Id, It.IsAny<CancellationToken>()))
			.ReturnsAsync(datFile);

		// Act
		var result = await _service.VerifyRomFileAsync(romFileId);

		// Assert
		Assert.NotNull(result);
		Assert.True(result.IsMatch);
		Assert.Equal("Test Game", result.Match!.GameName);
	}

	[Fact]
	public async Task VerifyRomFileAsync_WithNonExistentFile_ThrowsKeyNotFoundException() {
		// Arrange
		var romFileId = Guid.NewGuid();
		_romFileRepoMock.Setup(r => r.GetByIdAsync(romFileId, It.IsAny<CancellationToken>()))
			.ReturnsAsync((RomFile?)null);

		// Act & Assert
		await Assert.ThrowsAsync<KeyNotFoundException>(
			() => _service.VerifyRomFileAsync(romFileId));
	}

	[Fact]
	public async Task VerifyRomFileAsync_WithUnhashedFile_ThrowsInvalidOperationException() {
		// Arrange
		var romFileId = Guid.NewGuid();
		var romFile = RomFile.Create(
			driveId: Guid.NewGuid(),
			relativePath: "test.rom",
			size: 1024,
			lastModified: DateTime.UtcNow);

		_romFileRepoMock.Setup(r => r.GetByIdAsync(romFileId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(romFile);

		// Act & Assert
		await Assert.ThrowsAsync<InvalidOperationException>(
			() => _service.VerifyRomFileAsync(romFileId));
	}

	[Fact]
	public async Task VerifyBatchAsync_VerifiesAllFiles() {
		// Arrange
		var hashes = new RomHashes(
			Crc.Create("12345678"),
			Md5.Create("12345678901234567890123456789012"),
			Sha1.Create("1234567890123456789012345678901234567890"));

		var romFile1 = CreateRomFile(hashes);
		var romFile2 = CreateRomFile(hashes);
		var datFile = CreateDatFileWithGame("Test DAT", "Test Game", "test.rom", hashes);

		_romFileRepoMock.Setup(r => r.GetByIdAsync(romFile1.Id, It.IsAny<CancellationToken>()))
			.ReturnsAsync(romFile1);
		_romFileRepoMock.Setup(r => r.GetByIdAsync(romFile2.Id, It.IsAny<CancellationToken>()))
			.ReturnsAsync(romFile2);
		_datFileRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(new List<DatFile> { datFile });
		_datFileRepoMock.Setup(r => r.GetByIdWithGamesAsync(datFile.Id, It.IsAny<CancellationToken>()))
			.ReturnsAsync(datFile);

		var ids = new[] { romFile1.Id, romFile2.Id };

		// Act
		var results = await _service.VerifyBatchAsync(ids);

		// Assert
		Assert.Equal(2, results.Count);
		Assert.All(results, r => Assert.True(r.IsMatch));
	}

	[Fact]
	public async Task VerifyBatchAsync_ReportsProgress() {
		// Arrange
		var hashes = new RomHashes(
			Crc.Create("12345678"),
			Md5.Create("12345678901234567890123456789012"),
			Sha1.Create("1234567890123456789012345678901234567890"));

		var romFile = CreateRomFile(hashes);
		var datFile = CreateDatFileWithGame("Test DAT", "Test Game", "test.rom", hashes);

		_romFileRepoMock.Setup(r => r.GetByIdAsync(romFile.Id, It.IsAny<CancellationToken>()))
			.ReturnsAsync(romFile);
		_datFileRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(new List<DatFile> { datFile });
		_datFileRepoMock.Setup(r => r.GetByIdWithGamesAsync(datFile.Id, It.IsAny<CancellationToken>()))
			.ReturnsAsync(datFile);

		var progressReports = new List<BatchVerificationProgress>();
		var progress = new Progress<BatchVerificationProgress>(p => progressReports.Add(p));

		// Act
		await _service.VerifyBatchAsync(new[] { romFile.Id }, progress);

		// Wait for progress to be reported
		await Task.Delay(50);

		// Assert
		Assert.NotEmpty(progressReports);
		Assert.Equal(1, progressReports[0].Completed);
		Assert.Equal(1, progressReports[0].Total);
	}

	[Fact]
	public async Task VerifyBatchAsync_ContinuesOnError() {
		// Arrange
		var goodId = Guid.NewGuid();
		var badId = Guid.NewGuid();

		var hashes = new RomHashes(
			Crc.Create("12345678"),
			Md5.Create("12345678901234567890123456789012"),
			Sha1.Create("1234567890123456789012345678901234567890"));

		var goodFile = CreateRomFile(hashes);
		var datFile = CreateDatFileWithGame("Test DAT", "Test Game", "test.rom", hashes);

		_romFileRepoMock.Setup(r => r.GetByIdAsync(goodFile.Id, It.IsAny<CancellationToken>()))
			.ReturnsAsync(goodFile);
		_romFileRepoMock.Setup(r => r.GetByIdAsync(badId, It.IsAny<CancellationToken>()))
			.ReturnsAsync((RomFile?)null);
		_datFileRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(new List<DatFile> { datFile });
		_datFileRepoMock.Setup(r => r.GetByIdWithGamesAsync(datFile.Id, It.IsAny<CancellationToken>()))
			.ReturnsAsync(datFile);

		// Act
		var results = await _service.VerifyBatchAsync(new[] { badId, goodFile.Id });

		// Assert
		Assert.Equal(2, results.Count);
		Assert.NotNull(results[0].Error);
		Assert.Null(results[1].Error);
	}

	[Fact]
	public async Task LookupByHashesAsync_WithMultipleDats_FindsFirstMatch() {
		// Arrange
		var hashes = new RomHashes(
			Crc.Create("12345678"),
			Md5.Create("12345678901234567890123456789012"),
			Sha1.Create("1234567890123456789012345678901234567890"));

		var datFile1 = CreateDatFileWithGame("DAT 1", "Game 1", "rom1.rom", hashes);
		var datFile2 = CreateDatFileWithGame("DAT 2", "Game 2", "rom2.rom", hashes);

		_datFileRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(new List<DatFile> { datFile1, datFile2 });
		_datFileRepoMock.Setup(r => r.GetByIdWithGamesAsync(datFile1.Id, It.IsAny<CancellationToken>()))
			.ReturnsAsync(datFile1);

		// Act
		var result = await _service.LookupByHashesAsync(hashes);

		// Assert
		Assert.NotNull(result);
		Assert.Equal("DAT 1", result.DatFileName);
		Assert.Equal("Game 1", result.GameName);
	}

	// Helper methods
	private static DatFile CreateDatFileWithGame(string datName, string gameName, string romName, RomHashes hashes) {
		var datFile = new DatFile {
			FileName = $"{datName}.dat",
			Name = datName,
			Provider = DatProvider.NoIntro,
			Format = DatFormat.LogiqxXml,
			IsEnabled = true
		};

		var game = new GameEntry {
			Name = gameName,
			Description = gameName,
			DatFileId = datFile.Id
		};

		var rom = new RomEntry {
			Name = romName,
			Size = 1024,
			Crc = hashes.Crc.Value,
			Md5 = hashes.Md5.Value,
			Sha1 = hashes.Sha1.Value,
			GameId = 1
		};

		game.AddRom(rom);
		datFile.AddGame(game);

		return datFile;
	}

	private static RomFile CreateRomFile(RomHashes hashes) {
		var romFile = RomFile.Create(
			driveId: Guid.NewGuid(),
			relativePath: "test.rom",
			size: 1024,
			lastModified: DateTime.UtcNow);
		romFile.SetHashes(hashes);
		return romFile;
	}
}
