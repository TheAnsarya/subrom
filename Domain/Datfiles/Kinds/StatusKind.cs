using System;
using System.Text.RegularExpressions;
using ValueOf;

namespace Subrom.Domain.Datfiles.Kinds {
	public class StatusKind : ValueOf<string, StatusKind> {
		protected static Regex ValidRegex { get; } = new(@"^(baddump|nodump|good|verified)$", RegexOptions.Compiled);

		protected override void Validate() {
			if (!ValidRegex.IsMatch(Value)) {
				// TODO: make a custom exception?
				throw new ArgumentException($"{nameof(Value)} is not a valid {nameof(StatusKind)}: {Value}");
			}
		}
	}
}
