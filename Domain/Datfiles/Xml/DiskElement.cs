using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Subrom.Domain.Hash;

namespace Subrom.Domain.Datfiles.Xml {
	public class DiskElement {
		[XmlAttribute("name")]
		public string Name { get; set; } = "";

		[XmlAttribute("crc")]
		public string Crc { get; set; } = "";

		[XmlAttribute("md5")]
		public string Md5 { get; set; } = "";

		[XmlAttribute("sha1")]
		public string Sha1 { get; set; } = "";
	}
}
