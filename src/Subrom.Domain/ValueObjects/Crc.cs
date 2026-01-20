using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Subrom.Domain.ValueObjects;

/// <summary>
/// CRC32 hash value object. Immutable, validated, always lowercase.
/// </summary>
public readonly partial record struct Crc : IParsable<Crc>, ISpanParsable<Crc> {
	private const int CrcLength = 8;
	private readonly string _value;

	public string Value => _value ?? "00000000";

	private Crc(string value) {
		_value = value;
	}

	/// <summary>
	/// Creates a CRC from a validated hex string.
	/// </summary>
	public static Crc Create(string value) {
		ArgumentException.ThrowIfNullOrWhiteSpace(value);

		var normalized = value.ToLowerInvariant();
		if (!IsValidCrc(normalized)) {
			throw new ArgumentException($"Invalid CRC32 value: {value}. Must be 8 hex characters.", nameof(value));
		}

		return new Crc(normalized);
	}

	/// <summary>
	/// Tries to create a CRC from a hex string.
	/// </summary>
	public static bool TryCreate(string? value, [NotNullWhen(true)] out Crc? result) {
		if (string.IsNullOrWhiteSpace(value)) {
			result = null;
			return false;
		}

		var normalized = value.ToLowerInvariant();
		if (!IsValidCrc(normalized)) {
			result = null;
			return false;
		}

		result = new Crc(normalized);
		return true;
	}

	/// <summary>
	/// Creates a CRC from a uint value.
	/// </summary>
	public static Crc FromUInt32(uint value) =>
		new(value.ToString("x8"));

	/// <summary>
	/// Converts the CRC to a uint.
	/// </summary>
	public uint ToUInt32() =>
		Convert.ToUInt32(_value, 16);

	private static bool IsValidCrc(string value) =>
		value.Length == CrcLength && CrcRegex().IsMatch(value);

	[GeneratedRegex(@"^[a-f\d]{8}$", RegexOptions.Compiled)]
	private static partial Regex CrcRegex();

	// IParsable implementation
	public static Crc Parse(string s, IFormatProvider? provider) =>
		Create(s);

	public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out Crc result) {
		if (TryCreate(s, out var crc)) {
			result = crc.Value;
			return true;
		}
		result = default;
		return false;
	}

	// ISpanParsable implementation
	public static Crc Parse(ReadOnlySpan<char> s, IFormatProvider? provider) =>
		Create(s.ToString());

	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Crc result) =>
		TryParse(s.ToString(), provider, out result);

	public override string ToString() => Value;

	// Implicit conversion to string for convenience
	public static implicit operator string(Crc crc) => crc.Value;
}
