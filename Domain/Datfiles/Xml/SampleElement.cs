using System.Xml.Serialization;

namespace Subrom.Domain.Datfiles.Xml {
	// DTD: <!ELEMENT sample EMPTY>
	public class SampleElement {
		// DTD: <!ATTLIST sample name CDATA #REQUIRED>
		[XmlAttribute("name")]
		public string Name { get; set; } = "";
	}
}
