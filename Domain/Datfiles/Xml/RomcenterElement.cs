using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Subrom.Domain.Hash;

namespace Subrom.Domain.Datfiles.Xml {
	// DTD: <!ELEMENT romcenter EMPTY>
	public class RomcenterElement {
		// DTD: <!ATTLIST romcenter plugin CDATA #IMPLIED>
		[XmlAttribute("plugin")]
		public string Plugin { get; set; } = "";

		// DTD: <!ATTLIST romcenter rommode (merged|split|unmerged) "split">
		[XmlAttribute("rommode")]
		public string RomMode { get; set; } = "split";

		// DTD: <!ATTLIST romcenter biosmode (merged|split|unmerged) "split">
		[XmlAttribute("biosmode")]
		public string BiosMode { get; set; } = "split";

		// DTD: <!ATTLIST romcenter samplemode (merged|unmerged) "merged">
		[XmlAttribute("samplemode")]
		public string SampleMode { get; set; } = "merged";

		// DTD: <!ATTLIST romcenter lockrommode (yes|no) "no">
		[XmlAttribute("lockrommode")]
		public string LockRomMode { get; set; } = "no";

		// DTD: <!ATTLIST romcenter lockbiosmode (yes|no) "no">
		[XmlAttribute("lockbiosmode")]
		public string LockBiosMode { get; set; } = "no";

		// DTD: <!ATTLIST romcenter locksamplemode (yes|no) "no">
		[XmlAttribute("locksamplemode")]
		public string LockSampleMode { get; set; } = "no";
	}
}
