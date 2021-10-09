using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Subrom.Domain.Datfiles.Kinds;
using Subrom.Domain.Hash;

namespace Subrom.Domain.Datfiles {
	public class Romcenter {
		public string Plugin { get; set; } = "";

		public RomModeKind RomMode { get; set; } = RomModeKind.From("split");

		public BiosModeKind BiosMode { get; set; } = BiosModeKind.From("split");

		public SampleModeKind SampleMode { get; set; } = SampleModeKind.From("merged");

		public bool LockRomMode { get; set; }

		public bool LockBiosMode { get; set; }

		public bool LockSampleMode { get; set; }
	}
}
