using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Subrom.Domain.ValueObjects;

/// <summary>
/// SHA-1 hash value object. Immutable, validated, always lowercase.
/// </summary>
public readonly partial record struct Sha1 : IParsable<Sha1>, ISpanParsable<Sha1> {
	private const int Sha1Length = 40;
	private readonly string _value;

	public string Value => _value ?? new string('0', Sha1Length);

	private Sha1(string value) {
		_value = value;
	}

	/// <summary>
	/// Creates a SHA-1 from a validated hex string.
	/// </summary>
	public static Sha1 Create(string value) {
		ArgumentException.ThrowIfNullOrWhiteSpace(value);

		var normalized = value.ToLowerInvariant();
		if (!IsValidSha1(normalized)) {
			throw new ArgumentException($"Invalid SHA-1 value: {value}. Must be 40 hex characters.", nameof(value));
		}

		return new Sha1(normalized);
	}

	/// <summary>
	/// Tries to create a SHA-1 from a hex string.
	/// </summary>
	public static bool TryCreate(string? value, [NotNullWhen(true)] out Sha1? result) {
		if (string.IsNullOrWhiteSpace(value)) {
			result = null;
			return false;
		}

		var normalized = value.ToLowerInvariant();
		if (!IsValidSha1(normalized)) {
			result = null;
			return false;
		}

		result = new Sha1(normalized);
		return true;
	}

	/// <summary>
	/// Creates a SHA-1 from a byte array.
	/// </summary>
	public static Sha1 FromBytes(byte[] bytes) {
		ArgumentNullException.ThrowIfNull(bytes);
		if (bytes.Length != 20) {
			throw new ArgumentException("SHA-1 bytes must be exactly 20 bytes.", nameof(bytes));
		}
		return new Sha1(Convert.ToHexStringLower(bytes));
	}

	/// <summary>
	/// Creates a SHA-1 from a span of bytes.
	/// </summary>
	public static Sha1 FromBytes(ReadOnlySpan<byte> bytes) {
		if (bytes.Length != 20) {
			throw new ArgumentException("SHA-1 bytes must be exactly 20 bytes.", nameof(bytes));
		}
		return new Sha1(Convert.ToHexStringLower(bytes));
	}

	/// <summary>
	/// Converts to a byte array.
	/// </summary>
	public byte[] ToBytes() =>
		Convert.FromHexString(_value);

	private static bool IsValidSha1(string value) =>
		value.Length == Sha1Length && Sha1Regex().IsMatch(value);

	[GeneratedRegex(@"^[a-f\d]{40}$", RegexOptions.Compiled)]
	private static partial Regex Sha1Regex();

	// IParsable implementation
	public static Sha1 Parse(string s, IFormatProvider? provider) =>
		Create(s);

	public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out Sha1 result) {
		if (TryCreate(s, out var sha1)) {
			result = sha1.Value;
			return true;
		}
		result = default;
		return false;
	}

	// ISpanParsable implementation
	public static Sha1 Parse(ReadOnlySpan<char> s, IFormatProvider? provider) =>
		Create(s.ToString());

	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Sha1 result) =>
		TryParse(s.ToString(), provider, out result);

	public override string ToString() => Value;

	public static implicit operator string(Sha1 sha1) => sha1.Value;
}
