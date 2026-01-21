using Subrom.Application.Interfaces;

namespace Subrom.Infrastructure.Services;

/// <summary>
/// Service for detecting and handling ROM headers.
/// Supports common copier/dumper headers that need to be removed for accurate hashing.
/// </summary>
public sealed class RomHeaderService : IRomHeaderService {
	// Standard header sizes by extension
	private static readonly Dictionary<string, int> StandardHeaderSizes = new(StringComparer.OrdinalIgnoreCase) {
		// Nintendo
		[".nes"] = 16,      // iNES header
		[".fds"] = 16,      // FDS header (fwNES format)
		[".unf"] = 0,       // UNIF format (variable, handled separately)

		// SNES
		[".smc"] = 512,     // Super MagiCom header
		[".sfc"] = 0,       // No header (clean)
		[".fig"] = 512,     // Pro Fighter header
		[".swc"] = 512,     // Super Wild Card header

		// N64
		[".n64"] = 0,       // No standard header
		[".v64"] = 0,       // Byteswapped, no header
		[".z64"] = 0,       // Big-endian, no header

		// Game Boy
		[".gb"] = 0,        // No header (internal header doesn't count)
		[".gbc"] = 0,       // No header
		[".gba"] = 0,       // No header
		[".sgb"] = 0,       // No header

		// Sega
		[".smd"] = 512,     // Super Magic Drive interleaved header
		[".md"] = 0,        // No header (clean Genesis)
		[".gen"] = 0,       // No header
		[".bin"] = 0,       // Generic, no header
		[".sms"] = 0,       // No header
		[".gg"] = 0,        // No header
		[".32x"] = 0,       // No header

		// Atari
		[".a78"] = 128,     // Atari 7800 header
		[".a26"] = 0,       // No header
		[".lnx"] = 64,      // Lynx header

		// PC Engine / TurboGrafx
		[".pce"] = 0,       // No header (clean)
		[".sgx"] = 0,       // No header

		// Neo Geo Pocket
		[".ngp"] = 0,       // No header
		[".ngc"] = 0,       // No header

		// WonderSwan
		[".ws"] = 0,        // No header
		[".wsc"] = 0,       // No header
	};

	private static readonly HashSet<string> SupportedFormats = [
		".nes", ".fds", ".smc", ".sfc", ".fig", ".swc",
		".n64", ".v64", ".z64", ".gb", ".gbc", ".gba", ".sgb",
		".smd", ".md", ".gen", ".bin", ".sms", ".gg", ".32x",
		".a78", ".a26", ".lnx", ".pce", ".sgx",
		".ngp", ".ngc", ".ws", ".wsc"
	];

	public IReadOnlySet<string> SupportedExtensions => SupportedFormats;

	public bool SupportsFormat(string extension) {
		return SupportedFormats.Contains(extension.ToLowerInvariant());
	}

	public int GetStandardHeaderSize(string extension) {
		return StandardHeaderSizes.GetValueOrDefault(extension.ToLowerInvariant(), 0);
	}

	public async Task<RomHeaderInfo?> DetectHeaderAsync(
		Stream stream,
		string extension,
		CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(stream);

		if (!stream.CanRead || !stream.CanSeek) {
			throw new ArgumentException("Stream must be readable and seekable", nameof(stream));
		}

		var ext = extension.ToLowerInvariant();

		// Save original position
		var originalPosition = stream.Position;

		try {
			stream.Position = 0;

			return ext switch {
				".nes" => await DetectNesHeaderAsync(stream, cancellationToken),
				".fds" => await DetectFdsHeaderAsync(stream, cancellationToken),
				".smc" or ".fig" or ".swc" => await DetectSnesHeaderAsync(stream, ext, cancellationToken),
				".sfc" => await DetectSfcHeaderAsync(stream, cancellationToken),
				".smd" => await DetectSmdHeaderAsync(stream, cancellationToken),
				".a78" => await DetectA78HeaderAsync(stream, cancellationToken),
				".lnx" => await DetectLynxHeaderAsync(stream, cancellationToken),
				_ => null
			};
		}
		finally {
			// Restore original position
			stream.Position = originalPosition;
		}
	}

	private static async Task<RomHeaderInfo?> DetectNesHeaderAsync(Stream stream, CancellationToken ct) {
		if (stream.Length < 16) return null;

		var header = new byte[16];
		await stream.ReadExactlyAsync(header, ct);

		// iNES magic: "NES" + 0x1A
		if (header[0] == 'N' && header[1] == 'E' && header[2] == 'S' && header[3] == 0x1a) {
			var isNes2 = (header[7] & 0x0c) == 0x08;
			var prgSize = header[4];
			var chrSize = header[5];

			return new RomHeaderInfo {
				HeaderSize = 16,
				Format = isNes2 ? "NES 2.0" : "iNES",
				Description = $"{(isNes2 ? "NES 2.0" : "iNES")} header: PRG={prgSize * 16}KB, CHR={chrSize * 8}KB",
				Metadata = new Dictionary<string, string> {
					["PRG_SIZE"] = (prgSize * 16).ToString(),
					["CHR_SIZE"] = (chrSize * 8).ToString(),
					["MAPPER"] = ((header[6] >> 4) | (header[7] & 0xf0)).ToString()
				}
			};
		}

		return null;
	}

	private static async Task<RomHeaderInfo?> DetectFdsHeaderAsync(Stream stream, CancellationToken ct) {
		if (stream.Length < 16) return null;

		var header = new byte[16];
		await stream.ReadExactlyAsync(header, ct);

		// fwNES FDS header: "FDS" + 0x1A
		if (header[0] == 'F' && header[1] == 'D' && header[2] == 'S' && header[3] == 0x1a) {
			var sideCount = header[4];

			return new RomHeaderInfo {
				HeaderSize = 16,
				Format = "fwNES FDS",
				Description = $"fwNES FDS header: {sideCount} disk side(s)",
				Metadata = new Dictionary<string, string> {
					["SIDES"] = sideCount.ToString()
				}
			};
		}

		return null;
	}

	private static async Task<RomHeaderInfo?> DetectSnesHeaderAsync(Stream stream, string ext, CancellationToken ct) {
		// SMC/SWC/FIG files typically have 512-byte copier headers
		// Check if file size is (power of 2) + 512

		var length = stream.Length;
		var remainder = length % 1024;

		// If file has 512 byte offset from 1KB boundary, likely has header
		if (remainder == 512 && length > 512) {
			var header = new byte[512];
			await stream.ReadExactlyAsync(header, ct);

			var format = ext switch {
				".smc" => "Super MagiCom",
				".fig" => "Pro Fighter",
				".swc" => "Super Wild Card",
				_ => "SNES Copier"
			};

			return new RomHeaderInfo {
				HeaderSize = 512,
				Format = format,
				Description = $"{format} copier header (512 bytes)"
			};
		}

		return null;
	}

	private static Task<RomHeaderInfo?> DetectSfcHeaderAsync(Stream stream, CancellationToken ct) {
		// SFC files are usually headerless, but check for accidental SMC extension
		var length = stream.Length;
		var remainder = length % 1024;

		if (remainder == 512 && length > 512) {
			return Task.FromResult<RomHeaderInfo?>(new RomHeaderInfo {
				HeaderSize = 512,
				Format = "SNES Copier",
				Description = "Unexpected copier header on .sfc file (512 bytes)",
				IsStandardHeader = false
			});
		}

		return Task.FromResult<RomHeaderInfo?>(null);
	}

	private static async Task<RomHeaderInfo?> DetectSmdHeaderAsync(Stream stream, CancellationToken ct) {
		// SMD files have a 512-byte header
		if (stream.Length < 512) return null;

		var header = new byte[512];
		await stream.ReadExactlyAsync(header, ct);

		// SMD header usually starts with block count and other metadata
		// Check for typical SMD patterns
		var blockCount = header[0] | (header[1] << 8);
		var romType = header[2]; // 0x03 for split files, others for single

		// Simple validation: reasonable block count and file size match
		var expectedBlocks = (stream.Length - 512) / 16384;
		if (blockCount > 0 && blockCount <= expectedBlocks + 1) {
			return new RomHeaderInfo {
				HeaderSize = 512,
				Format = "Super Magic Drive",
				Description = $"SMD header: {blockCount} blocks",
				Metadata = new Dictionary<string, string> {
					["BLOCKS"] = blockCount.ToString(),
					["TYPE"] = romType.ToString()
				}
			};
		}

		return null;
	}

	private static async Task<RomHeaderInfo?> DetectA78HeaderAsync(Stream stream, CancellationToken ct) {
		if (stream.Length < 128) return null;

		var header = new byte[128];
		await stream.ReadExactlyAsync(header, ct);

		// A78 header signature: "ATARI7800" at offset 1
		if (header[1] == 'A' && header[2] == 'T' && header[3] == 'A' &&
		    header[4] == 'R' && header[5] == 'I' && header[6] == '7' &&
		    header[7] == '8' && header[8] == '0' && header[9] == '0') {

			// Extract title (bytes 17-48)
			var titleBytes = header.AsSpan(17, 32);
			var title = System.Text.Encoding.ASCII.GetString(titleBytes).TrimEnd('\0', ' ');

			return new RomHeaderInfo {
				HeaderSize = 128,
				Format = "A78",
				Description = $"Atari 7800 header: {title}",
				Metadata = new Dictionary<string, string> {
					["TITLE"] = title
				}
			};
		}

		return null;
	}

	private static async Task<RomHeaderInfo?> DetectLynxHeaderAsync(Stream stream, CancellationToken ct) {
		if (stream.Length < 64) return null;

		var header = new byte[64];
		await stream.ReadExactlyAsync(header, ct);

		// Lynx header signature: "LYNX" at start
		if (header[0] == 'L' && header[1] == 'Y' && header[2] == 'N' && header[3] == 'X') {
			// Extract cart name (bytes 10-42)
			var nameBytes = header.AsSpan(10, 32);
			var name = System.Text.Encoding.ASCII.GetString(nameBytes).TrimEnd('\0', ' ');

			return new RomHeaderInfo {
				HeaderSize = 64,
				Format = "LNX",
				Description = $"Atari Lynx header: {name}",
				Metadata = new Dictionary<string, string> {
					["NAME"] = name
				}
			};
		}

		return null;
	}
}
