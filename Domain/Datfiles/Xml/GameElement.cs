using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Subrom.Domain.Datfiles.Xml {
	public class GameElement {
		[XmlAttribute("name")]
		public string Name { get; set; } = "";

		[XmlElement("description")]
		public string Description { get; set; } = "";

		[XmlElement("rom")]
		public RomElement[] Roms { get; set; } = Array.Empty<RomElement>();
	}
}
