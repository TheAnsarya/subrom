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

		public string? SourceFile { get; set; }

		public string? CloneOf { get; set; }

		public string? RomOf { get; set; }

		public string? SampleOf { get; set; }

		public string? Board { get; set; }

		public string? RebuildTo { get; set; }

		public List<string> Comments { get; set; } = [];

		public List<Release> Releases { get; set; } = [];

		public List<BiosSet> BiosSets { get; set; } = [];

		public List<Disk> Disks { get; set; } = [];

		public List<Rom> Roms { get; set; } = [];

		public List<Sample> Samples { get; set; } = [];

		public List<Archive> Archives { get; set; } = [];
	}
}
