using FluentAssertions;
using LogoVisualizer.Api.Services;
using Xunit;

namespace LogoVisualizer.Tests;

/// <summary>
/// Unit tests for product validation (title and image dimensions).
/// Based on test cases TC-01 through TC-12.
/// </summary>
public class ProductValidationTests
{
    private readonly ProductValidator _validator = new();

    // =====================================================================
    // Title validation (TC-01 to TC-05)
    // =====================================================================

    /// <summary>TC-01: Accept a valid product title.</summary>
    [Fact]
    public void ValidateTitle_WithValidTitle_ReturnsNull()
    {
        var error = _validator.ValidateTitle("Classic T-Shirt");

        error.Should().BeNull();
    }

    /// <summary>TC-03: Single-character title is valid (minimum length).</summary>
    [Fact]
    public void ValidateTitle_SingleCharacter_ReturnsNull()
    {
        var error = _validator.ValidateTitle("A");

        error.Should().BeNull();
    }

    /// <summary>TC-04: Title at maximum length (500 characters) is valid.</summary>
    [Fact]
    public void ValidateTitle_AtMaximumLength_ReturnsNull()
    {
        var error = _validator.ValidateTitle(new string('A', 500));

        error.Should().BeNull();
    }

    /// <summary>TC-02/TC-05: Reject empty, null, whitespace-only, and overlong titles.</summary>
    [Theory]
    [InlineData("",    "required")]   // TC-02: empty string
    [InlineData(null,  "required")]   // null title
    [InlineData("   ", "required")]   // whitespace-only (treated the same as empty)
    public void ValidateTitle_RequiredViolation_ReturnsError(string? title, string expectedFragment)
    {
        var error = _validator.ValidateTitle(title);

        error.Should().NotBeNullOrEmpty();
        error.Should().Contain(expectedFragment);
    }

    [Fact]
    public void ValidateTitle_ExceedsMaximumLength_ReturnsError()
    {
        // TC-05: 501 characters — one over the limit
        var error = _validator.ValidateTitle(new string('A', 501));

        error.Should().NotBeNullOrEmpty();
        error.Should().Contain("500");
    }

    // =====================================================================
    // Image dimension validation
    // =====================================================================

    /// <summary>Accept typical positive image dimensions.</summary>
    [Fact]
    public void ValidateImageDimensions_PositiveDimensions_ReturnsNull()
    {
        var error = _validator.ValidateImageDimensions(800, 600);

        error.Should().BeNull();
    }

    /// <summary>Accept minimum valid dimensions (1x1).</summary>
    [Fact]
    public void ValidateImageDimensions_OneByOne_ReturnsNull()
    {
        var error = _validator.ValidateImageDimensions(1, 1);

        error.Should().BeNull();
    }

    /// <summary>Reject zero or negative width or height.</summary>
    [Theory]
    [InlineData(0,    600)]   // zero width
    [InlineData(800,  0)]     // zero height
    [InlineData(-100, 600)]   // negative width
    [InlineData(800,  -1)]    // negative height
    [InlineData(-1,   -1)]    // both negative
    public void ValidateImageDimensions_NonPositiveDimension_ReturnsError(int width, int height)
    {
        var error = _validator.ValidateImageDimensions(width, height);

        error.Should().NotBeNullOrEmpty();
        error.Should().Contain("positive");
    }
}
