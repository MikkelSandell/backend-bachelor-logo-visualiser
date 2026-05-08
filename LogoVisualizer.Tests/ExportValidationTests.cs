using FluentAssertions;
using LogoVisualizer.Api.DTOs;
using LogoVisualizer.Api.Services;
using Xunit;

namespace LogoVisualizer.Tests;

/// <summary>
/// Unit tests for PNG and PDF export validation.
/// Based on test cases TC-51 through TC-59.
/// </summary>
public class ExportValidationTests
{
    private readonly ExportValidator _validator = new();

    // =====================================================================
    // PNG Export Validation (TC-51 to TC-55)
    // =====================================================================

    /// <summary>TC-51: Accept valid PNG export request (product + background + logo + placement)</summary>
    [Fact]
    public void ValidatePngRequest_WithLogoPlacements_ReturnsNull()
    {
        // Arrange
        var placements = new List<ZonePlacement>
        {
            new ZonePlacement { ZoneId = "1", LogoId = "logo1", LogoX = 10, LogoY = 10, LogoWidth = 100, LogoHeight = 100 }
        };
        var textPlacements = new List<TextPlacement>();

        // Act
        var error = _validator.ValidatePngRequest(placements, textPlacements);

        // Assert
        error.Should().BeNull();
    }

    /// <summary>TC-52: Reject export without background image</summary>
    [Fact]
    public void ValidateBackgroundImageUrl_WithEmptyUrl_ReturnsError()
    {
        // Arrange
        var backgroundImageUrl = "";

        // Act
        var error = _validator.ValidateBackgroundImageUrl(backgroundImageUrl);

        // Assert
        error.Should().NotBeNullOrEmpty();
        error.Should().Contain("required");
    }

    /// <summary>TC-53: Reject export with no placements (no logos and no text)</summary>
    [Fact]
    public void ValidatePngRequest_NoPlacementsAndNoText_ReturnsError()
    {
        // Arrange
        var placements = new List<ZonePlacement>();
        var textPlacements = new List<TextPlacement>();

        // Act
        var error = _validator.ValidatePngRequest(placements, textPlacements);

        // Assert
        error.Should().NotBeNullOrEmpty();
        error.Should().Contain("At least one");
    }

    /// <summary>TC-54: Accept export with one placement</summary>
    [Fact]
    public void ValidatePngRequest_WithOnePlacement_ReturnsNull()
    {
        // Arrange
        var placements = new List<ZonePlacement>
        {
            new ZonePlacement { ZoneId = "1", LogoId = "logo1", LogoX = 10, LogoY = 10, LogoWidth = 100, LogoHeight = 100 }
        };
        var textPlacements = new List<TextPlacement>();

        // Act
        var error = _validator.ValidatePngRequest(placements, textPlacements);

        // Assert
        error.Should().BeNull();
    }

    /// <summary>TC-55: Accept export with multiple placements</summary>
    [Fact]
    public void ValidatePngRequest_WithMultiplePlacements_ReturnsNull()
    {
        // Arrange
        var placements = new List<ZonePlacement>
        {
            new ZonePlacement { ZoneId = "1", LogoId = "logo1", LogoX = 10, LogoY = 10, LogoWidth = 100, LogoHeight = 100 },
            new ZonePlacement { ZoneId = "2", LogoId = "logo2", LogoX = 150, LogoY = 150, LogoWidth = 50, LogoHeight = 50 }
        };
        var textPlacements = new List<TextPlacement>();

        // Act
        var error = _validator.ValidatePngRequest(placements, textPlacements);

        // Assert
        error.Should().BeNull();
    }

    /// <summary>Accept export with only text (no logos)</summary>
    [Fact]
    public void ValidatePngRequest_WithOnlyTextPlacements_ReturnsNull()
    {
        // Arrange
        var placements = new List<ZonePlacement>();
        var textPlacements = new List<TextPlacement>
        {
            new TextPlacement { ZoneId = "1", Text = "Hello", X = 10, Y = 10, FontSize = 24, Color = "#000000" }
        };

        // Act
        var error = _validator.ValidatePngRequest(placements, textPlacements);

        // Assert
        error.Should().BeNull();
    }

    /// <summary>Accept export with both logos and text</summary>
    [Fact]
    public void ValidatePngRequest_WithLogosAndText_ReturnsNull()
    {
        // Arrange
        var placements = new List<ZonePlacement>
        {
            new ZonePlacement { ZoneId = "1", LogoId = "logo1", LogoX = 10, LogoY = 10, LogoWidth = 100, LogoHeight = 100 }
        };
        var textPlacements = new List<TextPlacement>
        {
            new TextPlacement { ZoneId = "1", Text = "Hello", X = 120, Y = 10, FontSize = 24 }
        };

        // Act
        var error = _validator.ValidatePngRequest(placements, textPlacements);

        // Assert
        error.Should().BeNull();
    }

    /// <summary>Handle null placements list</summary>
    [Fact]
    public void ValidatePngRequest_WithNullPlacements_ReturnsError()
    {
        // Arrange
        List<ZonePlacement>? placements = null;
        var textPlacements = new List<TextPlacement>();

        // Act
        var error = _validator.ValidatePngRequest(placements, textPlacements);

        // Assert
        error.Should().NotBeNullOrEmpty();
        error.Should().Contain("At least one");
    }

    /// <summary>Validate background URL with valid URL</summary>
    [Fact]
    public void ValidateBackgroundImageUrl_WithValidUrl_ReturnsNull()
    {
        // Arrange
        var backgroundImageUrl = "https://example.com/product-image.png";

        // Act
        var error = _validator.ValidateBackgroundImageUrl(backgroundImageUrl);

        // Assert
        error.Should().BeNull();
    }

    /// <summary>Reject null background URL</summary>
    [Fact]
    public void ValidateBackgroundImageUrl_WithNull_ReturnsError()
    {
        // Arrange
        string? backgroundImageUrl = null;

        // Act
        var error = _validator.ValidateBackgroundImageUrl(backgroundImageUrl);

        // Assert
        error.Should().NotBeNullOrEmpty();
        error.Should().Contain("required");
    }

    /// <summary>Reject whitespace-only background URL</summary>
    [Fact]
    public void ValidateBackgroundImageUrl_WithWhitespace_ReturnsError()
    {
        // Arrange
        var backgroundImageUrl = "   ";

        // Act
        var error = _validator.ValidateBackgroundImageUrl(backgroundImageUrl);

        // Assert
        error.Should().NotBeNullOrEmpty();
        error.Should().Contain("required");
    }

    // =====================================================================
    // PDF Export Validation (TC-57 to TC-59)
    // =====================================================================

    /// <summary>TC-57: Accept PDF export with one page</summary>
    [Fact]
    public void ValidatePdfRequest_WithOnePage_ReturnsNull()
    {
        // Arrange
        var pages = new List<ExportPngRequest>
        {
            new ExportPngRequest
            {
                ProductId = "1",
                BackgroundImageUrl = "https://example.com/image.png",
                Placements = new List<ZonePlacement> { new ZonePlacement { ZoneId = "1", LogoId = "logo1" } },
                TextPlacements = new List<TextPlacement>()
            }
        };

        // Act
        var error = _validator.ValidatePdfRequest(pages);

        // Assert
        error.Should().BeNull();
    }

    /// <summary>TC-58: Accept PDF export with two pages</summary>
    [Fact]
    public void ValidatePdfRequest_WithMultiplePages_ReturnsNull()
    {
        // Arrange
        var pages = new List<ExportPngRequest>
        {
            new ExportPngRequest
            {
                ProductId = "1",
                BackgroundImageUrl = "https://example.com/image1.png",
                Placements = new List<ZonePlacement> { new ZonePlacement { ZoneId = "1", LogoId = "logo1" } },
                TextPlacements = new List<TextPlacement>()
            },
            new ExportPngRequest
            {
                ProductId = "1",
                BackgroundImageUrl = "https://example.com/image2.png",
                Placements = new List<ZonePlacement> { new ZonePlacement { ZoneId = "2", LogoId = "logo2" } },
                TextPlacements = new List<TextPlacement>()
            }
        };

        // Act
        var error = _validator.ValidatePdfRequest(pages);

        // Assert
        error.Should().BeNull();
    }

    /// <summary>TC-59: Reject PDF export with empty pages list</summary>
    [Fact]
    public void ValidatePdfRequest_WithEmptyPages_ReturnsError()
    {
        // Arrange
        var pages = new List<ExportPngRequest>();

        // Act
        var error = _validator.ValidatePdfRequest(pages);

        // Assert
        error.Should().NotBeNullOrEmpty();
        error.Should().Contain("At least one");
    }

    /// <summary>Reject null pages list</summary>
    [Fact]
    public void ValidatePdfRequest_WithNullPages_ReturnsError()
    {
        // Arrange
        List<ExportPngRequest>? pages = null;

        // Act
        var error = _validator.ValidatePdfRequest(pages);

        // Assert
        error.Should().NotBeNullOrEmpty();
        error.Should().Contain("At least one");
    }

    /// <summary>Three+ pages validation</summary>
    [Fact]
    public void ValidatePdfRequest_WithThreePages_ReturnsNull()
    {
        // Arrange
        var pages = new List<ExportPngRequest>
        {
            new ExportPngRequest { ProductId = "1", BackgroundImageUrl = "url1", Placements = new List<ZonePlacement> { new() { ZoneId = "1", LogoId = "logo1" } } },
            new ExportPngRequest { ProductId = "1", BackgroundImageUrl = "url2", Placements = new List<ZonePlacement> { new() { ZoneId = "1", LogoId = "logo2" } } },
            new ExportPngRequest { ProductId = "1", BackgroundImageUrl = "url3", Placements = new List<ZonePlacement> { new() { ZoneId = "1", LogoId = "logo3" } } }
        };

        // Act
        var error = _validator.ValidatePdfRequest(pages);

        // Assert
        error.Should().BeNull();
    }
}
