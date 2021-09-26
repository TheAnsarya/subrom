using System.Xml.Serialization;

namespace Subrom.Domain.Datfiles.Xml {
	public class HeaderElement {
		[XmlElement("name")]
		public string Name { get; set; } = "";

		[XmlElement("description")]
		public string Description { get; set; } = "";

		[XmlElement("category")]
		public string Category { get; set; } = "";

		[XmlElement("version")]
		public string Version { get; set; } = "";

		[XmlElement("date")]
		public string Date { get; set; } = "";

		[XmlElement("author")]
		public string Author { get; set; } = "";

		[XmlElement("email")]
		public string Email { get; set; } = "";

		[XmlElement("homepage")]
		public string Homepage { get; set; } = "";

		[XmlElement("url")]
		public string Url { get; set; } = "";

		[XmlElement("comment")]
		public string Comment { get; set; } = "";

		[XmlElement("clrmamepro")]
		public ClrmameproElement Clrmamepro { get; set; }
	}
}
