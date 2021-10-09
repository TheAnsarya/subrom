using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Subrom.Domain.Hash;

namespace Subrom.Domain.Datfiles.Xml {
	// DTD: <!ELEMENT clrmamepro EMPTY>
	public class ClrmameproElement {
		// DTD:<!ATTLIST clrmamepro header CDATA #IMPLIED>
		[XmlAttribute("header")]
		public string Header { get; set; } = "";

		// DTD: <!ATTLIST clrmamepro forcemerging (none|split|full) "split">
		[XmlAttribute("forcemerging")]
		public string ForceMerging { get; set; } = "split";

		// DTD: <!ATTLIST clrmamepro forcenodump (obsolete|required|ignore) "obsolete">
		[XmlAttribute("forcenodump")]
		public string ForceNoDump { get; set; } = "obsolete";

		// DTD: <!ATTLIST clrmamepro forcepacking (zip|unzip) "zip">
		[XmlAttribute("forcepacking")]
		public string ForcePacking { get; set; } = "zip";
	}
}
