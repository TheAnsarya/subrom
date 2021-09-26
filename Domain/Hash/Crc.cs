using System;
using System.Text.RegularExpressions;
using ValueOf;

namespace Subrom.Domain.Hash {
	public class Crc : ValueOf<string, Crc> {
		protected static Regex ValidCrcRegex { get; } = new(@"^[a-f\d]{8}$", RegexOptions.Compiled);

		protected override void Validate() {
			if (!ValidCrcRegex.IsMatch(Value)) {
				// TODO: make a custom exception?
				throw new ArgumentException($"{nameof(Value)} is not a valid Crc string: {Value}");
			}
		}
	}
}
