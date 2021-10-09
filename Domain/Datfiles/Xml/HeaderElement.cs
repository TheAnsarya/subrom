using System.Xml.Serialization;

namespace Subrom.Domain.Datfiles.Xml {
	// DTD: <!ELEMENT header (name, description, category?, version, date?, author, email?, homepage?, url?, comment?, clrmamepro?, romcenter?)>
	public class HeaderElement {
		// DTD: <!ELEMENT name (#PCDATA)>
		[XmlElement("name")]
		public string Name { get; set; } = "";

		// DTD: <!ELEMENT description (#PCDATA)>
		[XmlElement("description")]
		public string Description { get; set; } = "";

		// DTD: <!ELEMENT category (#PCDATA)>
		[XmlElement("category")]
		public string Category { get; set; } = "";

		// DTD: <!ELEMENT version (#PCDATA)>
		[XmlElement("version")]
		public string Version { get; set; } = "";

		// DTD: <!ELEMENT date (#PCDATA)>
		[XmlElement("date")]
		public string Date { get; set; } = "";

		// DTD: <!ELEMENT author (#PCDATA)>
		[XmlElement("author")]
		public string Author { get; set; } = "";

		// DTD: <!ELEMENT email (#PCDATA)>
		[XmlElement("email")]
		public string Email { get; set; } = "";

		// DTD: <!ELEMENT homepage (#PCDATA)>
		[XmlElement("homepage")]
		public string Homepage { get; set; } = "";

		// DTD: <!ELEMENT url (#PCDATA)>
		[XmlElement("url")]
		public string Url { get; set; } = "";

		// DTD: <!ELEMENT comment (#PCDATA)>
		[XmlElement("comment")]
		public string Comment { get; set; } = "";

		// DTD: <!ELEMENT clrmamepro EMPTY>
		[XmlElement("clrmamepro")]
		public ClrmameproElement Clrmamepro { get; set; }

		// DTD: <!ELEMENT romcenter EMPTY>
		[XmlElement("romcenter")]
		public RomcenterElement Romcenter { get; set; }
	}
}
