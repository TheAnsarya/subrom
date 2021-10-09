using Subrom.Domain.Datfiles.Kinds;
using Subrom.Domain.Hash;

namespace Subrom.Domain.Datfiles {
	public class Disk {
		public string Name { get; set; } = "";

		// TODO: This isn't in the dtd, do any dats have crcs on disks?
		public Crc Crc { get; set; }

		public Md5 Md5 { get; set; }

		public Sha1 Sha1 { get; set; }

		public string Merge { get; set; } = "";

		public StatusKind Status { get; set; } = StatusKind.From("good");
	}
}
