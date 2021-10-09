using Subrom.Domain.Datfiles.Kinds;
using Subrom.Domain.Hash;

namespace Subrom.Domain.Datfiles {
	public class Rom {
		public string Name { get; set; } = "";

		public long Size { get; set; }

		public Crc Crc { get; set; }

		public Md5 Md5 { get; set; }

		public Sha1 Sha1 { get; set; }

		public string Merge { get; set; } = "";

		public StatusKind Status { get; set; } = StatusKind.From("good");

		public string Date { get; set; } = "";
	}
}
