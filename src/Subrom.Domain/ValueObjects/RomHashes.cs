namespace Subrom.Domain.ValueObjects;

/// <summary>
/// Combines all hash values for a ROM file.
/// </summary>
public readonly record struct RomHashes {
	public Crc Crc { get; init; }
	public Md5 Md5 { get; init; }
	public Sha1 Sha1 { get; init; }

	public RomHashes(Crc crc, Md5 md5, Sha1 sha1) {
		Crc = crc;
		Md5 = md5;
		Sha1 = sha1;
	}

	/// <summary>
	/// Creates hashes from string values.
	/// </summary>
	public static RomHashes Create(string crc, string md5, string sha1) =>
		new(Crc.Create(crc), Md5.Create(md5), Sha1.Create(sha1));

	/// <summary>
	/// Tries to create hashes from string values.
	/// </summary>
	public static bool TryCreate(string? crc, string? md5, string? sha1, out RomHashes result) {
		if (Crc.TryCreate(crc, out var crcVal) &&
			Md5.TryCreate(md5, out var md5Val) &&
			Sha1.TryCreate(sha1, out var sha1Val)) {
			result = new RomHashes(crcVal.Value, md5Val.Value, sha1Val.Value);
			return true;
		}

		result = default;
		return false;
	}

	/// <summary>
	/// Checks if any hash matches the other set.
	/// </summary>
	public bool MatchesAny(RomHashes other) =>
		Crc == other.Crc || Md5 == other.Md5 || Sha1 == other.Sha1;

	/// <summary>
	/// Checks if all hashes match.
	/// </summary>
	public bool MatchesAll(RomHashes other) =>
		Crc == other.Crc && Md5 == other.Md5 && Sha1 == other.Sha1;

	public override string ToString() =>
		$"CRC:{Crc} MD5:{Md5} SHA1:{Sha1}";
}
