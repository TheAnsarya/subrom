using System;
using System.Text.RegularExpressions;
using ValueOf;

namespace Subrom.Domain.Datfiles.Kinds {
	public class ForceMergingKind : ValueOf<string, ForceMergingKind> {
		protected static Regex ValidRegex { get; } = new(@"^(none|split|full)$", RegexOptions.Compiled);

		protected override void Validate() {
			if (!ValidRegex.IsMatch(Value)) {
				// TODO: make a custom exception?
				throw new ArgumentException($"{nameof(Value)} is not a valid {nameof(ForceMergingKind)}: {Value}");
			}
		}
	}
}
