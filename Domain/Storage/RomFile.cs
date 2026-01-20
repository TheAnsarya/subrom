using Subrom.Domain.Hash;

namespace Subrom.Domain.Storage;

/// <summary>
/// Represents a ROM file on disk with its location and hash information.
/// </summary>
public class RomFile {
	/// <summary>
	/// Unique identifier for this ROM file record.
	/// </summary>
	public Guid Id { get; set; } = Guid.NewGuid();

	/// <summary>
	/// The drive this file is located on.
	/// </summary>
	public Guid DriveId { get; set; }

	/// <summary>
	/// Full path to the file.
	/// </summary>
	public string Path { get; set; } = "";

	/// <summary>
	/// File name without path.
	/// </summary>
	public string FileName { get; set; } = "";

	/// <summary>
	/// File size in bytes.
	/// </summary>
	public long Size { get; set; }

	/// <summary>
	/// Last modification time.
	/// </summary>
	public DateTime ModifiedAt { get; set; }

	/// <summary>
	/// CRC32 hash.
	/// </summary>
	public Crc? Crc32 { get; set; }

	/// <summary>
	/// MD5 hash.
	/// </summary>
	public Md5? Md5 { get; set; }

	/// <summary>
	/// SHA1 hash.
	/// </summary>
	public Sha1? Sha1 { get; set; }

	/// <summary>
	/// When the hashes were last computed/verified.
	/// </summary>
	public DateTime? VerifiedAt { get; set; }

	/// <summary>
	/// Whether this file is currently accessible (drive online).
	/// </summary>
	public bool IsOnline { get; set; } = true;

	/// <summary>
	/// Whether this file is inside an archive.
	/// </summary>
	public bool IsInArchive { get; set; }

	/// <summary>
	/// If in an archive, the path to the archive.
	/// </summary>
	public string? ArchivePath { get; set; }

	/// <summary>
	/// If in an archive, the path within the archive.
	/// </summary>
	public string? PathInArchive { get; set; }

	/// <summary>
	/// When this record was first created.
	/// </summary>
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	/// <summary>
	/// When this record was last updated.
	/// </summary>
	public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
