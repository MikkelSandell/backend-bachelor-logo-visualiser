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
        { "screen_print",  1 },
        { "embroidery",    2 },
        { "sublimation",   3 },
        { "engraving",     4 },
        { "digital_print", 5 },
        { "pad_print",     6 }
    };

    // =====================================================================
    // Single technique — valid
    // =====================================================================

    /// <summary>TC-29: Every known technique name resolves to its correct ID.</summary>
    [Theory]
    [InlineData("screen_print",  1)]
    [InlineData("embroidery",    2)]
    [InlineData("sublimation",   3)]
    [InlineData("engraving",     4)]
    [InlineData("digital_print", 5)]
    [InlineData("pad_print",     6)]
    public void ValidateTechniqueName_AllKnownTechniques_ReturnCorrectId(string name, int expectedId)
    {
        var (isValid, techniqueId, error) = _validator.ValidateTechniqueName(name, _knownTechniques);

        isValid.Should().BeTrue();
        techniqueId.Should().Be(expectedId);
        error.Should().BeNull();
    }

    /// <summary>Technique lookup is case-insensitive.</summary>
    [Theory]
    [InlineData("SCREEN_PRINT")]
    [InlineData("Screen_Print")]
    [InlineData("EMBROIDERY")]
    public void ValidateTechniqueName_CaseVariants_ReturnsValid(string name)
    {
        var (isValid, _, error) = _validator.ValidateTechniqueName(name, _knownTechniques);

        isValid.Should().BeTrue();
        error.Should().BeNull();
    }

    // =====================================================================
    // Single technique — invalid
    // =====================================================================

    /// <summary>TC-30: Unknown technique name is rejected.</summary>
    [Fact]
    public void ValidateTechniqueName_UnknownTechnique_ReturnsInvalid()
    {
        var (isValid, techniqueId, error) = _validator.ValidateTechniqueName("laser_cut", _knownTechniques);

        isValid.Should().BeFalse();
        techniqueId.Should().Be(0);
        error.Should().NotBeNullOrEmpty();
        error.Should().Contain("Unknown");
    }

    /// <summary>Null, empty, and whitespace-only names are all rejected.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateTechniqueName_NullOrEmpty_ReturnsInvalid(string? name)
    {
        var (isValid, _, error) = _validator.ValidateTechniqueName(name, _knownTechniques);

        isValid.Should().BeFalse();
        error.Should().NotBeNullOrEmpty();
    }

    // =====================================================================
    // Multiple techniques
    // =====================================================================

    /// <summary>TC-31: Single valid technique resolves correctly.</summary>
    [Fact]
    public void ValidateTechniqueNames_SingleTechnique_ReturnsOneId()
    {
        var (ids, errors) = _validator.ValidateTechniqueNames(["screen_print"], _knownTechniques);

        ids.Should().ContainSingle().Which.Should().Be(1);
        errors.Should().BeEmpty();
    }

    /// <summary>TC-32: Multiple valid techniques all resolve.</summary>
    [Fact]
    public void ValidateTechniqueNames_MultipleTechniques_ReturnsAllIds()
    {
        var (ids, errors) = _validator.ValidateTechniqueNames(["screen_print", "embroidery"], _knownTechniques);

        ids.Should().BeEquivalentTo(new[] { 1, 2 });
        errors.Should().BeEmpty();
    }

    /// <summary>TC-33: Empty list is valid — returns no IDs and no errors.</summary>
    [Fact]
    public void ValidateTechniqueNames_EmptyList_ReturnsEmpty()
    {
        var (ids, errors) = _validator.ValidateTechniqueNames([], _knownTechniques);

        ids.Should().BeEmpty();
        errors.Should().BeEmpty();
    }

    /// <summary>Mixed valid/invalid list: valid IDs are returned, one error per invalid name.</summary>
    [Fact]
    public void ValidateTechniqueNames_MixedList_ReturnsValidIdsAndOneErrorPerInvalidName()
    {
        var (ids, errors) = _validator.ValidateTechniqueNames(["screen_print", "laser_cut", "embroidery"], _knownTechniques);

        ids.Should().BeEquivalentTo(new[] { 1, 2 });
        errors.Should().HaveCount(1);
        errors.First().Should().Contain("laser_cut");
    }

    /// <summary>All-invalid list returns no IDs and an error per entry.</summary>
    [Fact]
    public void ValidateTechniqueNames_AllInvalid_ReturnsNoIdsAndAllErrors()
    {
        var (ids, errors) = _validator.ValidateTechniqueNames(["laser_cut", "3d_print"], _knownTechniques);

        ids.Should().BeEmpty();
        errors.Should().HaveCount(2);
    }
}
