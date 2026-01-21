using Subrom.Application.Interfaces;
using Subrom.Infrastructure.Services;

namespace Subrom.Tests.Unit.Services;

/// <summary>
/// Unit tests for ROM header detection service.
/// Tests detection of various copier headers (iNES, SMC, SMD, etc.)
/// </summary>
public sealed class RomHeaderServiceTests : IDisposable {
	private readonly IRomHeaderService _service;
	private readonly List<string> _tempFiles = [];

	public RomHeaderServiceTests() {
		_service = new RomHeaderService();
	}

	public void Dispose() {
		foreach (var file in _tempFiles) {
			try { File.Delete(file); } catch { }
		}
	}

	private async Task<string> CreateTempFileAsync(byte[] content) {
		var path = Path.GetTempFileName();
		_tempFiles.Add(path);
		await File.WriteAllBytesAsync(path, content);
		return path;
	}

	#region iNES Header Tests

	[Fact]
	public async Task DetectHeaderAsync_WithINesHeader_ReturnsCorrectInfo() {
		// Arrange: iNES header (NES\x1A)
		var content = new byte[16400];
		content[0] = 0x4e; // 'N'
		content[1] = 0x45; // 'E'
		content[2] = 0x53; // 'S'
		content[3] = 0x1a; // EOF marker
		content[4] = 0x02; // PRG ROM size
		content[5] = 0x01; // CHR ROM size
		content[7] = 0x00; // NES 1.0 format (bits 2-3 of byte 7 are 00)

		using var stream = new MemoryStream(content);

		// Act
		var result = await _service.DetectHeaderAsync(stream, ".nes");

		// Assert
		Assert.NotNull(result);
		Assert.Equal(16, result.HeaderSize);
		Assert.Equal("iNES", result.Format);
	}

	[Fact]
	public async Task DetectHeaderAsync_WithNes20Header_ReturnsCorrectInfo() {
		// Arrange: NES 2.0 header (has bits 2-3 of byte 7 set to 10)
		var content = new byte[16400];
		content[0] = 0x4e; // 'N'
		content[1] = 0x45; // 'E'
		content[2] = 0x53; // 'S'
		content[3] = 0x1a; // EOF marker
		content[4] = 0x02; // PRG ROM size
		content[5] = 0x01; // CHR ROM size
		content[7] = 0x08; // NES 2.0 format (bits 2-3 = 10 binary = 0x08)

		using var stream = new MemoryStream(content);

		// Act
		var result = await _service.DetectHeaderAsync(stream, ".nes");

		// Assert
		Assert.NotNull(result);
		Assert.Equal(16, result.HeaderSize);
		Assert.Equal("NES 2.0", result.Format);
	}

	[Fact]
	public async Task DetectHeaderAsync_WithoutINesHeader_ReturnsNull() {
		// Arrange: NES file without header
		var content = new byte[8192];
		content[0] = 0xff;
		content[1] = 0xff;

		using var stream = new MemoryStream(content);

		// Act
		var result = await _service.DetectHeaderAsync(stream, ".nes");

		// Assert
		Assert.Null(result);
	}

	#endregion

	#region FDS Header Tests

	[Fact]
	public async Task DetectHeaderAsync_WithFdsHeader_ReturnsCorrectInfo() {
		// Arrange: FDS header (FDS\x1A)
		var content = new byte[16400];
		content[0] = 0x46; // 'F'
		content[1] = 0x44; // 'D'
		content[2] = 0x53; // 'S'
		content[3] = 0x1a; // EOF marker
		content[4] = 0x02; // Side count

		using var stream = new MemoryStream(content);

		// Act
		var result = await _service.DetectHeaderAsync(stream, ".fds");

		// Assert
		Assert.NotNull(result);
		Assert.Equal(16, result.HeaderSize);
		Assert.Equal("fwNES FDS", result.Format); // Actual format name from implementation
	}

	#endregion

	#region SNES Header Tests

	[Fact]
	public async Task DetectHeaderAsync_WithSmcHeader_ReturnsCorrectInfo() {
		// Arrange: SMC header (512 bytes, file size = power of 2 + 512)
		// 32KB ROM + 512 byte header = 33280 bytes (32768 + 512)
		var content = new byte[32768 + 512];

		using var stream = new MemoryStream(content);

		// Act
		var result = await _service.DetectHeaderAsync(stream, ".smc");

		// Assert
		Assert.NotNull(result);
		Assert.Equal(512, result.HeaderSize);
		Assert.Equal("Super MagiCom", result.Format); // Actual format name
	}

	[Fact]
	public async Task DetectHeaderAsync_WithSwcHeader_ReturnsCorrectInfo() {
		// Arrange: SWC header (512 bytes)
		var content = new byte[32768 + 512];

		using var stream = new MemoryStream(content);

		// Act
		var result = await _service.DetectHeaderAsync(stream, ".swc");

		// Assert
		Assert.NotNull(result);
		Assert.Equal(512, result.HeaderSize);
		Assert.Equal("Super Wild Card", result.Format);
	}

	[Fact]
	public async Task DetectHeaderAsync_SfcWithHeader_DetectsHeader() {
		// Arrange: SFC file that happens to have a header (size = ROM + 512)
		var content = new byte[32768 + 512];

		using var stream = new MemoryStream(content);

		// Act
		var result = await _service.DetectHeaderAsync(stream, ".sfc");

		// Assert
		Assert.NotNull(result);
		Assert.Equal(512, result.HeaderSize);
	}

	[Fact]
	public async Task DetectHeaderAsync_SnesWithoutHeader_ReturnsNull() {
		// Arrange: SNES ROM without copier header (clean dump, power of 2 size)
		var content = new byte[32768]; // Exactly 32KB

		using var stream = new MemoryStream(content);

		// Act
		var result = await _service.DetectHeaderAsync(stream, ".sfc");

		// Assert
		Assert.Null(result);
	}

	#endregion

	#region Genesis/Mega Drive Header Tests

	[Fact]
	public async Task DetectHeaderAsync_WithSmdHeader_ReturnsCorrectInfo() {
		// Arrange: SMD header (512 bytes)
		// SMD detection checks block count (bytes 0-1) against file size
		var romSize = 32768; // 2 blocks of 16384
		var content = new byte[romSize + 512];
		// Set block count = 2 in little-endian (matches romSize / 16384)
		content[0] = 0x02; // Low byte of block count
		content[1] = 0x00; // High byte of block count
		content[2] = 0x00; // ROM type

		using var stream = new MemoryStream(content);

		// Act
		var result = await _service.DetectHeaderAsync(stream, ".smd");

		// Assert
		Assert.NotNull(result);
		Assert.Equal(512, result.HeaderSize);
		Assert.Equal("Super Magic Drive", result.Format);
	}

	[Fact]
	public async Task DetectHeaderAsync_GenesisMdWithoutHeader_ReturnsNull() {
		// Arrange: Genesis ROM without SMD header (clean dump)
		var content = new byte[32768]; // Exactly power of 2

		using var stream = new MemoryStream(content);

		// Act
		var result = await _service.DetectHeaderAsync(stream, ".md");

		// Assert
		Assert.Null(result); // .md extension doesn't trigger SMD detection
	}

	#endregion

	#region Atari Header Tests

	[Fact]
	public async Task DetectHeaderAsync_WithA78Header_ReturnsCorrectInfo() {
		// Arrange: A78 header (128 bytes with ATARI7800 signature at offset 1)
		var content = new byte[16384 + 128];
		// A78 signature: "ATARI7800" at offset 1 (after version byte)
		content[0] = 0x03; // Version
		content[1] = (byte)'A';
		content[2] = (byte)'T';
		content[3] = (byte)'A';
		content[4] = (byte)'R';
		content[5] = (byte)'I';
		content[6] = (byte)'7';
		content[7] = (byte)'8';
		content[8] = (byte)'0';
		content[9] = (byte)'0';

		using var stream = new MemoryStream(content);

		// Act
		var result = await _service.DetectHeaderAsync(stream, ".a78");

		// Assert
		Assert.NotNull(result);
		Assert.Equal(128, result.HeaderSize);
		Assert.Equal("A78", result.Format);
	}

	[Fact]
	public async Task DetectHeaderAsync_WithLnxHeader_ReturnsCorrectInfo() {
		// Arrange: LNX header (64 bytes with LYNX signature)
		var content = new byte[16384 + 64];
		content[0] = (byte)'L';
		content[1] = (byte)'Y';
		content[2] = (byte)'N';
		content[3] = (byte)'X';

		using var stream = new MemoryStream(content);

		// Act
		var result = await _service.DetectHeaderAsync(stream, ".lnx");

		// Assert
		Assert.NotNull(result);
		Assert.Equal(64, result.HeaderSize);
		Assert.Equal("LNX", result.Format);
	}

	#endregion

	#region SupportsFormat Tests

	[Theory]
	[InlineData(".nes", true)]
	[InlineData(".fds", true)]
	[InlineData(".sfc", true)]
	[InlineData(".smc", true)]
	[InlineData(".swc", true)]
	[InlineData(".fig", true)]
	[InlineData(".md", true)]
	[InlineData(".smd", true)]
	[InlineData(".gen", true)]
	[InlineData(".a78", true)]
	[InlineData(".lnx", true)]
	[InlineData(".gb", true)]  // GB is supported (just has 0 header size)
	[InlineData(".gba", true)] // GBA is supported (just has 0 header size)
	[InlineData(".iso", false)]
	[InlineData(".txt", false)]
	public void SupportsFormat_ReturnsCorrectValue(string extension, bool expected) {
		// Act
		var result = _service.SupportsFormat(extension);

		// Assert
		Assert.Equal(expected, result);
	}

	#endregion

	#region GetStandardHeaderSize Tests

	[Theory]
	[InlineData(".nes", 16)]
	[InlineData(".fds", 16)]
	[InlineData(".sfc", 0)]    // SFC is headerless by default
	[InlineData(".smc", 512)]
	[InlineData(".md", 0)]     // MD is headerless (.smd has headers)
	[InlineData(".smd", 512)]
	[InlineData(".a78", 128)]
	[InlineData(".lnx", 64)]
	public void GetStandardHeaderSize_ReturnsCorrectSize(string extension, int expectedSize) {
		// Act
		var size = _service.GetStandardHeaderSize(extension);

		// Assert
		Assert.Equal(expectedSize, size);
	}

	[Fact]
	public void GetStandardHeaderSize_UnsupportedFormat_ReturnsZero() {
		// Act
		var size = _service.GetStandardHeaderSize(".txt");

		// Assert
		Assert.Equal(0, size);
	}

	#endregion

	#region Stream Position Tests

	[Fact]
	public async Task DetectHeaderAsync_ResetsStreamPosition() {
		// Arrange
		var content = new byte[16400];
		content[0] = 0x4e;
		content[1] = 0x45;
		content[2] = 0x53;
		content[3] = 0x1a;

		using var stream = new MemoryStream(content);
		stream.Position = 100; // Set initial position

		// Act
		await _service.DetectHeaderAsync(stream, ".nes");

		// Assert: Stream should be reset to original position
		Assert.Equal(100, stream.Position);
	}

	#endregion
}
