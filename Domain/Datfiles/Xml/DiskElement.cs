using System.Xml.Serialization;

namespace Subrom.Domain.Datfiles.Xml {
	// DTD: <!ELEMENT disk EMPTY>
	public class DiskElement {
		// DTD: <!ATTLIST disk name CDATA #REQUIRED>
		[XmlAttribute("name")]
		public string Name { get; set; } = "";

		// DTD: 
		[XmlAttribute("crc")]
		public string Crc { get; set; } = "";

		// DTD: <!ATTLIST disk md5 CDATA #IMPLIED>
		[XmlAttribute("md5")]
		public string Md5 { get; set; } = "";

		// DTD: <!ATTLIST disk sha1 CDATA #IMPLIED>
		[XmlAttribute("sha1")]
		public string Sha1 { get; set; } = "";

		// DTD: <!ATTLIST disk merge CDATA #IMPLIED>
		[XmlAttribute("merge")]
		public string Merge { get; set; } = "";

		// DTD: <!ATTLIST disk status (baddump|nodump|good|verified) "good">
		[XmlAttribute("status")]
		public string Status { get; set; } = "good";
	}
}
