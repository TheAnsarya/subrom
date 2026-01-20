namespace Subrom.Domain.Aggregates.DatFiles;

/// <summary>
/// DAT file provider/source type.
/// </summary>
public enum DatProvider {
	Unknown = 0,
	NoIntro,
	TOSEC,
	Redump,
	GoodSets,
	MAME,
	FinalBurn,
	Custom
}

/// <summary>
/// DAT file format type.
/// </summary>
public enum DatFormat {
	Unknown = 0,
	LogiqxXml,      // Standard No-Intro/Redump XML format
	ClrMamePro,     // ClrMamePro text format
	RomCenter,      // RomCenter format
	DosCenter,      // DOSCenter format
	OfflineList     // Offline List XML format
}
