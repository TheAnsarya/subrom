using Subrom.Domain.ValueObjects;

namespace Subrom.Tests.Unit.Domain.ValueObjects;

public class HashesTests {
	[Fact]
	public void Crc_Create_ValidHex_Success() {
		// Arrange & Act
		var crc = Crc.Create("cafebabe");

		// Assert
		Assert.Equal("cafebabe", crc.Value);
	}

	[Theory]
	[InlineData("00000000")]
	[InlineData("ffffffff")]
	[InlineData("12ab34cd")]
	public void Crc_Create_ValidFormats_Success(string hex) {
		// Arrange & Act
		var crc = Crc.Create(hex);

		// Assert
		Assert.Equal(hex.ToLowerInvariant(), crc.Value);
	}

	[Fact]
	public void Crc_Create_InvalidLength_Throws() {
		// Arrange, Act & Assert
		Assert.Throws<ArgumentException>(() => Crc.Create("abc"));
		Assert.Throws<ArgumentException>(() => Crc.Create("12345"));
	}

	[Fact]
	public void Crc_FromUInt32_Success() {
		// Arrange & Act
		var crc = Crc.FromUInt32(0xcafebabe);

		// Assert
		Assert.Equal("cafebabe", crc.Value);
		Assert.Equal(0xcafebabeU, crc.ToUInt32());
	}

	[Fact]
	public void Md5_Create_ValidHex_Success() {
		// Arrange & Act
		var md5 = Md5.Create("d41d8cd98f00b204e9800998ecf8427e");

		// Assert
		Assert.Equal("d41d8cd98f00b204e9800998ecf8427e", md5.Value);
	}

	[Theory]
	[InlineData("d41d8cd98f00b204e9800998ecf8427e")]
	[InlineData("D41D8CD98F00B204E9800998ECF8427E")]
	public void Md5_Create_ValidFormats_Success(string hex) {
		// Arrange & Act
		var md5 = Md5.Create(hex);

		// Assert
		Assert.Equal(hex.ToLowerInvariant(), md5.Value);
	}

	[Fact]
	public void Sha1_Create_ValidHex_Success() {
		// Arrange & Act
		var sha1 = Sha1.Create("da39a3ee5e6b4b0d3255bfef95601890afd80709");

		// Assert
		Assert.Equal("da39a3ee5e6b4b0d3255bfef95601890afd80709", sha1.Value);
	}

	[Theory]
	[InlineData("da39a3ee5e6b4b0d3255bfef95601890afd80709")]
	[InlineData("DA39A3EE5E6B4B0D3255BFEF95601890AFD80709")]
	public void Sha1_Create_ValidFormats_Success(string hex) {
		// Arrange & Act
		var sha1 = Sha1.Create(hex);

		// Assert
		Assert.Equal(hex.ToLowerInvariant(), sha1.Value);
	}

	[Fact]
	public void RomHashes_Create_Success() {
		// Arrange & Act
		var hashes = new RomHashes(
			Crc.Create("cafebabe"),
			Md5.Create("d41d8cd98f00b204e9800998ecf8427e"),
			Sha1.Create("da39a3ee5e6b4b0d3255bfef95601890afd80709"));

		// Assert
		Assert.Equal("cafebabe", hashes.Crc.Value);
		Assert.Equal("d41d8cd98f00b204e9800998ecf8427e", hashes.Md5.Value);
		Assert.Equal("da39a3ee5e6b4b0d3255bfef95601890afd80709", hashes.Sha1.Value);
	}

	[Fact]
	public void RomHashes_CreateFromStrings_Success() {
		// Arrange & Act
		var hashes = RomHashes.Create("cafebabe", "d41d8cd98f00b204e9800998ecf8427e", "da39a3ee5e6b4b0d3255bfef95601890afd80709");

		// Assert
		Assert.Equal("cafebabe", hashes.Crc.Value);
		Assert.Equal("d41d8cd98f00b204e9800998ecf8427e", hashes.Md5.Value);
		Assert.Equal("da39a3ee5e6b4b0d3255bfef95601890afd80709", hashes.Sha1.Value);
	}

	[Fact]
	public void RomHashes_MatchesAll_WhenEqual_ReturnsTrue() {
		// Arrange
		var hashes1 = RomHashes.Create("cafebabe", "d41d8cd98f00b204e9800998ecf8427e", "da39a3ee5e6b4b0d3255bfef95601890afd80709");
		var hashes2 = RomHashes.Create("cafebabe", "d41d8cd98f00b204e9800998ecf8427e", "da39a3ee5e6b4b0d3255bfef95601890afd80709");

		// Act & Assert
		Assert.True(hashes1.MatchesAll(hashes2));
	}

	[Fact]
	public void RomHashes_MatchesAny_WithSameCrc_ReturnsTrue() {
		// Arrange
		var hashes1 = RomHashes.Create("cafebabe", "d41d8cd98f00b204e9800998ecf8427e", "da39a3ee5e6b4b0d3255bfef95601890afd80709");
		var hashes2 = RomHashes.Create("cafebabe", "00000000000000000000000000000000", "0000000000000000000000000000000000000000");

		// Act & Assert
		Assert.True(hashes1.MatchesAny(hashes2));
	}
}
