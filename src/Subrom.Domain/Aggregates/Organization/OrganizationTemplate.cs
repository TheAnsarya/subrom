namespace Subrom.Domain.Aggregates.Organization;

/// <summary>
/// Represents a folder structure template for organizing ROMs.
/// Templates define how ROMs should be organized into folders based on metadata.
/// </summary>
public class OrganizationTemplate {
	/// <summary>
	/// Unique identifier.
	/// </summary>
	public Guid Id { get; init; } = Guid.NewGuid();

	/// <summary>
	/// User-friendly name for the template.
	/// </summary>
	public required string Name { get; init; }

	/// <summary>
	/// Description of what this template does.
	/// </summary>
	public string? Description { get; init; }

	/// <summary>
	/// The folder path template string using placeholders.
	/// Example: "{System}/{Region}/{FirstLetter}/{Name}"
	/// </summary>
	public required string FolderTemplate { get; init; }

	/// <summary>
	/// The filename template string using placeholders.
	/// Example: "{Name} ({Region}){Extension}"
	/// </summary>
	public required string FileNameTemplate { get; init; }

	/// <summary>
	/// Whether this is a built-in template (cannot be deleted).
	/// </summary>
	public bool IsBuiltIn { get; init; }

	/// <summary>
	/// Whether to use 1G1R (one game one ROM) mode.
	/// </summary>
	public bool Use1G1R { get; init; }

	/// <summary>
	/// Region priority order for 1G1R selection.
	/// Higher priority regions are preferred.
	/// </summary>
	public IReadOnlyList<string> RegionPriority { get; init; } = [];

	/// <summary>
	/// Language priority order for 1G1R selection.
	/// </summary>
	public IReadOnlyList<string> LanguagePriority { get; init; } = [];

	/// <summary>
	/// Categories to exclude from organization.
	/// Examples: "BIOS", "Demo", "Beta", "Proto"
	/// </summary>
	public IReadOnlyList<string> ExcludeCategories { get; init; } = [];

	/// <summary>
	/// Whether to group clones with their parent.
	/// </summary>
	public bool GroupClones { get; init; }

	/// <summary>
	/// Whether to create separate folders for each archive.
	/// </summary>
	public bool SeparateArchiveFolders { get; init; }

	/// <summary>
	/// Date when template was created.
	/// </summary>
	public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

	/// <summary>
	/// Date when template was last modified.
	/// </summary>
	public DateTime? ModifiedAt { get; set; }

	/// <summary>
	/// Creates the default "No-Intro Style" organization template.
	/// </summary>
	public static OrganizationTemplate NoIntroStyle => new() {
		Name = "No-Intro Style",
		Description = "Organizes ROMs by system name, with clean No-Intro naming",
		FolderTemplate = "{System}",
		FileNameTemplate = "{Name}{Extension}",
		IsBuiltIn = true,
		Use1G1R = false
	};

	/// <summary>
	/// Creates the default "By Region" organization template.
	/// </summary>
	public static OrganizationTemplate ByRegion => new() {
		Name = "By Region",
		Description = "Organizes ROMs by system and then by region",
		FolderTemplate = "{System}/{Region}",
		FileNameTemplate = "{Name}{Extension}",
		IsBuiltIn = true,
		Use1G1R = false
	};

	/// <summary>
	/// Creates the "1G1R USA Priority" template.
	/// </summary>
	public static OrganizationTemplate OneGameOneRomUsa => new() {
		Name = "1G1R - USA Priority",
		Description = "One game, one ROM - prefers USA versions, then Europe, then Japan",
		FolderTemplate = "{System}",
		FileNameTemplate = "{Name}{Extension}",
		IsBuiltIn = true,
		Use1G1R = true,
		RegionPriority = ["USA", "World", "Europe", "Japan", "Germany", "France", "Spain", "Italy"],
		LanguagePriority = ["En", "En,Fr", "En,De", "En,Es"]
	};

	/// <summary>
	/// Creates the "Alphabetical" template.
	/// </summary>
	public static OrganizationTemplate Alphabetical => new() {
		Name = "Alphabetical",
		Description = "Organizes ROMs by system and first letter of name",
		FolderTemplate = "{System}/{FirstLetter}",
		FileNameTemplate = "{Name}{Extension}",
		IsBuiltIn = true,
		Use1G1R = false
	};

	/// <summary>
	/// Creates the "RetroArch Style" template for RetroArch organization.
	/// </summary>
	public static OrganizationTemplate RetroArchStyle => new() {
		Name = "RetroArch Style",
		Description = "Organizes ROMs in RetroArch-compatible folder structure",
		FolderTemplate = "roms/{SystemShort}",
		FileNameTemplate = "{Name}{Extension}",
		IsBuiltIn = true,
		Use1G1R = false
	};

	/// <summary>
	/// Gets all built-in templates.
	/// </summary>
	public static IReadOnlyList<OrganizationTemplate> BuiltInTemplates =>
		[NoIntroStyle, ByRegion, OneGameOneRomUsa, Alphabetical, RetroArchStyle];
}

/// <summary>
/// Available placeholders for templates.
/// </summary>
public static class TemplatePlaceholders {
	/// <summary>System/platform name (e.g., "Nintendo - Nintendo Entertainment System")</summary>
	public const string System = "{System}";

	/// <summary>Short system name (e.g., "NES")</summary>
	public const string SystemShort = "{SystemShort}";

	/// <summary>Region code or name (e.g., "USA", "Europe")</summary>
	public const string Region = "{Region}";

	/// <summary>Region short code (e.g., "U", "E", "J")</summary>
	public const string RegionShort = "{RegionShort}";

	/// <summary>Language codes (e.g., "En", "En,Fr")</summary>
	public const string Languages = "{Languages}";

	/// <summary>Game/ROM name without extension</summary>
	public const string Name = "{Name}";

	/// <summary>Clean name without region/flags</summary>
	public const string CleanName = "{CleanName}";

	/// <summary>First letter of name (uppercase, # for numbers)</summary>
	public const string FirstLetter = "{FirstLetter}";

	/// <summary>Release year</summary>
	public const string Year = "{Year}";

	/// <summary>Publisher/developer name</summary>
	public const string Publisher = "{Publisher}";

	/// <summary>File extension including dot (e.g., ".nes")</summary>
	public const string Extension = "{Extension}";

	/// <summary>Parent game name (for clones)</summary>
	public const string Parent = "{Parent}";

	/// <summary>Category (e.g., "Games", "BIOS", "Demo")</summary>
	public const string Category = "{Category}";

	/// <summary>DAT file name</summary>
	public const string DatName = "{DatName}";

	/// <summary>DAT provider (e.g., "No-Intro", "TOSEC")</summary>
	public const string Provider = "{Provider}";

	/// <summary>CRC32 hash</summary>
	public const string Crc = "{Crc}";

	/// <summary>All available placeholders.</summary>
	public static IReadOnlyList<string> All =>
		[System, SystemShort, Region, RegionShort, Languages, Name, CleanName,
		 FirstLetter, Year, Publisher, Extension, Parent, Category, DatName, Provider, Crc];
}
