using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Subrom.Domain.Datfiles {
	public class Machine {
		public string Name { get; set; } = "";

		public string Description { get; set; } = "";

		public string Year { get; set; } = "";

		public string Manufacturer { get; set; } = "";

		public List<Disk> Disks { get; set; } = new();
	}
}
