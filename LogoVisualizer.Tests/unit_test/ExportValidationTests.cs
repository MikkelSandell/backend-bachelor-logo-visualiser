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
        var placements = new List<ZonePlacement>
        {
            new() { ZoneId = "1", LogoId = "logo1", LogoX = 10, LogoY = 10, LogoWidth = 100, LogoHeight = 100 }
        };

        var error = _validator.ValidatePngRequest(placements, []);

        error.Should().BeNull();
    }

    /// <summary>TC-52: Reject export without background image</summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ValidateBackgroundImageUrl_WithMissingUrl_ReturnsError(string? backgroundImageUrl)
    {
        var error = _validator.ValidateBackgroundImageUrl(backgroundImageUrl);

        error.Should().NotBeNullOrEmpty();
        error.Should().Contain("required");
    }

    /// <summary>TC-53: Reject export with no placements (no logos and no text)</summary>
    [Fact]
    public void ValidatePngRequest_NoPlacementsAndNoText_ReturnsError()
    {
        var error = _validator.ValidatePngRequest([], []);

        error.Should().NotBeNullOrEmpty();
        error.Should().Contain("At least one");
    }

    /// <summary>TC-55: Accept export with multiple logo placements</summary>
    [Fact]
    public void ValidatePngRequest_WithMultiplePlacements_ReturnsNull()
    {
        var placements = new List<ZonePlacement>
        {
            new() { ZoneId = "1", LogoId = "logo1", LogoX = 10, LogoY = 10, LogoWidth = 100, LogoHeight = 100 },
            new() { ZoneId = "2", LogoId = "logo2", LogoX = 150, LogoY = 150, LogoWidth = 50, LogoHeight = 50 }
        };

        var error = _validator.ValidatePngRequest(placements, []);

        error.Should().BeNull();
    }

    /// <summary>Accept export with only text (no logos)</summary>
    [Fact]
    public void ValidatePngRequest_WithOnlyTextPlacements_ReturnsNull()
    {
        var textPlacements = new List<TextPlacement>
        {
            new() { ZoneId = "1", Text = "Hello", X = 10, Y = 10, FontSize = 24, Color = "#000000" }
        };

        var error = _validator.ValidatePngRequest([], textPlacements);

        error.Should().BeNull();
    }

    /// <summary>Accept export with both logos and text</summary>
    [Fact]
    public void ValidatePngRequest_WithLogosAndText_ReturnsNull()
    {
        var placements = new List<ZonePlacement>
        {
            new() { ZoneId = "1", LogoId = "logo1", LogoX = 10, LogoY = 10, LogoWidth = 100, LogoHeight = 100 }
        };
        var textPlacements = new List<TextPlacement>
        {
            new() { ZoneId = "1", Text = "Hello", X = 120, Y = 10, FontSize = 24 }
        };

        var error = _validator.ValidatePngRequest(placements, textPlacements);

        error.Should().BeNull();
    }

    /// <summary>Handle null placements list</summary>
    [Fact]
    public void ValidatePngRequest_WithNullPlacements_ReturnsError()
    {
        var error = _validator.ValidatePngRequest(null, []);

        error.Should().NotBeNullOrEmpty();
        error.Should().Contain("At least one");
    }

    /// <summary>Accept valid background URL</summary>
    [Fact]
    public void ValidateBackgroundImageUrl_WithValidUrl_ReturnsNull()
    {
        var error = _validator.ValidateBackgroundImageUrl("https://example.com/product-image.png");

        error.Should().BeNull();
    }

    // =====================================================================
    // PDF Export Validation (TC-57 to TC-59)
    // =====================================================================

    /// <summary>TC-57: Accept PDF export with one page</summary>
    [Fact]
    public void ValidatePdfRequest_WithOnePage_ReturnsNull()
    {
        var pages = new List<ExportPngRequest>
        {
            new()
            {
                ProductId = "1",
                BackgroundImageUrl = "https://example.com/image.png",
                Placements = [new() { ZoneId = "1", LogoId = "logo1" }],
                TextPlacements = []
            }
        };

        var error = _validator.ValidatePdfRequest(pages);

        error.Should().BeNull();
    }

    /// <summary>TC-58: Accept PDF export with two pages</summary>
    [Fact]
    public void ValidatePdfRequest_WithMultiplePages_ReturnsNull()
    {
        var pages = new List<ExportPngRequest>
        {
            new() { ProductId = "1", BackgroundImageUrl = "url1", Placements = [new() { ZoneId = "1", LogoId = "logo1" }], TextPlacements = [] },
            new() { ProductId = "1", BackgroundImageUrl = "url2", Placements = [new() { ZoneId = "2", LogoId = "logo2" }], TextPlacements = [] }
        };

        var error = _validator.ValidatePdfRequest(pages);

        error.Should().BeNull();
    }

    /// <summary>TC-59: Reject PDF export with empty or null pages list</summary>
    [Fact]
    public void ValidatePdfRequest_WithEmptyPages_ReturnsError()
    {
        var error = _validator.ValidatePdfRequest([]);

        error.Should().NotBeNullOrEmpty();
        error.Should().Contain("At least one");
    }

    [Fact]
    public void ValidatePdfRequest_WithNullPages_ReturnsError()
    {
        var error = _validator.ValidatePdfRequest(null);

        error.Should().NotBeNullOrEmpty();
        error.Should().Contain("At least one");
    }
}
