using FluentAssertions;
using LogoVisualizer.Api.Services;
using Xunit;

namespace LogoVisualizer.Tests;

/// <summary>
/// Unit tests for print technique validation.
/// Based on test cases TC-29 through TC-33.
/// </summary>
public class PrintTechniqueValidationTests
{
    private readonly PrintTechniqueValidator _validator = new();

    private readonly Dictionary<string, int> _knownTechniques = new()
    {
        { "screen_print", 1 },
        { "embroidery", 2 },
        { "sublimation", 3 },
        { "engraving", 4 },
        { "digital_print", 5 },
        { "pad_print", 6 }
    };

    // =====================================================================
    // Print Technique Validation (TC-29 to TC-33)
    // =====================================================================

    /// <summary>TC-29: Accept existing print technique (screen_print)</summary>
    [Fact]
    public void ValidateTechniqueName_WithExistingTechnique_ReturnsValid()
    {
        // Arrange
        var techniqueName = "screen_print";

        // Act
        var (isValid, techniqueId, error) = _validator.ValidateTechniqueName(techniqueName, _knownTechniques);

        // Assert
        isValid.Should().BeTrue();
        techniqueId.Should().Be(1);
        error.Should().BeNull();
    }

    /// <summary>TC-30: Reject unknown print technique (laser_cut)</summary>
    [Fact]
    public void ValidateTechniqueName_WithUnknownTechnique_ReturnsInvalid()
    {
        // Arrange
        var techniqueName = "laser_cut";

        // Act
        var (isValid, techniqueId, error) = _validator.ValidateTechniqueName(techniqueName, _knownTechniques);

        // Assert
        isValid.Should().BeFalse();
        techniqueId.Should().Be(0);
        error.Should().NotBeNullOrEmpty();
        error.Should().Contain("Unknown");
    }

    /// <summary>TC-31: Accept one allowed technique</summary>
    [Fact]
    public void ValidateTechniqueNames_WithSingleTechnique_ReturnsValidId()
    {
        // Arrange
        var techniques = new[] { "screen_print" };

        // Act
        var (ids, errors) = _validator.ValidateTechniqueNames(techniques, _knownTechniques);

        // Assert
        ids.Should().HaveCount(1);
        ids.Should().Contain(1);
        errors.Should().BeEmpty();
    }

    /// <summary>TC-32: Accept multiple allowed techniques</summary>
    [Fact]
    public void ValidateTechniqueNames_WithMultipleTechniques_ReturnsAllValidIds()
    {
        // Arrange
        var techniques = new[] { "screen_print", "embroidery" };

        // Act
        var (ids, errors) = _validator.ValidateTechniqueNames(techniques, _knownTechniques);

        // Assert
        ids.Should().HaveCount(2);
        ids.Should().Contain(new[] { 1, 2 });
        errors.Should().BeEmpty();
    }

    /// <summary>TC-33: Handle empty allowed techniques list (valid edge case)</summary>
    [Fact]
    public void ValidateTechniqueNames_WithEmptyList_ReturnsEmpty()
    {
        // Arrange
        var techniques = new string[] { };

        // Act
        var (ids, errors) = _validator.ValidateTechniqueNames(techniques, _knownTechniques);

        // Assert
        ids.Should().BeEmpty();
        errors.Should().BeEmpty();
    }

    /// <summary>Handle case-insensitive technique names</summary>
    [Fact]
    public void ValidateTechniqueName_CaseInsensitive_ReturnsValid()
    {
        // Arrange
        var techniqueName = "SCREEN_PRINT";

        // Act
        var (isValid, techniqueId, error) = _validator.ValidateTechniqueName(techniqueName, _knownTechniques);

        // Assert
        isValid.Should().BeTrue();
        techniqueId.Should().Be(1);
        error.Should().BeNull();
    }

    /// <summary>Reject null technique name</summary>
    [Fact]
    public void ValidateTechniqueName_WithNull_ReturnsInvalid()
    {
        // Arrange
        string? techniqueName = null;

        // Act
        var (isValid, techniqueId, error) = _validator.ValidateTechniqueName(techniqueName, _knownTechniques);

        // Assert
        isValid.Should().BeFalse();
        error.Should().NotBeNullOrEmpty();
    }

    /// <summary>Reject mixed valid/invalid techniques</summary>
    [Fact]
    public void ValidateTechniqueNames_WithMixedValidInvalid_ReturnsOnlyValid()
    {
        // Arrange
        var techniques = new[] { "screen_print", "laser_cut", "embroidery" };

        // Act
        var (ids, errors) = _validator.ValidateTechniqueNames(techniques, _knownTechniques);

        // Assert
        ids.Should().HaveCount(2);
        ids.Should().Contain(new[] { 1, 2 });
        errors.Should().HaveCount(1);
        errors.First().Should().Contain("laser_cut");
    }
}
