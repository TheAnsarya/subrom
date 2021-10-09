using System.Collections.Generic;
using Subrom.Domain.Datfiles.Kinds;

namespace Subrom.Domain.Datfiles {
	public class Machine {
		public string Name { get; set; } = "";

		public bool IsBios { get; set; }

		public bool IsDevice { get; set; }

		public bool IsMechanical { get; set; }

		public bool Runnable { get; set; }

		public string Description { get; set; } = "";

		public Year? Year { get; set; }

		public string Manufacturer { get; set; } = "";

		public List<Disk> Disks { get; set; } = new();

		public List<Rom> Roms { get; set; } = new();
	}
}
