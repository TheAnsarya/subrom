using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Subrom.Domain.Datfiles {

	public class Datafile {
		public string Build { get; set; } = "";

		public bool Debug { get; set; }

		public Header Header { get; set; } = new();

		public List<Machine> Machines { get; set; } = new();

		public List<Game> Games { get; set; } = new();
	}
}
