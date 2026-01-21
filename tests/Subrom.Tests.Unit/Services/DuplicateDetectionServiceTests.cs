using Subrom.Application.Interfaces;
using Subrom.Domain.ValueObjects;
using Subrom.Infrastructure.Services;

namespace Subrom.Tests.Unit.Services;

/// <summary>
/// Unit tests for duplicate detection service.
/// </summary>
public sealed class DuplicateDetectionServiceTests {
	private readonly IDuplicateDetectionService _service;

	public DuplicateDetectionServiceTests() {
		_service = new DuplicateDetectionService();
	}

	// Pre-defined test hashes with correct lengths
	// CRC32 = 8 hex chars, MD5 = 32 hex chars, SHA1 = 40 hex chars
	private static RomHashes HashA => RomHashes.Create(
		"1234abcd",
		"1234567890abcdef1234567890abcdef",
		"1234567890abcdef1234567890abcdef12345678");

	private static RomHashes HashB => RomHashes.Create(
		"5678efab",
		"abcdef1234567890abcdef1234567890",
		"abcdef1234567890abcdef1234567890abcdef12");

	private static RomHashes HashC => RomHashes.Create(
		"9abcdef0",
		"fedcba0987654321fedcba0987654321",
		"fedcba0987654321fedcba0987654321fedcba09");

	private static RomHashes HashX => RomHashes.Create(
		"deadbeef",
		"deadbeefdeadbeefdeadbeefdeadbeef",
		"deadbeefdeadbeefdeadbeefdeadbeefdeadbeef");

	private static ScannedRomEntry CreateEntry(string name, RomHashes hashes, long size = 1024) =>
		new(Path: $"/roms/{name}", EntryPath: null, Hashes: hashes, Size: size, FileName: name);

	#region FindDuplicatesAsync Tests

	[Fact]
	public async Task FindDuplicatesAsync_WithNoDuplicates_ReturnsEmptyList() {
		// Arrange
		var entries = new[] {
			CreateEntry("game1.nes", HashA),
			CreateEntry("game2.nes", HashB),
			CreateEntry("game3.nes", HashC)
		};

		// Act
		var result = await _service.FindDuplicatesAsync(entries);

		// Assert
		Assert.Empty(result);
	}

	[Fact]
	public async Task FindDuplicatesAsync_WithDuplicates_ReturnsDuplicateGroups() {
		// Arrange
		var entries = new[] {
			CreateEntry("game1.nes", HashA),
			CreateEntry("game1_copy.nes", HashA),
			CreateEntry("game2.nes", HashB),
			CreateEntry("game2_backup.nes", HashB),
			CreateEntry("unique.nes", HashC)
		};

		// Act
		var result = await _service.FindDuplicatesAsync(entries);

		// Assert
		Assert.Equal(2, result.Count);
		Assert.All(result, g => Assert.Equal(2, g.Count));
	}

	[Fact]
	public async Task FindDuplicatesAsync_OrdersByWastedSpaceDescending() {
		// Arrange
		var entries = new[] {
			CreateEntry("small1.nes", HashA, size: 100),
			CreateEntry("small2.nes", HashA, size: 100),
			CreateEntry("large1.nes", HashB, size: 10000),
			CreateEntry("large2.nes", HashB, size: 10000)
		};

		// Act
		var result = await _service.FindDuplicatesAsync(entries);

		// Assert
		Assert.Equal(2, result.Count);
		Assert.True(result[0].WastedSpace > result[1].WastedSpace);
	}

	[Fact]
	public async Task FindDuplicatesAsync_CalculatesCorrectTotalSize() {
		// Arrange
		var entries = new[] {
			CreateEntry("game1.nes", HashA, size: 1000),
			CreateEntry("game2.nes", HashA, size: 1000),
			CreateEntry("game3.nes", HashA, size: 1000)
		};

		// Act
		var result = await _service.FindDuplicatesAsync(entries);

		// Assert
		Assert.Single(result);
		Assert.Equal(3000, result[0].TotalSize);
		Assert.Equal(2000, result[0].WastedSpace); // 3000 - 1000 (keeping one)
	}

	[Fact]
	public async Task FindDuplicatesAsync_WithEmptyCollection_ReturnsEmptyList() {
		// Arrange
		var entries = Array.Empty<ScannedRomEntry>();

		// Act
		var result = await _service.FindDuplicatesAsync(entries);

		// Assert
		Assert.Empty(result);
	}

	#endregion

	#region FindDuplicatesOfAsync Tests

	[Fact]
	public async Task FindDuplicatesOfAsync_FindsMatchingEntries() {
		// Arrange
		var entries = new[] {
			CreateEntry("game1.nes", HashA),
			CreateEntry("game2.nes", HashB),
			CreateEntry("game1_copy.nes", HashA)
		};

		// Act
		var result = await _service.FindDuplicatesOfAsync(HashA, entries);

		// Assert
		Assert.Equal(2, result.Count);
		Assert.All(result, e => Assert.Equal(HashA, e.Hashes));
	}

	[Fact]
	public async Task FindDuplicatesOfAsync_WithNoMatches_ReturnsEmptyList() {
		// Arrange
		var entries = new[] {
			CreateEntry("game1.nes", HashA),
			CreateEntry("game2.nes", HashB)
		};

		// Act
		var result = await _service.FindDuplicatesOfAsync(HashX, entries);

		// Assert
		Assert.Empty(result);
	}

	#endregion

	#region GroupByHashesAsync Tests

	[Fact]
	public async Task GroupByHashesAsync_GroupsEntriesByHash() {
		// Arrange
		var entries = new[] {
			CreateEntry("game1.nes", HashA),
			CreateEntry("game1_copy.nes", HashA),
			CreateEntry("game2.nes", HashB)
		};

		// Act
		var result = await _service.GroupByHashesAsync(entries);

		// Assert
		Assert.Equal(2, result.Count);
		Assert.Equal(2, result[HashA].Count);
		Assert.Single(result[HashB]);
	}

	[Fact]
	public async Task GroupByHashesAsync_WithEmptyCollection_ReturnsEmptyDictionary() {
		// Arrange
		var entries = Array.Empty<ScannedRomEntry>();

		// Act
		var result = await _service.GroupByHashesAsync(entries);

		// Assert
		Assert.Empty(result);
	}

	#endregion

	#region ScannedRomEntry Tests

	[Fact]
	public void ScannedRomEntry_IsArchived_ReturnsTrueForArchivedRom() {
		// Arrange
		var entry = new ScannedRomEntry(
			Path: "/roms/archive.zip",
			EntryPath: "game.nes",
			Hashes: HashA,
			Size: 1024,
			FileName: "game.nes");

		// Assert
		Assert.True(entry.IsArchived);
	}

	[Fact]
	public void ScannedRomEntry_IsArchived_ReturnsFalseForLooseFile() {
		// Arrange
		var entry = new ScannedRomEntry(
			Path: "/roms/game.nes",
			EntryPath: null,
			Hashes: HashA,
			Size: 1024,
			FileName: "game.nes");

		// Assert
		Assert.False(entry.IsArchived);
	}

	[Fact]
	public void ScannedRomEntry_DisplayLocation_FormatsCorrectlyForArchived() {
		// Arrange
		var entry = new ScannedRomEntry(
			Path: "/roms/archive.zip",
			EntryPath: "game.nes",
			Hashes: HashA,
			Size: 1024,
			FileName: "game.nes");

		// Assert
		Assert.Equal("archive.zip:game.nes", entry.DisplayLocation);
	}

	[Fact]
	public void ScannedRomEntry_DisplayLocation_FormatsCorrectlyForLooseFile() {
		// Arrange
		var entry = new ScannedRomEntry(
			Path: "/roms/game.nes",
			EntryPath: null,
			Hashes: HashA,
			Size: 1024,
			FileName: "game.nes");

		// Assert
		Assert.Equal("game.nes", entry.DisplayLocation);
	}

	#endregion

	#region DuplicateGroup Tests

	[Fact]
	public void DuplicateGroup_Count_ReturnsCorrectCount() {
		// Arrange
		var entries = new[] {
			CreateEntry("game1.nes", HashA),
			CreateEntry("game2.nes", HashA),
			CreateEntry("game3.nes", HashA)
		};
		var group = new DuplicateGroup(HashA, entries, 3000);

		// Assert
		Assert.Equal(3, group.Count);
	}

	[Fact]
	public void DuplicateGroup_WastedSpace_CalculatesCorrectly() {
		// Arrange
		var entries = new[] {
			CreateEntry("game1.nes", HashA, size: 1000),
			CreateEntry("game2.nes", HashA, size: 1000)
		};
		var group = new DuplicateGroup(HashA, entries, 2000);

		// Assert
		Assert.Equal(1000, group.WastedSpace); // 2000 - 1000 (keeping one)
	}

	#endregion
}
