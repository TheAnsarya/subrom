using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Subrom.Domain.Datfiles.Kinds;
using Subrom.Domain.Hash;

namespace Subrom.Domain.Datfiles {
	public class Clrmamepro {
		public string Header { get; set; } = "";

		// VALUES: (none|split|full) "split">
		public ForceMergingKind ForceMerging { get; set; } = ForceMergingKind.From("split");

		// VALUES: (obsolete|required|ignore) "obsolete"
		public ForceNoDumpKind ForceNoDump { get; set; } = ForceNoDumpKind.From("obsolete");

		// VALUES: (zip|unzip) "zip"
		public ForcePackingKind ForcePacking { get; set; } = ForcePackingKind.From("zip");
	}
}
