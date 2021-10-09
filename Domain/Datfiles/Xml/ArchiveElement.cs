using System.Xml.Serialization;

namespace Subrom.Domain.Datfiles.Xml {
	// DTD: <!ELEMENT archive EMPTY>
	public class ArchiveElement {
		// DTD: <!ATTLIST archive name CDATA #REQUIRED>
		[XmlAttribute("name")]
		public string Name { get; set; } = "";
	}
}
