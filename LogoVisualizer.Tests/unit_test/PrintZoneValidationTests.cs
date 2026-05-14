using FluentAssertions;
using LogoVisualizer.Api.Services;
using Xunit;

namespace LogoVisualizer.Tests;

/// <summary>
/// Unit tests for print zone validation (name, coordinates, size, bounds).
/// Based on test cases TC-13 through TC-28.
/// </summary>
public class PrintZoneValidationTests
{
    private readonly PrintZoneValidator _validator = new();

    // =====================================================================
    // Name validation (TC-13 to TC-16)
    // =====================================================================

    /// <summary>TC-13: Accept valid print zone name.</summary>
    [Fact]
    public void ValidateName_WithValidName_ReturnsNull()
    {
        var error = _validator.ValidateName("Front");

        error.Should().BeNull();
    }

    /// <summary>TC-14: Reject empty, null, and whitespace-only names.</summary>
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void ValidateName_NullOrWhitespace_ReturnsError(string? name)
    {
        var error = _validator.ValidateName(name);

        error.Should().NotBeNullOrEmpty();
        error.Should().Contain("required");
    }

    /// <summary>TC-15: Accept maximum allowed name length (200 characters).</summary>
    [Fact]
    public void ValidateName_AtMaximumLength_ReturnsNull()
    {
        var error = _validator.ValidateName(new string('A', 200));

        error.Should().BeNull();
    }

    /// <summary>TC-16: Reject name that exceeds maximum length (201 characters).</summary>
    [Fact]
    public void ValidateName_ExceedsMaximumLength_ReturnsError()
    {
        var error = _validator.ValidateName(new string('A', 201));

        error.Should().NotBeNullOrEmpty();
        error.Should().Contain("200");
    }

    // =====================================================================
    // Coordinate validation (TC-17 to TC-20)
    // =====================================================================

    /// <summary>TC-17: Accept coordinates at origin (0, 0).</summary>
    [Fact]
    public void ValidateCoordinates_AtOrigin_ReturnsNull()
    {
        var error = _validator.ValidateCoordinates(0, 0);

        error.Should().BeNull();
    }

    /// <summary>TC-18: Accept positive coordinates.</summary>
    [Fact]
    public void ValidateCoordinates_PositiveValues_ReturnsNull()
    {
        var error = _validator.ValidateCoordinates(50, 50);

        error.Should().BeNull();
    }

    /// <summary>TC-19/TC-20: Reject any negative coordinate.</summary>
    [Theory]
    [InlineData(-1, 0)]   // negative X only
    [InlineData(0, -1)]   // negative Y only
    [InlineData(-5, -5)]  // both negative
    public void ValidateCoordinates_NegativeValue_ReturnsError(int x, int y)
    {
        var error = _validator.ValidateCoordinates(x, y);

        error.Should().NotBeNullOrEmpty();
        error.Should().Contain("non-negative");
    }

    // =====================================================================
    // Size validation (TC-21 to TC-24)
    // =====================================================================

    /// <summary>TC-21: Accept minimum valid zone size (1x1).</summary>
    [Fact]
    public void ValidateSize_MinimumDimensions_ReturnsNull()
    {
        var error = _validator.ValidateSize(1, 1);

        error.Should().BeNull();
    }

    /// <summary>TC-22: Accept normal zone dimensions.</summary>
    [Fact]
    public void ValidateSize_NormalDimensions_ReturnsNull()
    {
        var error = _validator.ValidateSize(100, 100);

        error.Should().BeNull();
    }

    /// <summary>TC-23/TC-24: Reject zero or negative width/height.</summary>
    [Theory]
    [InlineData(0,   100)]  // zero width
    [InlineData(100, 0)]    // zero height
    [InlineData(-1,  100)]  // negative width
    [InlineData(100, -1)]   // negative height
    public void ValidateSize_NonPositiveDimension_ReturnsError(int width, int height)
    {
        var error = _validator.ValidateSize(width, height);

        error.Should().NotBeNullOrEmpty();
        error.Should().Contain("positive");
    }

    // =====================================================================
    // Bounds validation (TC-25 to TC-28)
    // =====================================================================

    /// <summary>TC-25/TC-27: Accept zone that fits exactly inside image dimensions.</summary>
    [Theory]
    [InlineData(900, 0,   100, 100, 1000, 1000)]  // touching right edge
    [InlineData(0,   900, 100, 100, 1000, 1000)]  // touching bottom edge
    [InlineData(900, 900, 100, 100, 1000, 1000)]  // touching both edges
    [InlineData(0,   0,   500, 500, 1000, 1000)]  // centred, well within bounds
    public void ValidateBounds_WithinImageBounds_ReturnsNull(int x, int y, int width, int height, int imgW, int imgH)
    {
        var error = _validator.ValidateBounds(x, y, width, height, imgW, imgH);

        error.Should().BeNull();
    }

    /// <summary>TC-26/TC-28: Reject zone that overflows image dimensions by even one pixel.</summary>
    [Theory]
    [InlineData(901, 0,   100, 100, 1000, 1000)]  // overflows right by 1 px
    [InlineData(0,   901, 100, 100, 1000, 1000)]  // overflows bottom by 1 px
    public void ValidateBounds_ExceedingImageBounds_ReturnsError(int x, int y, int width, int height, int imgW, int imgH)
    {
        var error = _validator.ValidateBounds(x, y, width, height, imgW, imgH);

        error.Should().NotBeNullOrEmpty();
        error.Should().Contain("bounds");
    }
}
