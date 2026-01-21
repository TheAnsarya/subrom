using System.Diagnostics.CodeAnalysis;
using Subrom.Application.Interfaces;
using Subrom.Domain.Aggregates.DatFiles;
using Subrom.Domain.ValueObjects;
using Subrom.Infrastructure.Services;

namespace Subrom.Tests.Unit.Services;

public class BadDumpServiceTests {
	// Test hashes for mocking
	private static readonly RomHashes GoodHash = RomHashes.Create("11111111", "11111111111111111111111111111111", "1111111111111111111111111111111111111111");
	private static readonly RomHashes BadHash = RomHashes.Create("22222222", "22222222222222222222222222222222", "2222222222222222222222222222222222222222");
	private static readonly RomHashes UnknownHash = RomHashes.Create("99999999", "99999999999999999999999999999999", "9999999999999999999999999999999999999999");

	#region Filename Analysis Tests

	[Fact]
	public void AnalyzeFileName_WithVerifiedFlag_DetectsFlag() {
		// Arrange
		var repository = CreateMockRepository([]);
		var service = new BadDumpService(repository);

		// Act
		var result = service.AnalyzeFileName("Super Mario Bros (USA) [!].nes");

		// Assert
		Assert.True(result.Flags.HasFlag(FileNameFlags.Verified));
		Assert.True(result.IsVerified);
		Assert.False(result.HasConcerningFlags);
	}

	[Fact]
	public void AnalyzeFileName_WithBadDumpFlag_DetectsFlag() {
		// Arrange
		var repository = CreateMockRepository([]);
		var service = new BadDumpService(repository);

		// Act
		var result = service.AnalyzeFileName("Super Mario Bros (USA) [b].nes");

		// Assert
		Assert.True(result.Flags.HasFlag(FileNameFlags.BadDump));
		Assert.True(result.HasConcerningFlags);
	}

	[Theory]
	[InlineData("Game [b1].nes", 1)]
	[InlineData("Game [b2].nes", 2)]
	[InlineData("Game [b10].nes", 10)]
	public void AnalyzeFileName_WithNumberedBadDump_ExtractsVersion(string fileName, int expectedVersion) {
		// Arrange
		var repository = CreateMockRepository([]);
		var service = new BadDumpService(repository);

		// Act
		var result = service.AnalyzeFileName(fileName);

		// Assert
		Assert.True(result.Flags.HasFlag(FileNameFlags.BadDump));
		Assert.Equal(expectedVersion, result.BadDumpVersion);
	}

	[Theory]
	[InlineData("Game [a1].nes", 1)]
	[InlineData("Game [a2].nes", 2)]
	[InlineData("Game [a].nes", null)]
	public void AnalyzeFileName_WithAlternateFlag_ExtractsVersion(string fileName, int? expectedVersion) {
		// Arrange
		var repository = CreateMockRepository([]);
		var service = new BadDumpService(repository);

		// Act
		var result = service.AnalyzeFileName(fileName);

		// Assert
		Assert.True(result.Flags.HasFlag(FileNameFlags.Alternate));
		Assert.Equal(expectedVersion, result.AlternateVersion);
	}

	[Fact]
	public void AnalyzeFileName_WithOverdumpFlag_DetectsFlag() {
		// Arrange
		var repository = CreateMockRepository([]);
		var service = new BadDumpService(repository);

		// Act
		var result = service.AnalyzeFileName("Game [o1].nes");

		// Assert
		Assert.True(result.Flags.HasFlag(FileNameFlags.Overdump));
		Assert.True(result.HasConcerningFlags);
	}

	[Fact]
	public void AnalyzeFileName_WithHackFlag_DetectsFlag() {
		// Arrange
		var repository = CreateMockRepository([]);
		var service = new BadDumpService(repository);

		// Act
		var result = service.AnalyzeFileName("Game [h1C].nes");

		// Assert
		Assert.True(result.Flags.HasFlag(FileNameFlags.Hack));
		Assert.True(result.IsModified);
	}

	[Fact]
	public void AnalyzeFileName_WithTranslationFlag_DetectsFlag() {
		// Arrange
		var repository = CreateMockRepository([]);
		var service = new BadDumpService(repository);

		// Act
		var result = service.AnalyzeFileName("Game [T+Eng].nes");

		// Assert
		Assert.True(result.Flags.HasFlag(FileNameFlags.Translation));
		Assert.True(result.IsModified);
	}

	[Theory]
	[InlineData("Game (USA).nes", "USA")]
	[InlineData("Game (Europe).nes", "Europe")]
	[InlineData("Game (Japan).nes", "Japan")]
	[InlineData("Game (World).nes", "World")]
	[InlineData("Game (U).nes", "U")]
	[InlineData("Game (E).nes", "E")]
	[InlineData("Game (J).nes", "J")]
	[InlineData("Game (UE).nes", "UE")]
	public void AnalyzeFileName_WithRegion_ExtractsRegion(string fileName, string expectedRegion) {
		// Arrange
		var repository = CreateMockRepository([]);
		var service = new BadDumpService(repository);

		// Act
		var result = service.AnalyzeFileName(fileName);

		// Assert
		Assert.Equal(expectedRegion, result.Region);
	}

	[Theory]
	[InlineData("(Unl)", FileNameFlags.Unlicensed)]
	[InlineData("(Proto)", FileNameFlags.Prototype)]
	[InlineData("(Beta)", FileNameFlags.Beta)]
	[InlineData("(Sample)", FileNameFlags.Sample)]
	[InlineData("(Demo)", FileNameFlags.Demo)]
	[InlineData("(PD)", FileNameFlags.PublicDomain)]
	public void AnalyzeFileName_WithParentheticalFlags_DetectsFlags(string flag, FileNameFlags expectedFlag) {
		// Arrange
		var repository = CreateMockRepository([]);
		var service = new BadDumpService(repository);

		// Act
		var result = service.AnalyzeFileName($"Game {flag}.nes");

		// Assert
		Assert.True(result.Flags.HasFlag(expectedFlag));
	}

	[Fact]
	public void AnalyzeFileName_WithMultipleFlags_DetectsAllFlags() {
		// Arrange
		var repository = CreateMockRepository([]);
		var service = new BadDumpService(repository);

		// Act
		var result = service.AnalyzeFileName("Game (USA) [!] [T+Eng] (Unl).nes");

		// Assert
		Assert.True(result.Flags.HasFlag(FileNameFlags.Verified));
		Assert.True(result.Flags.HasFlag(FileNameFlags.Translation));
		Assert.True(result.Flags.HasFlag(FileNameFlags.Unlicensed));
		Assert.Equal("USA", result.Region);
	}

	[Fact]
	public void AnalyzeFileName_CleanName_RemovesFlags() {
		// Arrange
		var repository = CreateMockRepository([]);
		var service = new BadDumpService(repository);

		// Act
		var result = service.AnalyzeFileName("Super Mario Bros (USA) [!] [b1].nes");

		// Assert
		Assert.Equal("Super Mario Bros (USA)", result.CleanName);
	}

	[Fact]
	public void AnalyzeFileName_EmptyFileName_ReturnsEmptyResult() {
		// Arrange
		var repository = CreateMockRepository([]);
		var service = new BadDumpService(repository);

		// Act
		var result = service.AnalyzeFileName("");

		// Assert
		Assert.Equal(FileNameFlags.None, result.Flags);
		Assert.Equal("", result.CleanName);
	}

	#endregion

	#region Hash Check Tests

	[Fact]
	public async Task CheckByHashAsync_NoMatch_ReturnsUnknown() {
		// Arrange
		var repository = CreateMockRepository([]);
		var service = new BadDumpService(repository);

		// Act
		var result = await service.CheckByHashAsync(UnknownHash);

		// Assert
		Assert.True(result.IsUnknown);
		Assert.Equal(BadDumpSource.NoMatch, result.Source);
	}

	[Fact]
	public async Task CheckByHashAsync_GoodMatch_ReturnsGood() {
		// Arrange
		var datFile = CreateDatFile("Test DAT");
		var game = CreateGame("Test Game", datFile.Id);
		var rom = CreateRom("test.nes", GoodHash, RomStatus.Good);
		var matches = new List<DatRomMatch> { new(rom, game, datFile) };
		var repository = CreateMockRepository(matches);
		var service = new BadDumpService(repository);

		// Act
		var result = await service.CheckByHashAsync(GoodHash);

		// Assert
		Assert.True(result.IsGood);
		Assert.Equal(BadDumpSource.DatFile, result.Source);
		Assert.NotNull(result.MatchedRomEntry);
		Assert.NotNull(result.DatFile);
	}

	[Fact]
	public async Task CheckByHashAsync_BadDumpMatch_ReturnsBadDump() {
		// Arrange
		var datFile = CreateDatFile("Test DAT");
		var game = CreateGame("Test Game", datFile.Id);
		var rom = CreateRom("test.nes", BadHash, RomStatus.BadDump);
		var matches = new List<DatRomMatch> { new(rom, game, datFile) };
		var repository = CreateMockRepository(matches);
		var service = new BadDumpService(repository);

		// Act
		var result = await service.CheckByHashAsync(BadHash);

		// Assert
		Assert.True(result.IsBadDump);
		Assert.Equal(BadDumpSource.DatFile, result.Source);
	}

	[Fact]
	public async Task CheckByHashAsync_MultipleMatches_PrefersBadDump() {
		// Arrange
		var datFile = CreateDatFile("Test DAT");
		var game = CreateGame("Test Game", datFile.Id);
		var goodRom = CreateRom("good.nes", GoodHash, RomStatus.Good);
		var badRom = CreateRom("bad.nes", GoodHash, RomStatus.BadDump);
		var matches = new List<DatRomMatch> {
			new(goodRom, game, datFile),
			new(badRom, game, datFile)
		};
		var repository = CreateMockRepository(matches);
		var service = new BadDumpService(repository);

		// Act
		var result = await service.CheckByHashAsync(GoodHash);

		// Assert
		Assert.True(result.IsBadDump);
	}

	#endregion

	#region Batch Check Tests

	[Fact]
	public async Task CheckBatchAsync_EmptyInput_ReturnsEmptyDictionary() {
		// Arrange
		var repository = CreateMockRepository([]);
		var service = new BadDumpService(repository);

		// Act
		var result = await service.CheckBatchAsync([]);

		// Assert
		Assert.Empty(result);
	}

	[Fact]
	public async Task CheckBatchAsync_MixedResults_CorrectlyIdentifies() {
		// Arrange
		var datFile = CreateDatFile("Test DAT");
		var game = CreateGame("Test Game", datFile.Id);
		var goodRom = CreateRom("good.nes", GoodHash, RomStatus.Good);
		var badRom = CreateRom("bad.nes", BadHash, RomStatus.BadDump);
		var matches = new List<DatRomMatch> {
			new(goodRom, game, datFile),
			new(badRom, game, datFile)
		};
		var repository = CreateMockRepository(matches);
		var service = new BadDumpService(repository);

		var entries = new[] {
			new ScannedRomEntry("/path/good.nes", null, GoodHash, 1024, "good.nes"),
			new ScannedRomEntry("/path/bad.nes", null, BadHash, 1024, "bad.nes"),
			new ScannedRomEntry("/path/unknown.nes", null, UnknownHash, 1024, "unknown.nes")
		};

		// Act
		var result = await service.CheckBatchAsync(entries);

		// Assert
		Assert.Equal(3, result.Count);
		Assert.True(result[entries[0]].IsGood);
		Assert.True(result[entries[1]].IsBadDump);
		Assert.True(result[entries[2]].IsUnknown);
	}

	[Fact]
	public async Task CheckBatchAsync_FileNameAnalysis_IncludedInResults() {
		// Arrange
		var repository = CreateMockRepository([]);
		var service = new BadDumpService(repository);

		var entry = new ScannedRomEntry("/path/game [b].nes", null, UnknownHash, 1024, "game [b].nes");

		// Act
		var result = await service.CheckBatchAsync([entry]);

		// Assert
		Assert.True(result[entry].FileNameFlags.HasFlag(FileNameFlags.BadDump));
	}

	#endregion

	#region Find Bad Dumps Tests

	[Fact]
	public async Task FindBadDumpsAsync_ReturnsBadDumpsOnly() {
		// Arrange
		var datFile = CreateDatFile("Test DAT");
		var game = CreateGame("Test Game", datFile.Id);
		var goodRom = CreateRom("good.nes", GoodHash, RomStatus.Good);
		var badRom = CreateRom("bad.nes", BadHash, RomStatus.BadDump);
		var matches = new List<DatRomMatch> {
			new(goodRom, game, datFile),
			new(badRom, game, datFile)
		};
		var repository = CreateMockRepository(matches);
		var service = new BadDumpService(repository);

		var entries = new[] {
			new ScannedRomEntry("/path/good.nes", null, GoodHash, 1024, "good.nes"),
			new ScannedRomEntry("/path/bad.nes", null, BadHash, 1024, "bad.nes"),
		};

		// Act
		var result = await service.FindBadDumpsAsync(entries);

		// Assert
		Assert.Single(result);
		Assert.Equal("bad.nes", result[0].Entry.FileName);
	}

	[Fact]
	public async Task FindBadDumpsAsync_IncludesFileNameBadDumps() {
		// Arrange
		var repository = CreateMockRepository([]);
		var service = new BadDumpService(repository);

		var entries = new[] {
			new ScannedRomEntry("/path/good.nes", null, UnknownHash, 1024, "good.nes"),
			new ScannedRomEntry("/path/bad [b].nes", null, UnknownHash, 1024, "bad [b].nes"),
		};

		// Act
		var result = await service.FindBadDumpsAsync(entries);

		// Assert
		Assert.Single(result);
		Assert.Equal("bad [b].nes", result[0].Entry.FileName);
	}

	#endregion

	#region BadDumpEntry Reason Tests

	[Fact]
	public void BadDumpEntry_ReasonFromDatFile_ShowsDatName() {
		// Arrange
		var datFile = CreateDatFile("No-Intro NES");
		var game = CreateGame("Test Game", datFile.Id);
		var rom = CreateRom("bad.nes", BadHash, RomStatus.BadDump);
		var result = BadDumpResult.FromDatMatch(rom, game, datFile);
		var entry = new ScannedRomEntry("/path/bad.nes", null, BadHash, 1024, "bad.nes");

		// Act
		var badDumpEntry = new BadDumpEntry(entry, result);

		// Assert
		Assert.Contains("No-Intro NES", badDumpEntry.Reason);
	}

	[Fact]
	public void BadDumpEntry_ReasonFromFileName_ShowsFlags() {
		// Arrange
		var result = BadDumpResult.FromFileName(FileNameFlags.BadDump);
		var entry = new ScannedRomEntry("/path/bad.nes", null, UnknownHash, 1024, "bad [b].nes");

		// Act
		var badDumpEntry = new BadDumpEntry(entry, result);

		// Assert
		Assert.Contains("flag", badDumpEntry.Reason, StringComparison.OrdinalIgnoreCase);
	}

	#endregion

	#region Helper Methods

	private static IDatFileRepository CreateMockRepository(List<DatRomMatch> matches) {
		return new MockDatFileRepository(matches);
	}

	private static DatFile CreateDatFile(string name) =>
		new TestDatFile(name);

	private static GameEntry CreateGame(string name, Guid datFileId) =>
		new TestGameEntry(name, datFileId);

	private static RomEntry CreateRom(string name, RomHashes hashes, RomStatus status) =>
		new TestRomEntry(name, hashes, status);

	/// <summary>
	/// Test subclass to set protected Id property.
	/// </summary>
	private sealed class TestDatFile : DatFile {
		[SetsRequiredMembers]
		public TestDatFile(string name) {
			Id = Guid.NewGuid();
			FileName = name + ".dat";
			Name = name;
			Description = name;
			Version = "1.0";
			Format = DatFormat.LogiqxXml;
			Provider = DatProvider.NoIntro;
		}
	}

	/// <summary>
	/// Test subclass to set protected Id property.
	/// </summary>
	private sealed class TestGameEntry : GameEntry {
		[SetsRequiredMembers]
		public TestGameEntry(string name, Guid datFileId) {
			Id = 1;
			Name = name;
			Description = name;
			DatFileId = datFileId;
		}
	}

	/// <summary>
	/// Test subclass to set protected Id property.
	/// </summary>
	private sealed class TestRomEntry : RomEntry {
		[SetsRequiredMembers]
		public TestRomEntry(string name, RomHashes hashes, RomStatus status) {
			Id = 1;
			Name = name;
			Size = 1024;
			Crc = hashes.Crc.Value;
			Md5 = hashes.Md5.Value;
			Sha1 = hashes.Sha1.Value;
			Status = status;
			GameId = 1;
		}
	}

	/// <summary>
	/// Minimal mock implementation for testing.
	/// </summary>
	private sealed class MockDatFileRepository : IDatFileRepository {
		private readonly List<DatRomMatch> _matches;

		public MockDatFileRepository(List<DatRomMatch> matches) {
			_matches = matches;
		}

		public Task<IReadOnlyList<DatRomMatch>> FindRomsByHashAsync(RomHashes hashes, CancellationToken cancellationToken = default) {
			var results = _matches.Where(m =>
				m.RomEntry.Crc == hashes.Crc.Value ||
				m.RomEntry.Md5 == hashes.Md5.Value ||
				m.RomEntry.Sha1 == hashes.Sha1.Value
			).ToList();

			return Task.FromResult<IReadOnlyList<DatRomMatch>>(results);
		}

		public Task<IReadOnlyList<DatRomMatch>> FindRomsByHashesAsync(IEnumerable<RomHashes> hashes, CancellationToken cancellationToken = default) {
			var hashList = hashes.ToList();
			var results = _matches.Where(m =>
				hashList.Any(h =>
					m.RomEntry.Crc == h.Crc.Value ||
					m.RomEntry.Md5 == h.Md5.Value ||
					m.RomEntry.Sha1 == h.Sha1.Value
				)
			).ToList();

			return Task.FromResult<IReadOnlyList<DatRomMatch>>(results);
		}

		// Not used in tests - just satisfy interface
		public Task<DatFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<DatFile?>(null);
		public Task<DatFile?> GetByIdWithGamesAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<DatFile?>(null);
		public Task<IReadOnlyList<DatFile>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<DatFile>>([]);
		public Task<IReadOnlyList<DatFile>> GetByCategoryAsync(string categoryPath, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<DatFile>>([]);
		public Task<IReadOnlyList<DatFile>> GetByProviderAsync(DatProvider provider, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<DatFile>>([]);
		public Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default) => Task.FromResult(false);
		public Task AddAsync(DatFile datFile, CancellationToken cancellationToken = default) => Task.CompletedTask;
		public Task UpdateAsync(DatFile datFile, CancellationToken cancellationToken = default) => Task.CompletedTask;
		public Task RemoveAsync(DatFile datFile, CancellationToken cancellationToken = default) => Task.CompletedTask;
		public Task<int> GetCountAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
		public Task<IReadOnlyList<string>> GetCategoryPathsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<string>>([]);
	}

	#endregion
}
