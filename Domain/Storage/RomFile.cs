using Subrom.Domain.Hash;

namespace Subrom.Domain.Storage;

/// <summary>
/// Represents a ROM file on disk with its location and hash information.
/// CRITICAL: These records are NEVER deleted when drives go offline.
/// </summary>
public sealed record RomFile {
	/// <summary>Unique identifier for this ROM file record.</summary>
	public required Guid Id { get; init; }

	/// <summary>The drive this file is located on.</summary>
	public required Guid DriveId { get; init; }

	/// <summary>Full path to the file.</summary>
	public required string Path { get; init; }

	/// <summary>File name without path.</summary>
	public required string FileName { get; init; }

	/// <summary>File size in bytes.</summary>
	public long Size { get; init; }

	/// <summary>Last modification time.</summary>
	public DateTime ModifiedAt { get; init; }

	/// <summary>CRC32 hash.</summary>
	public Crc? Crc32 { get; set; }

	/// <summary>MD5 hash.</summary>
	public Md5? Md5 { get; set; }

	/// <summary>SHA1 hash.</summary>
	public Sha1? Sha1 { get; set; }

	/// <summary>When the hashes were last computed/verified.</summary>
	public DateTime? VerifiedAt { get; set; }

	/// <summary>Whether this file is currently accessible (drive online).</summary>
	public bool IsOnline { get; set; } = true;

	/// <summary>Whether this file is inside an archive.</summary>
	public bool IsInArchive { get; init; }

	/// <summary>If in an archive, the path to the archive.</summary>
	public string? ArchivePath { get; init; }

	/// <summary>If in an archive, the path within the archive.</summary>
	public string? PathInArchive { get; init; }

	/// <summary>When this record was first created.</summary>
	public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

	/// <summary>When this record was last updated.</summary>
	public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

	/// <summary>Creates a new RomFile with a generated ID.</summary>
	public static RomFile Create(Guid driveId, string path, string fileName, long size) => new() {
		Id = Guid.CreateVersion7(),
		DriveId = driveId,
		Path = path,
		FileName = fileName,
		Size = size,
		ModifiedAt = DateTime.UtcNow,
	};
}
