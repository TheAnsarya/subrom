using Subrom.Application.Interfaces;
using Subrom.Infrastructure.Services;
using Subrom.Domain.ValueObjects;

namespace Subrom.Tests.Unit.Infrastructure.Services;

public class HashServiceTests : IDisposable {
	private readonly string _tempFilePath;
	private readonly HashService _hashService;

	public HashServiceTests() {
		_hashService = new HashService();
		_tempFilePath = Path.GetTempFileName();
	}

	[Fact]
	public async Task ComputeHashesAsync_EmptyFile_ReturnsKnownHashes() {
		// Arrange - empty file has known hashes
		await File.WriteAllBytesAsync(_tempFilePath, []);

		// Act
		var hashes = await _hashService.ComputeHashesAsync(_tempFilePath);

		// Assert
		Assert.Equal("00000000", hashes.Crc.Value);
		Assert.Equal("d41d8cd98f00b204e9800998ecf8427e", hashes.Md5.Value);
		Assert.Equal("da39a3ee5e6b4b0d3255bfef95601890afd80709", hashes.Sha1.Value);
	}

	[Fact]
	public async Task ComputeHashesAsync_HelloWorld_ReturnsKnownHashes() {
		// Arrange - "Hello, World!" has known test vector hashes
		await File.WriteAllTextAsync(_tempFilePath, "Hello, World!");

		// Act
		var hashes = await _hashService.ComputeHashesAsync(_tempFilePath);

		// Assert
		// Known test vectors for "Hello, World!"
		Assert.Equal("d0c34aec", hashes.Crc.Value);
		Assert.Equal("65a8e27d8879283831b664bd8b7f0ad4", hashes.Md5.Value);
		Assert.Equal("0a0a9f2a6772942557ab5355d76af442f8f65e01", hashes.Sha1.Value);
	}

	[Fact]
	public async Task ComputeHashesAsync_LargeFile_WithProgress_ReportsProgress() {
		// Arrange
		var data = new byte[1024 * 1024]; // 1MB
		new Random(42).NextBytes(data);
		await File.WriteAllBytesAsync(_tempFilePath, data);

		var progressReports = new List<HashProgress>();
		var progress = new Progress<HashProgress>(p => progressReports.Add(p));

		// Act
		var hashes = await _hashService.ComputeHashesAsync(_tempFilePath, progress);

		// Assert
		Assert.True(progressReports.Count > 0);
		Assert.Equal(data.Length, progressReports.Last().ProcessedBytes);
		Assert.Equal(data.Length, progressReports.Last().TotalBytes);
	}

	[Fact]
	public async Task ComputeHashesAsync_Stream_Success() {
		// Arrange
		await File.WriteAllTextAsync(_tempFilePath, "test content");
		await using var stream = File.OpenRead(_tempFilePath);

		// Act
		var hashes = await _hashService.ComputeHashesAsync(stream);

		// Assert
		Assert.NotEqual(default, hashes.Crc);
		Assert.NotEqual(default, hashes.Md5);
		Assert.NotEqual(default, hashes.Sha1);
	}

	[Fact]
	public async Task ComputeHashesAsync_FileNotFound_Throws() {
		// Arrange
		var nonExistentPath = Path.Combine(Path.GetTempPath(), "nonexistent_file_12345.dat");

		// Act & Assert
		await Assert.ThrowsAsync<FileNotFoundException>(() =>
			_hashService.ComputeHashesAsync(nonExistentPath));
	}

	public void Dispose() {
		if (File.Exists(_tempFilePath)) {
			File.Delete(_tempFilePath);
		}
	}
}
