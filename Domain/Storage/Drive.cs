namespace Subrom.Domain.Storage;

/// <summary>
/// Represents a registered storage drive for ROM files.
/// </summary>
public class Drive {
	/// <summary>
	/// Unique identifier for this drive.
	/// </summary>
	public Guid Id { get; set; } = Guid.NewGuid();

	/// <summary>
	/// User-friendly label for this drive.
	/// </summary>
	public string Label { get; set; } = "";

	/// <summary>
	/// Root path for ROM storage on this drive.
	/// </summary>
	public string Path { get; set; } = "";

	/// <summary>
	/// Volume serial number or other unique identifier.
	/// Used to detect when the same drive is reconnected.
	/// </summary>
	public string VolumeId { get; set; } = "";

	/// <summary>
	/// Whether this drive is currently accessible.
	/// </summary>
	public bool IsOnline { get; set; } = true;

	/// <summary>
	/// Last time this drive was seen online.
	/// </summary>
	public DateTime LastSeen { get; set; } = DateTime.UtcNow;

	/// <summary>
	/// Last time this drive was scanned.
	/// </summary>
	public DateTime? LastScanned { get; set; }

	/// <summary>
	/// Total capacity in bytes.
	/// </summary>
	public long TotalCapacity { get; set; }

	/// <summary>
	/// Available free space in bytes.
	/// </summary>
	public long FreeSpace { get; set; }

	/// <summary>
	/// When this drive was registered.
	/// </summary>
	public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

	/// <summary>
	/// Number of ROM files on this drive.
	/// </summary>
	public int RomCount { get; set; }

	/// <summary>
	/// Whether this drive is enabled for scanning and organization.
	/// </summary>
	public bool IsEnabled { get; set; } = true;
}
