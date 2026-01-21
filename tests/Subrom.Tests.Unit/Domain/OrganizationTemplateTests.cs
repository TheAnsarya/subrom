using Subrom.Domain.Aggregates.Organization;

namespace Subrom.Tests.Unit.Domain;

/// <summary>
/// Unit tests for OrganizationTemplate and TemplateParser.
/// </summary>
public class OrganizationTemplateTests {
	[Fact]
	public void BuiltInTemplates_ShouldContainExpectedTemplates() {
		// Act
		var templates = OrganizationTemplate.BuiltInTemplates;

		// Assert
		Assert.Equal(5, templates.Count);
		Assert.Contains(templates, t => t.Name == "No-Intro Style");
		Assert.Contains(templates, t => t.Name == "By Region");
		Assert.Contains(templates, t => t.Name == "1G1R - USA Priority");
		Assert.Contains(templates, t => t.Name == "Alphabetical");
		Assert.Contains(templates, t => t.Name == "RetroArch Style");
	}

	[Fact]
	public void NoIntroStyle_ShouldHaveCorrectConfiguration() {
		// Act
		var template = OrganizationTemplate.NoIntroStyle;

		// Assert
		Assert.Equal("{System}", template.FolderTemplate);
		Assert.Equal("{Name}{Extension}", template.FileNameTemplate);
		Assert.True(template.IsBuiltIn);
		Assert.False(template.Use1G1R);
	}

	[Fact]
	public void OneGameOneRomUsa_ShouldHaveRegionPriority() {
		// Act
		var template = OrganizationTemplate.OneGameOneRomUsa;

		// Assert
		Assert.True(template.Use1G1R);
		Assert.NotEmpty(template.RegionPriority);
		Assert.Equal("USA", template.RegionPriority[0]);
		Assert.Contains("Europe", template.RegionPriority);
		Assert.Contains("Japan", template.RegionPriority);
	}

	[Fact]
	public void AllBuiltInTemplates_ShouldHaveIsBuiltInTrue() {
		// Act & Assert
		foreach (var template in OrganizationTemplate.BuiltInTemplates) {
			Assert.True(template.IsBuiltIn, $"Template '{template.Name}' should have IsBuiltIn = true");
		}
	}
}

/// <summary>
/// Unit tests for TemplateParser.
/// </summary>
public class TemplateParserTests {
	private readonly TemplateContext _context = new() {
		Name = "Super Mario Bros (USA)",
		Extension = ".nes",
		System = "Nintendo - Nintendo Entertainment System",
		SystemShort = "NES",
		Region = "USA",
		RegionShort = "U",
		Languages = "En",
		CleanName = "Super Mario Bros",
		Year = "1985",
		Publisher = "Nintendo",
		Category = "Games",
		DatName = "Nintendo - Nintendo Entertainment System (20240101-000000)",
		Provider = "No-Intro",
		Crc = "3337ec46"
	};

	[Fact]
	public void Parse_WithSystemPlaceholder_ShouldReturnSystemName() {
		// Act
		var result = TemplateParser.Parse("{System}", _context);

		// Assert
		Assert.Equal("Nintendo - Nintendo Entertainment System", result);
	}

	[Fact]
	public void Parse_WithMultiplePlaceholders_ShouldReplaceAll() {
		// Arrange
		var template = "{System}/{Region}/{Name}{Extension}";

		// Act
		var result = TemplateParser.Parse(template, _context);

		// Assert
		Assert.Equal("Nintendo - Nintendo Entertainment System/USA/Super Mario Bros (USA).nes", result);
	}

	[Fact]
	public void Parse_WithUnknownPlaceholder_ShouldReturnEmptyForThatPart() {
		// Arrange
		var template = "{Unknown}/{Name}";

		// Act
		var result = TemplateParser.Parse(template, _context);

		// Assert
		Assert.Equal("/Super Mario Bros (USA)", result);
	}

	[Fact]
	public void Parse_WithNoPlaceholders_ShouldReturnOriginal() {
		// Arrange
		var template = "roms/nes";

		// Act
		var result = TemplateParser.Parse(template, _context);

		// Assert
		Assert.Equal("roms/nes", result);
	}

	[Fact]
	public void Parse_WithFirstLetterPlaceholder_ShouldComputeCorrectly() {
		// Arrange
		var context = new TemplateContext { Name = "Zelda", Extension = ".nes" };

		// Act
		var result = TemplateParser.Parse("{FirstLetter}", context);

		// Assert
		Assert.Equal("Z", result);
	}

	[Fact]
	public void Parse_WithCrcPlaceholder_ShouldReturnLowercaseCrc() {
		// Act
		var result = TemplateParser.Parse("{Crc}", _context);

		// Assert
		Assert.Equal("3337ec46", result);
	}

	[Fact]
	public void Validate_WithValidTemplate_ShouldReturnNoErrors() {
		// Arrange
		var template = "{System}/{Region}/{Name}{Extension}";

		// Act
		var errors = TemplateParser.Validate(template);

		// Assert
		Assert.Empty(errors);
	}

	[Fact]
	public void Validate_WithUnknownPlaceholder_ShouldReturnError() {
		// Arrange
		var template = "{Unknown}/{Name}";

		// Act
		var errors = TemplateParser.Validate(template);

		// Assert
		Assert.Single(errors);
		Assert.Contains("Unknown placeholder", errors[0]);
	}

	[Fact]
	public void Validate_WithEmptyTemplate_ShouldReturnError() {
		// Act
		var errors = TemplateParser.Validate("");

		// Assert
		Assert.Single(errors);
		Assert.Contains("cannot be empty", errors[0]);
	}

	[Fact]
	public void Validate_WithMismatchedBraces_ShouldReturnError() {
		// Arrange
		var template = "{System/{Name}";

		// Act
		var errors = TemplateParser.Validate(template);

		// Assert
		Assert.Contains(errors, e => e.Contains("Mismatched braces"));
	}
}

/// <summary>
/// Unit tests for TemplateContext helper methods.
/// </summary>
public class TemplateContextTests {
	[Theory]
	[InlineData("Mario", "M")]
	[InlineData("zelda", "Z")]
	[InlineData("The Legend of Zelda", "L")]
	[InlineData("A Boy and His Blob", "B")]
	[InlineData("An American Tail", "A")]
	[InlineData("007 GoldenEye", "#")]
	[InlineData("1942", "#")]
	[InlineData("", "#")]
	public void ComputeFirstLetter_ShouldReturnCorrectLetter(string name, string expected) {
		// Act
		var result = TemplateContext.ComputeFirstLetter(name);

		// Assert
		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData("Super Mario Bros (USA)", "Super Mario Bros")]
	[InlineData("Zelda (Europe) [!]", "Zelda")]
	[InlineData("Game (USA, Europe) (En,Fr)", "Game")]
	[InlineData("Clean Name", "Clean Name")]
	public void ExtractCleanName_ShouldRemoveParensAndBrackets(string fullName, string expected) {
		// Act
		var result = TemplateContext.ExtractCleanName(fullName);

		// Assert
		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData("Game (USA)", "USA")]
	[InlineData("Game (Europe)", "Europe")]
	[InlineData("Game (Japan)", "Japan")]
	[InlineData("Game (World)", "World")]
	[InlineData("Game (USA, Europe)", "USA")]
	[InlineData("Game", null)]
	public void ExtractRegion_ShouldReturnCorrectRegion(string fullName, string? expected) {
		// Act
		var result = TemplateContext.ExtractRegion(fullName);

		// Assert
		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData("USA", "U")]
	[InlineData("Europe", "E")]
	[InlineData("Japan", "J")]
	[InlineData("World", "W")]
	[InlineData("Germany", "G")]
	[InlineData(null, null)]
	public void GetShortRegion_ShouldReturnCorrectCode(string? region, string? expected) {
		// Act
		var result = TemplateContext.GetShortRegion(region);

		// Assert
		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData("Game (En)", "En")]
	[InlineData("Game (En,Fr)", "En,Fr")]
	[InlineData("Game (Ja)", "Ja")]
	[InlineData("Game (USA)", null)]
	public void ExtractLanguages_ShouldReturnCorrectLanguages(string fullName, string? expected) {
		// Act
		var result = TemplateContext.ExtractLanguages(fullName);

		// Assert
		Assert.Equal(expected, result);
	}
}
