using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ValueOf;

namespace Subrom.Domain.Hash {
	public class Sha1: ValueOf<string, Sha1> {
		protected static Regex ValidSHA1Regex { get; } = new(@"^[a-f\d]{40}$", RegexOptions.Compiled);

		protected override void Validate() {
			if (!ValidSHA1Regex.IsMatch(Value)) {
				// TODO: make a custom exception?
				throw new ArgumentException($"{nameof(Value)} is not a valid SHA1 string: {Value}");
			}
		}
	}
}
