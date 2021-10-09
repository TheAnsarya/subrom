using System.Xml.Serialization;

namespace Subrom.Domain.Datfiles.Xml {
	// DTD: <!ELEMENT rom EMPTY>
	public class RomElement {
		// DTD: <!ATTLIST rom name CDATA #REQUIRED>
		[XmlAttribute("name")]
		public string Name { get; set; } = "";

		// DTD: <!ATTLIST rom size CDATA #REQUIRED>
		[XmlAttribute("size")]
		public long Size { get; set; }

		// DTD: <!ATTLIST rom crc CDATA #IMPLIED>
		[XmlAttribute("crc")]
		public string Crc { get; set; } = "";

		// DTD: <!ATTLIST rom md5 CDATA #IMPLIED>
		[XmlAttribute("md5")]
		public string Md5 { get; set; } = "";

		// DTD: <!ATTLIST rom sha1 CDATA #IMPLIED>
		[XmlAttribute("sha1")]
		public string Sha1 { get; set; } = "";

		// DTD: <!ATTLIST rom merge CDATA #IMPLIED>
		[XmlAttribute("merge")]
		public string Merge { get; set; } = "";

		// DTD: <!ATTLIST rom status (baddump|nodump|good|verified) "good">
		[XmlAttribute("status")]
		public string Status { get; set; } = "good";

		// DTD: <!ATTLIST rom date CDATA #IMPLIED>
		[XmlAttribute("date")]
		public string Date { get; set; } = "";
	}
}
