using System;
using System.Xml.Serialization;

namespace Subrom.Domain.Datfiles.Xml {
	// DTO: <!ELEMENT game (comment*, description, year?, manufacturer?, release*, biosset*, rom*, disk*, sample*, archive*)>
	public class GameElement {
		// DTD: <!ATTLIST game name CDATA #REQUIRED>
		[XmlAttribute("name")]
		public string Name { get; set; } = "";

		// DTD: <!ATTLIST game sourcefile CDATA #IMPLIED>
		[XmlAttribute("sourcefile")]
		public string SourceFile { get; set; } = "";

		// DTD: <!ATTLIST game isbios (yes|no) "no">
		[XmlAttribute("isbios")]
		public string IsBios { get; set; } = "no";

		// DTD: <!ATTLIST game cloneof CDATA #IMPLIED>
		[XmlAttribute("cloneof")]
		public string CloneOf { get; set; } = "";

		// DTD: <!ATTLIST game romof CDATA #IMPLIED>
		[XmlAttribute("romof")]
		public string RomOf { get; set; } = "";

		// DTD: <!ATTLIST game sampleof CDATA #IMPLIED>
		[XmlAttribute("sampleof")]
		public string SampleOf { get; set; } = "";

		// DTD: <!ATTLIST game board CDATA #IMPLIED>
		[XmlAttribute("board")]
		public string Board { get; set; } = "";

		// DTD: <!ATTLIST game rebuildto CDATA #IMPLIED>
		[XmlAttribute("rebuildto")]
		public string RebuildTo { get; set; } = "";

		// DTD: <!ELEMENT year (#PCDATA)>
		[XmlElement("year")]
		public string Year { get; set; } = "";

		// DTD: <!ELEMENT manufacturer (#PCDATA)>
		[XmlElement("manufacturer")]
		public string Manufacturer { get; set; } = "";

		// DTD: 
		[XmlElement("description")]
		public string Description { get; set; } = "";

		// DTD: 
		[XmlElement("comment")]
		public string[] Comments { get; set; } = Array.Empty<string>();

		// DTD: <!ELEMENT release EMPTY>
		[XmlElement("release")]
		public ReleaseElement[] Releases { get; set; } = Array.Empty<ReleaseElement>();

		// DTD: <!ELEMENT biosset EMPTY>
		[XmlElement("biosset")]
		public BiosSetElement[] BiosSets { get; set; } = Array.Empty<BiosSetElement>();

		// DTD: <!ELEMENT rom EMPTY>
		[XmlElement("rom")]
		public RomElement[] Roms { get; set; } = Array.Empty<RomElement>();

		// DTD: <!ELEMENT disk EMPTY>
		[XmlElement("disk")]
		public DiskElement[] Disks { get; set; } = Array.Empty<DiskElement>();

		// DTD: <!ELEMENT sample EMPTY>
		[XmlElement("sample")]
		public SampleElement[] Samples { get; set; } = Array.Empty<SampleElement>();

		// DTD: <!ELEMENT archive EMPTY>
		[XmlElement("archive")]
		public ArchiveElement[] Archives { get; set; } = Array.Empty<ArchiveElement>();
	}
}
