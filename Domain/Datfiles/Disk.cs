﻿using Subrom.Domain.Hash;

namespace Subrom.Domain.Datfiles {
	public class Disk {
		public string Name { get; set; } = "";

		public Crc Crc { get; set; }

		public Md5 Md5 { get; set; }

		public Sha1 Sha1 { get; set; }
	}
}
