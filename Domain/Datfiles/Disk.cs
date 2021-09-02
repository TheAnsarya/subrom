using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Subrom.Domain.Hash;

namespace Subrom.Domain.Datfiles {
	public class Disk {
		public string Name { get; set; } = "";

		public Sha1 Sha1 { get; set; }
	}
}
