using System.Xml;
using Subrom.Domain.Datfiles;
using Subrom.Domain.Datfiles.Kinds;
using Subrom.Domain.Hash;

namespace Subrom.Infrastructure.Parsers;

/// <summary>
/// Streaming XML parser for DAT files (LogiqX/No-Intro format).
/// Handles large files efficiently by processing elements one at a time.
/// </summary>
public class XmlDatParser : IDatParser {
	public string FormatName => "XML/LogiqX";

	public bool CanParse(Stream stream) {
		try {
			using var reader = new StreamReader(stream, leaveOpen: true);
			var buffer = new char[100];
			var read = reader.Read(buffer, 0, 100);
			stream.Position = 0;

			var content = new string(buffer, 0, read);
			return content.Contains("<?xml") || content.Contains("<datafile");
		} catch {
			return false;
		}
	}

	public async Task<Datafile> ParseAsync(Stream stream, CancellationToken cancellationToken = default) {
		var datafile = new Datafile();

		var settings = new XmlReaderSettings {
			Async = true,
			IgnoreWhitespace = true,
			IgnoreComments = true,
			DtdProcessing = DtdProcessing.Ignore
		};

		using var reader = XmlReader.Create(stream, settings);

		while (await reader.ReadAsync()) {
			cancellationToken.ThrowIfCancellationRequested();

			if (reader.NodeType == XmlNodeType.Element) {
				switch (reader.LocalName.ToLowerInvariant()) {
					case "header":
						datafile.Header = await ParseHeaderAsync(reader);
						break;

					case "game":
						var game = await ParseGameAsync(reader);
						datafile.Games.Add(game);
						break;

					case "machine":
						var machine = await ParseMachineAsync(reader);
						datafile.Machines.Add(machine);
						break;
				}
			}
		}

		return datafile;
	}

	private async Task<Header> ParseHeaderAsync(XmlReader reader) {
		var header = new Header();
		var depth = reader.Depth;

		while (await reader.ReadAsync()) {
			if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth) {
				break;
			}

			if (reader.NodeType == XmlNodeType.Element) {
				var elementName = reader.LocalName.ToLowerInvariant();
				var value = await reader.ReadElementContentAsStringAsync();

				switch (elementName) {
					case "name":
						header.Name = value;
						break;
					case "description":
						header.Description = value;
						break;
					case "version":
						header.Version = value;
						break;
					case "author":
						header.Author = value;
						break;
					case "homepage":
						header.Homepage = value;
						break;
					case "url":
						header.Url = value;
						break;
					case "category":
						header.Category = value;
						break;
					case "date":
						header.Date = value;
						break;
					case "email":
						header.Email = value;
						break;
					case "comment":
						header.Comment = value;
						break;
				}
			}
		}

		return header;
	}

	private async Task<Game> ParseGameAsync(XmlReader reader) {
		var game = new Game {
			Name = reader.GetAttribute("name") ?? "",
			SourceFile = reader.GetAttribute("sourcefile") ?? "",
			CloneOf = reader.GetAttribute("cloneof") ?? "",
			RomOf = reader.GetAttribute("romof") ?? "",
			SampleOf = reader.GetAttribute("sampleof") ?? "",
			Board = reader.GetAttribute("board") ?? "",
			RebuildTo = reader.GetAttribute("rebuildto") ?? "",
			IsBios = reader.GetAttribute("isbios")?.ToLowerInvariant() == "yes"
		};

		if (reader.IsEmptyElement) {
			return game;
		}

		var depth = reader.Depth;

		while (await reader.ReadAsync()) {
			if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth) {
				break;
			}

			if (reader.NodeType == XmlNodeType.Element) {
				switch (reader.LocalName.ToLowerInvariant()) {
					case "description":
						game.Description = await reader.ReadElementContentAsStringAsync();
						break;

					case "year":
						var yearStr = await reader.ReadElementContentAsStringAsync();
						game.Year = Year.From(yearStr);
						break;

					case "manufacturer":
						game.Manufacturer = await reader.ReadElementContentAsStringAsync();
						break;

					case "rom":
						game.Roms.Add(ParseRom(reader));
						break;

					case "disk":
						game.Disks.Add(ParseDisk(reader));
						break;

					case "sample":
						game.Samples.Add(ParseSample(reader));
						break;

					case "archive":
						game.Archives.Add(ParseArchive(reader));
						break;

					case "biosset":
						game.BiosSets.Add(ParseBiosSet(reader));
						break;

					case "release":
						game.Releases.Add(ParseRelease(reader));
						break;
				}
			}
		}

		return game;
	}

	private async Task<Machine> ParseMachineAsync(XmlReader reader) {
		var game = await ParseGameAsync(reader);

		return new Machine {
			Name = game.Name,
			Description = game.Description,
			SourceFile = game.SourceFile,
			IsBios = game.IsBios,
			CloneOf = game.CloneOf,
			RomOf = game.RomOf,
			SampleOf = game.SampleOf,
			Board = game.Board,
			RebuildTo = game.RebuildTo,
			Year = game.Year,
			Manufacturer = game.Manufacturer,
			Comments = game.Comments,
			Releases = game.Releases,
			BiosSets = game.BiosSets,
			Roms = game.Roms,
			Disks = game.Disks,
			Samples = game.Samples,
			Archives = game.Archives
		};
	}

	private static Rom ParseRom(XmlReader reader) {
		var rom = new Rom {
			Name = reader.GetAttribute("name") ?? "",
			Merge = reader.GetAttribute("merge") ?? "",
			Date = reader.GetAttribute("date") ?? ""
		};

		var sizeStr = reader.GetAttribute("size");
		if (long.TryParse(sizeStr, out var size)) {
			rom.Size = size;
		}

		var crc = reader.GetAttribute("crc");
		if (!string.IsNullOrEmpty(crc)) {
			rom.Crc = Crc.From(crc.ToLowerInvariant());
		}

		var md5 = reader.GetAttribute("md5");
		if (!string.IsNullOrEmpty(md5)) {
			rom.Md5 = Md5.From(md5.ToLowerInvariant());
		}

		var sha1 = reader.GetAttribute("sha1");
		if (!string.IsNullOrEmpty(sha1)) {
			rom.Sha1 = Sha1.From(sha1.ToLowerInvariant());
		}

		var status = reader.GetAttribute("status");
		if (!string.IsNullOrEmpty(status)) {
			rom.Status = StatusKind.From(status);
		}

		return rom;
	}

	private static Disk ParseDisk(XmlReader reader) {
		return new Disk {
			Name = reader.GetAttribute("name") ?? "",
			Sha1 = reader.GetAttribute("sha1") ?? "",
			Md5 = reader.GetAttribute("md5") ?? "",
			Merge = reader.GetAttribute("merge") ?? "",
			Status = StatusKind.From(reader.GetAttribute("status") ?? "good")
		};
	}

	private static Sample ParseSample(XmlReader reader) {
		return new Sample {
			Name = reader.GetAttribute("name") ?? ""
		};
	}

	private static Archive ParseArchive(XmlReader reader) {
		return new Archive {
			Name = reader.GetAttribute("name") ?? ""
		};
	}

	private static BiosSet ParseBiosSet(XmlReader reader) {
		return new BiosSet {
			Name = reader.GetAttribute("name") ?? "",
			Description = reader.GetAttribute("description") ?? "",
			Default = reader.GetAttribute("default")?.ToLowerInvariant() == "yes"
		};
	}

	private static Release ParseRelease(XmlReader reader) {
		return new Release {
			Name = reader.GetAttribute("name") ?? "",
			Region = reader.GetAttribute("region") ?? "",
			Language = reader.GetAttribute("language") ?? "",
			Date = reader.GetAttribute("date") ?? "",
			Default = reader.GetAttribute("default")?.ToLowerInvariant() == "yes"
		};
	}
}
