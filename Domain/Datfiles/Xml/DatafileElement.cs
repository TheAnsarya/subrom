using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Subrom.Domain.Hash;

namespace Subrom.Domain.Datfiles.Xml {
	// TODO: must remove dtd line from the xml file before processing otherwise it errors
	//[XmlRoot("datafile", Namespace = "http://www.logiqx.com/Dats/datafile.dtd")]
	[XmlRoot("datafile")]
	public class DatafileElement {
		[XmlElement("debug")]
		public bool Debug { get; set; }

		[XmlElement("header")]
		public HeaderElement Header { get; set; } = new();

		[XmlElement("machine")]
		public MachineElement[] Machines { get; set; }

		[XmlElement("game")]
		public GameElement[] Games { get; set; }

		public Datafile ToDatafile() {
			var dat = new Datafile() {
				Header = new Header() {
					Author = Header.Author,
					Category = Header.Category,
					Clrmamepro = new Clrmamepro() {
						Forcepacking = Header.Clrmamepro?.Forcepacking ?? "",
					},
					Comment = Header.Comment,
					Date = Header.Date,
					Description = Header.Description,
					Email = Header.Email,
					Homepage = Header.Homepage,
					Name = Header.Name,
					Url = Header.Url,
					Version = Header.Version,
				},
				Games = Games.Select(x => new Game() {
					Description = x.Description,
					Name = x.Name,
					Roms = x.Roms.Select(y => new Rom() {
						Crc = Crc.From(y.Crc),
						Md5 = Md5.From(y.Md5),
						Name = y.Name,
						Sha1 = Sha1.From(y.Sha1),
						Size = y.Size,
					})
					.ToList(),
				})
				.ToList(),
			};

			return dat;
		}
	}
}
