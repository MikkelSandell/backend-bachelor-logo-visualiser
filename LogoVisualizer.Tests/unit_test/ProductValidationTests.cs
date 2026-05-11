using FluentAssertions;
using LogoVisualizer.Api.Services;
using Xunit;

namespace LogoVisualizer.Tests;

/// <summary>
/// Unit tests for product validation (title and image).
/// Based on test cases TC-01 through TC-12.
/// </summary>
public class ProductValidationTests
{
    private readonly ProductValidator _validator = new();

    // =====================================================================
    // Product Title Validation (TC-01 to TC-05)
    // =====================================================================

    /// <summary>TC-01: Accept valid product title</summary>
    [Fact]
    public void ValidateTitle_WithValidTitle_ReturnsNull()
    {
        // Arrange
        var title = "Classic T-Shirt";

        // Act
        var error = _validator.ValidateTitle(title);

        // Assert
        error.Should().BeNull();
    }

    /// <summary>TC-02: Reject empty product title</summary>
    [Fact]
    public void ValidateTitle_WithEmptyTitle_ReturnsError()
    {
        // Arrange
        var title = "";

        // Act
        var error = _validator.ValidateTitle(title);

        // Assert
        error.Should().NotBeNullOrEmpty();
        error.Should().Contain("required");
    }

    /// <summary>TC-03: Accept minimum title length (1 character)</summary>
    [Fact]
    public void ValidateTitle_WithOneCharacter_ReturnsNull()
    {
        // Arrange
        var title = "A";

        // Act
        var error = _validator.ValidateTitle(title);

        // Assert
        error.Should().BeNull();
    }

    /// <summary>TC-04: Accept maximum title length (500 characters)</summary>
    [Fact]
    public void ValidateTitle_WithMaximumLength_ReturnsNull()
    {
        // Arrange
        var title = new string('A', 500);

        // Act
        var error = _validator.ValidateTitle(title);

        // Assert
        error.Should().BeNull();
    }

    /// <summary>TC-05: Reject title above maximum length (501 characters)</summary>
    [Fact]
    public void ValidateTitle_ExceedsMaximumLength_ReturnsError()
    {
        // Arrange
        var title = new string('A', 501);

        // Act
        var error = _validator.ValidateTitle(title);

        // Assert
        error.Should().NotBeNullOrEmpty();
        error.Should().Contain("500");
    }

    // =====================================================================
    // Product Image Dimensions Validation (implicit validation for TC-06-TC-12)
    // Note: Image format validation (PNG/JPG) is tested in FileUploadValidationTests
    // =====================================================================

    /// <summary>Validate product image has positive dimensions</summary>
    [Fact]
    public void ValidateImageDimensions_WithPositiveDimensions_ReturnsNull()
    {
        // Arrange
        int width = 800, height = 600;

        // Act
        var error = _validator.ValidateImageDimensions(width, height);

        // Assert
        error.Should().BeNull();
    }

    /// <summary>Reject image with zero width</summary>
    [Fact]
    public void ValidateImageDimensions_WithZeroWidth_ReturnsError()
    {
        // Arrange
        int width = 0, height = 600;

        // Act
        var error = _validator.ValidateImageDimensions(width, height);

        // Assert
        error.Should().NotBeNullOrEmpty();
        error.Should().Contain("positive");
    }

    /// <summary>Reject image with negative dimensions</summary>
    [Fact]
    public void ValidateImageDimensions_WithNegativeDimensions_ReturnsError()
    {
        // Arrange
        int width = -100, height = 600;

        // Act
        var error = _validator.ValidateImageDimensions(width, height);

        // Assert
        error.Should().NotBeNullOrEmpty();
        error.Should().Contain("positive");
    }
}
