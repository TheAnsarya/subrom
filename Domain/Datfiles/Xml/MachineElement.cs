using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Subrom.Domain.Datfiles.Subitem;

namespace Subrom.Domain.Datfiles.Xml {
	public class MachineElement {
		[XmlAttribute("name")]
		public string Name { get; set; } = "";

		[XmlElement("description")]
		public string Description { get; set; } = "";

		[XmlElement("year")]
		public string Year { get; set; } = "";

		[XmlElement("manufacturer")]
		public string Manufacturer { get; set; } = "";

		[XmlElement("disk")]
		public DiskElement[] Disks { get; set; } = Array.Empty<DiskElement>();

		[XmlElement("rom")]
		public RomElement[] Roms { get; set; } = Array.Empty<RomElement>();
	}
}
