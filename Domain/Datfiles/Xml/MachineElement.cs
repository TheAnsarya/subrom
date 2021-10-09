using System;
using System.Xml.Serialization;

namespace Subrom.Domain.Datfiles.Xml {
	// DTD: 
	public class MachineElement {
		// DTD: 
		[XmlAttribute("name")]
		public string Name { get; set; } = "";

		// DTD: 
		[XmlAttribute("isbios")]
		public string IsBios { get; set; } = "";

		// DTD: 
		[XmlAttribute("isdevice")]
		public string IsDevice { get; set; } = "";

		// DTD: 
		[XmlAttribute("ismechanical")]
		public string IsMechanical { get; set; } = "";

		// DTD: 
		[XmlAttribute("runnable")]
		public string Runnable { get; set; } = "";

		// DTD: 
		[XmlElement("description")]
		public string Description { get; set; } = "";

		// DTD: 
		[XmlElement("year")]
		public string Year { get; set; } = "";

		// DTD: 
		[XmlElement("manufacturer")]
		public string Manufacturer { get; set; } = "";

		// DTD: 
		[XmlElement("disk")]
		public DiskElement[] Disks { get; set; } = Array.Empty<DiskElement>();

		// DTD: 
		[XmlElement("rom")]
		public RomElement[] Roms { get; set; } = Array.Empty<RomElement>();
	}
}
