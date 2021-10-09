using System.Xml.Serialization;

namespace Subrom.Domain.Datfiles {
	public class Header {
		public string Name { get; set; } = "";

		public string Description { get; set; } = "";

		public string Category { get; set; } = "";

		public string Version { get; set; } = "";

		public string Date { get; set; } = "";

		public string Author { get; set; } = "";

		public string Email { get; set; } = "";

		public string Homepage { get; set; } = "";

		public string Url { get; set; } = "";

		public string Comment { get; set; } = "";

		public Clrmamepro Clrmamepro { get; set; } = new();

		public Romcenter Romcenter { get; set; } = new();
	}
}
