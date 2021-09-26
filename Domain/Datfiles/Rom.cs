using System.Xml.Serialization;
using Subrom.Domain.Hash;

namespace Subrom.Domain.Datfiles {
	public class Rom {
		public string Name { get; set; } = "";

		public long Size { get; set; }

		public Crc Crc { get; set; }

		public Md5 Md5 { get; set; }

		public Sha1 Sha1 { get; set; }
	}
}
