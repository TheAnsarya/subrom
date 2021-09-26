using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ValueOf;

namespace Subrom.Domain.Hash {
	public class Md5: ValueOf<string, Md5> {
		protected static Regex ValidMD5Regex { get; } = new(@"^[a-f\d]{32}$", RegexOptions.Compiled);

		protected override void Validate() {
			if (!ValidMD5Regex.IsMatch(Value)) {
				// TODO: make a custom exception?
				throw new ArgumentException($"{nameof(Value)} is not a valid MD5 string: {Value}");
			}
		}
	}
}
