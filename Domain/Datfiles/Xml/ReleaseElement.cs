using System.Xml.Serialization;

namespace Subrom.Domain.Datfiles.Xml {
	// DTD: <!ELEMENT release EMPTY>
	public class ReleaseElement {
		// DTD: <!ATTLIST release name CDATA #REQUIRED>
		[XmlAttribute("name")]
		public string Name { get; set; } = "";

		// DTD: <!ATTLIST release region CDATA #REQUIRED>
		[XmlAttribute("region")]
		public string Region { get; set; } = "";

		// DTD: <!ATTLIST release language CDATA #IMPLIED>
		[XmlAttribute("language")]
		public string Language { get; set; } = "";

		// DTD: <!ATTLIST release date CDATA #IMPLIED>
		[XmlAttribute("date")]
		public string Date { get; set; } = "";

		// DTD: <!ATTLIST release default (yes|no) "no">
		[XmlAttribute("default")]
		public string Default { get; set; } = "no";
	}
}
