using System.Xml.Serialization;

namespace Subrom.Domain.Datfiles.Xml {
	// DTD: <!ELEMENT release EMPTY>
	public class BiosSetElement {
		// DTD: <!ATTLIST biosset name CDATA #REQUIRED>
		[XmlAttribute("name")]
		public string Name { get; set; } = "";

		// DTD: <!ATTLIST biosset description CDATA #REQUIRED>
		[XmlAttribute("description")]
		public string Description { get; set; } = "";

		// DTD: <!ATTLIST biosset default (yes|no) "no">
		[XmlAttribute("default")]
		public string Default { get; set; } = "no";
	}
}
