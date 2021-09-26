using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Subrom.Domain.Datfiles {
	public class Game {
		public string Name { get; set; } = "";

		public string Description { get; set; } = "";

		public List<Rom> Roms { get; set; } = new();
	}
}
