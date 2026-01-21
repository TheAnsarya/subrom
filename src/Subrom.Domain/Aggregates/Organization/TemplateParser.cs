using System.Text.RegularExpressions;

namespace Subrom.Domain.Aggregates.Organization;

/// <summary>
/// Context containing values for template placeholder substitution.
/// </summary>
public partial record TemplateContext {
	/// <summary>System/platform name.</summary>
	public string? System { get; init; }

	/// <summary>Short system name.</summary>
	public string? SystemShort { get; init; }

	/// <summary>Region name.</summary>
	public string? Region { get; init; }

	/// <summary>Short region code.</summary>
	public string? RegionShort { get; init; }

	/// <summary>Languages.</summary>
	public string? Languages { get; init; }

	/// <summary>ROM/game name.</summary>
	public required string Name { get; init; }

	/// <summary>Clean name without region/flags.</summary>
	public string? CleanName { get; init; }

	/// <summary>First letter for alphabetical organization.</summary>
	public string? FirstLetter { get; init; }

	/// <summary>Release year.</summary>
	public string? Year { get; init; }

	/// <summary>Publisher/developer.</summary>
	public string? Publisher { get; init; }

	/// <summary>File extension including dot.</summary>
	public required string Extension { get; init; }

	/// <summary>Parent game name (for clones).</summary>
	public string? Parent { get; init; }

	/// <summary>Category (Games, BIOS, etc).</summary>
	public string? Category { get; init; }

	/// <summary>DAT file name.</summary>
	public string? DatName { get; init; }

	/// <summary>DAT provider name.</summary>
	public string? Provider { get; init; }

	/// <summary>CRC32 hash.</summary>
	public string? Crc { get; init; }

	/// <summary>
	/// Computes the first letter for a name.
	/// Returns "#" for names starting with numbers.
	/// </summary>
	public static string ComputeFirstLetter(string name) {
		if (string.IsNullOrEmpty(name)) {
			return "#";
		}

		// Skip leading "The ", "A ", "An "
		var workingName = LeadingArticleRegex().Replace(name, "");
		if (string.IsNullOrEmpty(workingName)) {
			workingName = name;
		}

		var firstChar = workingName[0];
		return char.IsLetter(firstChar)
			? char.ToUpperInvariant(firstChar).ToString()
			: "#";
	}

	/// <summary>
	/// Extracts the clean name from a full ROM name by removing region/language flags.
	/// </summary>
	public static string ExtractCleanName(string fullName) {
		// Remove common ROM naming conventions: (USA), [!], (En,Fr), etc.
		var cleaned = ParenthesesRegex().Replace(fullName, "");
		cleaned = BracketsRegex().Replace(cleaned, "");
		return cleaned.Trim();
	}

	/// <summary>
	/// Extracts region from a ROM name.
	/// </summary>
	public static string? ExtractRegion(string fullName) {
		var match = RegionRegex().Match(fullName);
		return match.Success ? match.Groups[1].Value : null;
	}

	/// <summary>
	/// Extracts languages from a ROM name.
	/// </summary>
	public static string? ExtractLanguages(string fullName) {
		var match = LanguageRegex().Match(fullName);
		return match.Success ? match.Groups[1].Value : null;
	}

	/// <summary>
	/// Gets the short region code from a full region name.
	/// </summary>
	public static string? GetShortRegion(string? region) => region switch {
		"USA" => "U",
		"Europe" => "E",
		"Japan" => "J",
		"World" => "W",
		"Germany" => "G",
		"France" => "F",
		"Spain" => "S",
		"Italy" => "I",
		"Netherlands" => "H",
		"Sweden" => "Sw",
		"Norway" => "No",
		"Denmark" => "Dk",
		"Korea" => "K",
		"China" => "C",
		"Australia" => "A",
		"Brazil" => "B",
		_ => region?[..Math.Min(2, region?.Length ?? 0)]
	};

	[GeneratedRegex(@"^(?:The|A|An)\s+", RegexOptions.IgnoreCase)]
	private static partial Regex LeadingArticleRegex();

	[GeneratedRegex(@"\s*\([^)]*\)")]
	private static partial Regex ParenthesesRegex();

	[GeneratedRegex(@"\s*\[[^\]]*\]")]
	private static partial Regex BracketsRegex();

	[GeneratedRegex(@"\((USA|Europe|Japan|World|Germany|France|Spain|Italy|Netherlands|Sweden|Norway|Denmark|Korea|China|Australia|Brazil)(?:,\s*[^)]+)?\)", RegexOptions.IgnoreCase)]
	private static partial Regex RegionRegex();

	[GeneratedRegex(@"\(((En|Ja|Fr|De|Es|It|Nl|Sv|No|Da|Ko|Zh|Pt)(?:,(?:En|Ja|Fr|De|Es|It|Nl|Sv|No|Da|Ko|Zh|Pt))*)\)", RegexOptions.IgnoreCase)]
	private static partial Regex LanguageRegex();
}

/// <summary>
/// Parses and evaluates organization templates.
/// </summary>
public partial class TemplateParser {
	private static readonly Regex PlaceholderRegex = CreatePlaceholderRegex();

	/// <summary>
	/// Parses a template string and returns the result with placeholder values substituted.
	/// </summary>
	/// <param name="template">The template string with placeholders.</param>
	/// <param name="context">The context containing placeholder values.</param>
	/// <returns>The parsed template with values substituted.</returns>
	public static string Parse(string template, TemplateContext context) {
		ArgumentNullException.ThrowIfNull(template);
		ArgumentNullException.ThrowIfNull(context);

		return PlaceholderRegex.Replace(template, match => {
			var placeholder = match.Value;
			var value = GetPlaceholderValue(placeholder, context);

			// Return empty string for null values
			return SanitizePathComponent(value ?? string.Empty);
		});
	}

	/// <summary>
	/// Validates a template string and returns any errors.
	/// </summary>
	/// <param name="template">The template to validate.</param>
	/// <returns>List of validation errors, empty if valid.</returns>
	public static IReadOnlyList<string> Validate(string template) {
		if (string.IsNullOrWhiteSpace(template)) {
			return ["Template cannot be empty"];
		}

		var errors = new List<string>();
		var matches = PlaceholderRegex.Matches(template);

		foreach (Match match in matches) {
			var placeholder = match.Value;
			if (!TemplatePlaceholders.All.Contains(placeholder)) {
				errors.Add($"Unknown placeholder: {placeholder}");
			}
		}

		// Check for unclosed braces
		var openBraces = template.Count(c => c == '{');
		var closeBraces = template.Count(c => c == '}');
		if (openBraces != closeBraces) {
			errors.Add("Mismatched braces in template");
		}

		return errors;
	}

	/// <summary>
	/// Gets the value for a placeholder from the context.
	/// </summary>
	private static string? GetPlaceholderValue(string placeholder, TemplateContext context) => placeholder switch {
		TemplatePlaceholders.System => context.System,
		TemplatePlaceholders.SystemShort => context.SystemShort,
		TemplatePlaceholders.Region => context.Region,
		TemplatePlaceholders.RegionShort => context.RegionShort,
		TemplatePlaceholders.Languages => context.Languages,
		TemplatePlaceholders.Name => context.Name,
		TemplatePlaceholders.CleanName => context.CleanName,
		TemplatePlaceholders.FirstLetter => context.FirstLetter ?? TemplateContext.ComputeFirstLetter(context.Name),
		TemplatePlaceholders.Year => context.Year,
		TemplatePlaceholders.Publisher => context.Publisher,
		TemplatePlaceholders.Extension => context.Extension,
		TemplatePlaceholders.Parent => context.Parent,
		TemplatePlaceholders.Category => context.Category,
		TemplatePlaceholders.DatName => context.DatName,
		TemplatePlaceholders.Provider => context.Provider,
		TemplatePlaceholders.Crc => context.Crc,
		_ => null
	};

	/// <summary>
	/// Sanitizes a path component by removing invalid characters.
	/// </summary>
	private static string SanitizePathComponent(string value) {
		if (string.IsNullOrEmpty(value)) {
			return value;
		}

		// Remove characters invalid in file/folder names
		var invalid = Path.GetInvalidFileNameChars()
			.Where(c => c != Path.DirectorySeparatorChar && c != Path.AltDirectorySeparatorChar)
			.ToArray();

		var sanitized = string.Join("", value.Split(invalid));

		// Trim trailing dots and spaces (Windows restriction)
		return sanitized.TrimEnd('.', ' ');
	}

	[GeneratedRegex(@"\{[^}]+\}")]
	private static partial Regex CreatePlaceholderRegex();
}
