using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Subrom.Domain.ValueObjects;

/// <summary>
/// MD5 hash value object. Immutable, validated, always lowercase.
/// </summary>
public readonly partial record struct Md5 : IParsable<Md5>, ISpanParsable<Md5> {
	private const int Md5Length = 32;
	private readonly string _value;

	public string Value => _value ?? new string('0', Md5Length);

	private Md5(string value) {
		_value = value;
	}

	/// <summary>
	/// Creates an MD5 from a validated hex string.
	/// </summary>
	public static Md5 Create(string value) {
		ArgumentException.ThrowIfNullOrWhiteSpace(value);

		var normalized = value.ToLowerInvariant();
		if (!IsValidMd5(normalized)) {
			throw new ArgumentException($"Invalid MD5 value: {value}. Must be 32 hex characters.", nameof(value));
		}

		return new Md5(normalized);
	}

	/// <summary>
	/// Tries to create an MD5 from a hex string.
	/// </summary>
	public static bool TryCreate(string? value, [NotNullWhen(true)] out Md5? result) {
		if (string.IsNullOrWhiteSpace(value)) {
			result = null;
			return false;
		}

		var normalized = value.ToLowerInvariant();
		if (!IsValidMd5(normalized)) {
			result = null;
			return false;
		}

		result = new Md5(normalized);
		return true;
	}

	/// <summary>
	/// Creates an MD5 from a byte array.
	/// </summary>
	public static Md5 FromBytes(byte[] bytes) {
		ArgumentNullException.ThrowIfNull(bytes);
		if (bytes.Length != 16) {
			throw new ArgumentException("MD5 bytes must be exactly 16 bytes.", nameof(bytes));
		}

		return new Md5(Convert.ToHexStringLower(bytes));
	}

	/// <summary>
	/// Creates an MD5 from a span of bytes.
	/// </summary>
	public static Md5 FromBytes(ReadOnlySpan<byte> bytes) {
		if (bytes.Length != 16) {
			throw new ArgumentException("MD5 bytes must be exactly 16 bytes.", nameof(bytes));
		}

		return new Md5(Convert.ToHexStringLower(bytes));
	}

	/// <summary>
	/// Converts to a byte array.
	/// </summary>
	public byte[] ToBytes() =>
		Convert.FromHexString(_value);

	private static bool IsValidMd5(string value) =>
		value.Length == Md5Length && Md5Regex().IsMatch(value);

	[GeneratedRegex(@"^[a-f\d]{32}$", RegexOptions.Compiled)]
	private static partial Regex Md5Regex();

	// IParsable implementation
	public static Md5 Parse(string s, IFormatProvider? provider) =>
		Create(s);

	public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out Md5 result) {
		if (TryCreate(s, out var md5)) {
			result = md5.Value;
			return true;
		}

		result = default;
		return false;
	}

	// ISpanParsable implementation
	public static Md5 Parse(ReadOnlySpan<char> s, IFormatProvider? provider) =>
		Create(s.ToString());

	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Md5 result) =>
		TryParse(s.ToString(), provider, out result);

	public override string ToString() => Value;

	public static implicit operator string(Md5 md5) => md5.Value;
}
