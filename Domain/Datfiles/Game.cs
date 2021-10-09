using System;
using System.Collections.Generic;
using Subrom.Domain.Datfiles.Kinds;

namespace Subrom.Domain.Datfiles {
	public class Game {
		public string Name { get; set; } = "";

		public string Description { get; set; } = "";

		public string SourceFile { get; set; } = "";

		public bool IsBios { get; set; }

		public string CloneOf { get; set; } = "";

		public string RomOf { get; set; } = "";

		public string SampleOf { get; set; } = "";

		public string Board { get; set; } = "";

		public string RebuildTo { get; set; } = "";

		public Year? Year { get; set; }

		public string Manufacturer { get; set; } = "";

		public List<string> Comments { get; set; } = new();

		public List<Release> Releases { get; set; } = new();

		public List<BiosSet> BiosSets { get; set; } = new();

		public List<Rom> Roms { get; set; } = new();

		public List<Disk> Disks { get; set; } = new();

		public List<Sample> Samples { get; set; } = new();

		public List<Archive> Archives { get; set; } = new();
	}
}
