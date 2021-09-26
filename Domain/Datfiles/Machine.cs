﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Subrom.Domain.Datfiles.Subitem;

namespace Subrom.Domain.Datfiles {
	public class Machine {
		public string Name { get; set; } = "";

		public string Description { get; set; } = "";

		public Year Year { get; set; }

		public string Manufacturer { get; set; } = "";

		public List<Disk> Disks { get; set; } = new();

		public List<Rom> Roms { get; set; } = new();
	}
}
