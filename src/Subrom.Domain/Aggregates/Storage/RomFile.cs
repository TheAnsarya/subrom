using Subrom.Domain.Common;
using Subrom.Domain.ValueObjects;

namespace Subrom.Domain.Aggregates.Storage;

/// <summary>
/// Represents a ROM file found on a drive.
/// </summary>
public class RomFile : Entity {
	/// <summary>
	/// Path relative to the drive root.
	/// </summary>
	public required string RelativePath { get; init; }

	/// <summary>
	/// Filename only (extracted from path).
	/// </summary>
	public required string FileName { get; init; }

	/// <summary>
	/// File size in bytes.
	/// </summary>
	public required long Size { get; init; }

	/// <summary>
	/// CRC32 hash (nullable until computed).
	/// </summary>
	public string? Crc { get; set; }

	/// <summary>
	/// MD5 hash (nullable until computed).
	/// </summary>
	public string? Md5 { get; set; }

	/// <summary>
	/// SHA-1 hash (nullable until computed).
	/// </summary>
	public string? Sha1 { get; set; }

	/// <summary>
	/// Drive this file is located on.
	/// </summary>
	public Guid DriveId { get; init; }

	/// <summary>
	/// When this file was first scanned.
	/// </summary>
	public DateTime ScannedAt { get; init; } = DateTime.UtcNow;

	/// <summary>
	/// When this file was last hashed.
	/// </summary>
	public DateTime? HashedAt { get; set; }

	/// <summary>
	/// File's last modified timestamp.
	/// </summary>
	public DateTime LastModified { get; init; }

	/// <summary>
	/// Whether the file is inside an archive.
	/// </summary>
	public bool IsArchived { get; init; }

	/// <summary>
	/// Archive path if this file is inside an archive.
	/// </summary>
	public string? ArchivePath { get; init; }

	/// <summary>
	/// Path within the archive (if archived).
	/// </summary>
	public string? PathInArchive { get; init; }

	/// <summary>
	/// Verification status against DATs.
	/// </summary>
	public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Unknown;

	/// <summary>
	/// ID of the matched ROM entry (if verified).
	/// </summary>
	public int? MatchedRomEntryId { get; set; }

	/// <summary>
	/// ID of the matched DAT file (if verified).
	/// </summary>
	public Guid? MatchedDatFileId { get; set; }

	/// <summary>
	/// Whether hashes have been computed.
	/// </summary>
	public bool HasHashes => Crc is not null && Md5 is not null && Sha1 is not null;

	/// <summary>
	/// Creates a new ROM file entry.
	/// </summary>
	public static RomFile Create(
		Guid driveId,
		string relativePath,
		long size,
		DateTime lastModified,
		bool isArchived = false,
		string? archivePath = null,
		string? pathInArchive = null) {
		return new RomFile {
			DriveId = driveId,
			RelativePath = relativePath,
			FileName = Path.GetFileName(relativePath),
			Size = size,
			LastModified = lastModified,
			IsArchived = isArchived,
			ArchivePath = archivePath,
			PathInArchive = pathInArchive
		};
	}

	/// <summary>
	/// Sets the computed hashes for this file.
	/// </summary>
	public void SetHashes(RomHashes hashes) {
		Crc = hashes.Crc.Value;
		Md5 = hashes.Md5.Value;
		Sha1 = hashes.Sha1.Value;
		HashedAt = DateTime.UtcNow;
	}

	/// <summary>
	/// Gets the hashes as a RomHashes value object.
	/// </summary>
	public RomHashes? GetHashes() {
		if (!HasHashes) return null;
		return RomHashes.Create(Crc!, Md5!, Sha1!);
	}

	/// <summary>
	/// Marks this file as verified against a DAT entry.
	/// </summary>
	public void MarkVerified(Guid datFileId, int romEntryId) {
		VerificationStatus = VerificationStatus.Verified;
		MatchedDatFileId = datFileId;
		MatchedRomEntryId = romEntryId;
	}

	/// <summary>
	/// Marks this file as unverified (no DAT match).
	/// </summary>
	public void MarkUnverified() {
		VerificationStatus = VerificationStatus.Unknown;
		MatchedDatFileId = null;
		MatchedRomEntryId = null;
	}
}

/// <summary>
/// ROM verification status.
/// </summary>
public enum VerificationStatus {
	Unknown,
	Verified,
	NotInDat,
	BadDump,
	WrongName,
	Duplicate
}
