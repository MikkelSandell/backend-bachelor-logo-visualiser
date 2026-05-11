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
    // Print Zone Name Validation (TC-13 to TC-16)
    // =====================================================================

    /// <summary>TC-13: Accept valid print zone name</summary>
    [Fact]
    public void ValidateName_WithValidName_ReturnsNull()
    {
        // Arrange
        var name = "Front";

        // Act
        var error = _validator.ValidateName(name);

        // Assert
        error.Should().BeNull();
    }

    /// <summary>TC-14: Reject empty print zone name</summary>
    [Fact]
    public void ValidateName_WithEmptyName_ReturnsError()
    {
        // Arrange
        var name = "";

        // Act
        var error = _validator.ValidateName(name);

        // Assert
        error.Should().NotBeNullOrEmpty();
        error.Should().Contain("required");
    }

    /// <summary>TC-15: Accept maximum length zone name (200 characters)</summary>
    [Fact]
    public void ValidateName_WithMaximumLength_ReturnsNull()
    {
        // Arrange
        var name = new string('A', 200);

        // Act
        var error = _validator.ValidateName(name);

        // Assert
        error.Should().BeNull();
    }

    /// <summary>TC-16: Reject zone name above maximum length (201 characters)</summary>
    [Fact]
    public void ValidateName_ExceedsMaximumLength_ReturnsError()
    {
        // Arrange
        var name = new string('A', 201);

        // Act
        var error = _validator.ValidateName(name);

        // Assert
        error.Should().NotBeNullOrEmpty();
        error.Should().Contain("200");
    }

    // =====================================================================
    // Print Zone Coordinates Validation (TC-17 to TC-20)
    // =====================================================================

    /// <summary>TC-17: Accept coordinates at origin (0, 0)</summary>
    [Fact]
    public void ValidateCoordinates_AtOrigin_ReturnsNull()
    {
        // Arrange
        int x = 0, y = 0;

        // Act
        var error = _validator.ValidateCoordinates(x, y);

        // Assert
        error.Should().BeNull();
    }

    /// <summary>TC-18: Accept positive coordinates</summary>
    [Fact]
    public void ValidateCoordinates_WithPositiveValues_ReturnsNull()
    {
        // Arrange
        int x = 50, y = 50;

        // Act
        var error = _validator.ValidateCoordinates(x, y);

        // Assert
        error.Should().BeNull();
    }

    /// <summary>TC-19: Reject negative X coordinate</summary>
    [Fact]
    public void ValidateCoordinates_WithNegativeX_ReturnsError()
    {
        // Arrange
        int x = -1, y = 0;

        // Act
        var error = _validator.ValidateCoordinates(x, y);

        // Assert
        error.Should().NotBeNullOrEmpty();
        error.Should().Contain("non-negative");
    }

    /// <summary>TC-20: Reject negative Y coordinate</summary>
    [Fact]
    public void ValidateCoordinates_WithNegativeY_ReturnsError()
    {
        // Arrange
        int x = 0, y = -1;

        // Act
        var error = _validator.ValidateCoordinates(x, y);

        // Assert
        error.Should().NotBeNullOrEmpty();
        error.Should().Contain("non-negative");
    }

    // =====================================================================
    // Print Zone Size Validation (TC-21 to TC-24)
    // =====================================================================

    /// <summary>TC-21: Accept minimum valid zone size (1x1)</summary>
    [Fact]
    public void ValidateSize_WithMinimumDimensions_ReturnsNull()
    {
        // Arrange
        int width = 1, height = 1;

        // Act
        var error = _validator.ValidateSize(width, height);

        // Assert
        error.Should().BeNull();
    }

    /// <summary>TC-22: Accept normal zone size</summary>
    [Fact]
    public void ValidateSize_WithNormalDimensions_ReturnsNull()
    {
        // Arrange
        int width = 100, height = 100;

        // Act
        var error = _validator.ValidateSize(width, height);

        // Assert
        error.Should().BeNull();
    }

    /// <summary>TC-23: Reject zero width</summary>
    [Fact]
    public void ValidateSize_WithZeroWidth_ReturnsError()
    {
        // Arrange
        int width = 0, height = 100;

        // Act
        var error = _validator.ValidateSize(width, height);

        // Assert
        error.Should().NotBeNullOrEmpty();
        error.Should().Contain("positive");
    }

    /// <summary>TC-24: Reject zero height</summary>
    [Fact]
    public void ValidateSize_WithZeroHeight_ReturnsError()
    {
        // Arrange
        int width = 100, height = 0;

        // Act
        var error = _validator.ValidateSize(width, height);

        // Assert
        error.Should().NotBeNullOrEmpty();
        error.Should().Contain("positive");
    }

    // =====================================================================
    // Print Zone Bounds Validation (TC-25 to TC-28)
    // =====================================================================

    /// <summary>TC-25: Accept zone exactly inside image width</summary>
    [Fact]
    public void ValidateBounds_ExactlyAtImageBounds_ReturnsNull()
    {
        // Arrange
        int x = 900, y = 900, width = 100, height = 100;
        int imageWidth = 1000, imageHeight = 1000;

        // Act
        var error = _validator.ValidateBounds(x, y, width, height, imageWidth, imageHeight);

        // Assert
        error.Should().BeNull();
    }

    /// <summary>TC-26: Reject zone exceeding image width by 1px</summary>
    [Fact]
    public void ValidateBounds_ExceedsImageWidthByOnePx_ReturnsError()
    {
        // Arrange
        int x = 901, y = 0, width = 100, height = 100;
        int imageWidth = 1000, imageHeight = 1000;

        // Act
        var error = _validator.ValidateBounds(x, y, width, height, imageWidth, imageHeight);

        // Assert
        error.Should().NotBeNullOrEmpty();
        error.Should().Contain("bounds");
    }

    /// <summary>TC-27: Accept zone exactly inside image height</summary>
    [Fact]
    public void ValidateBounds_WithinImageHeight_ReturnsNull()
    {
        // Arrange
        int x = 0, y = 900, width = 100, height = 100;
        int imageWidth = 1000, imageHeight = 1000;

        // Act
        var error = _validator.ValidateBounds(x, y, width, height, imageWidth, imageHeight);

        // Assert
        error.Should().BeNull();
    }

    /// <summary>TC-28: Reject zone exceeding image height by 1px</summary>
    [Fact]
    public void ValidateBounds_ExceedsImageHeightByOnePx_ReturnsError()
    {
        // Arrange
        int x = 0, y = 901, width = 100, height = 100;
        int imageWidth = 1000, imageHeight = 1000;

        // Act
        var error = _validator.ValidateBounds(x, y, width, height, imageWidth, imageHeight);

        // Assert
        error.Should().NotBeNullOrEmpty();
        error.Should().Contain("bounds");
    }

    /// <summary>Zone at origin with positive dimensions fits image</summary>
    [Fact]
    public void ValidateBounds_AllZeroOriginWithinImage_ReturnsNull()
    {
        // Arrange
        int x = 0, y = 0, width = 500, height = 500;
        int imageWidth = 1000, imageHeight = 1000;

        // Act
        var error = _validator.ValidateBounds(x, y, width, height, imageWidth, imageHeight);

        // Assert
        error.Should().BeNull();
    }
}
