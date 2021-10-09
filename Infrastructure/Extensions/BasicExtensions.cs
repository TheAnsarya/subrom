using System.Globalization;
using System.Text;

namespace Subrom.Infrastructure.Extensions {
	public static class BasicExtensions {
		public static string ToHexString(this byte[] data) {
			if (data == null) {
				throw new ArgumentNullException(nameof(data), "Cannot convert null argument.");
			}

			var sb = new StringBuilder(data.Length * 2);

			foreach (var value in data) {
				_ = sb.Append(value.ToString("x2", CultureInfo.InvariantCulture));
			}

			var hex = sb.ToString();

			return hex;
		}
	}
}
