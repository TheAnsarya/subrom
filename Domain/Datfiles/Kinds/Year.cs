using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ValueOf;

namespace Subrom.Domain.Datfiles.Kinds {
	public class Year : ValueOf<string, Year> {
		protected static Regex ValidYearRegex { get; } = new(@"^(?:\d\?{3}|\d{2}\?{2}|\d{3}\?|\d{4})$", RegexOptions.Compiled);

		protected override void Validate() {
			if (!ValidYearRegex.IsMatch(Value)) {
				// TODO: make a custom exception?
				throw new ArgumentException($"{nameof(Value)} is not a valid year: {Value}");
			}
		}
	}
}
