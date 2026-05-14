using FluentAssertions;
using LogoVisualizer.Api.Helpers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace LogoVisualizer.Tests;

public class PrintTechniqueColorModeHelperTests
{
    // =====================================================================
    // ShouldRenderMonochrome
    // =====================================================================

    /// <summary>Only "engraving" should require monochrome rendering.</summary>
    [Theory]
    [InlineData("engraving",  true)]
    [InlineData("Engraving",  true)]   // case-insensitive
    [InlineData(" engraving ", true)]  // leading/trailing whitespace trimmed
    public void ShouldRenderMonochrome_EngravingVariants_ReturnsTrue(string technique, bool expected)
    {
        PrintTechniqueColorModeHelper.ShouldRenderMonochrome(technique).Should().Be(expected);
    }

    /// <summary>All non-engraving techniques and empty inputs should keep colour.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("screen_print")]
    [InlineData("embroidery")]
    [InlineData("sublimation")]
    [InlineData("digital_print")]
    [InlineData("pad_print")]
    [InlineData("unknown_technique")]
    public void ShouldRenderMonochrome_NonEngravingOrEmpty_ReturnsFalse(string? technique)
    {
        PrintTechniqueColorModeHelper.ShouldRenderMonochrome(technique).Should().BeFalse();
    }

    // =====================================================================
    // ApplyColorModeForTechnique
    // =====================================================================

    [Fact]
    public void ApplyColorModeForTechnique_NullTechnique_LeavesPixelUnchanged()
    {
        using var image = new Image<Rgba32>(1, 1);
        image[0, 0] = new Rgba32(200, 40, 10, 255);

        PrintTechniqueColorModeHelper.ApplyColorModeForTechnique(image, null);

        var pixel = image[0, 0];
        pixel.R.Should().Be(200);
        pixel.G.Should().Be(40);
        pixel.B.Should().Be(10);
        pixel.A.Should().Be(255);
    }

    [Fact]
    public void ApplyColorModeForTechnique_Engraving_ConvertsToGrayscale()
    {
        using var image = new Image<Rgba32>(1, 1);
        image[0, 0] = new Rgba32(180, 30, 90, 255);

        PrintTechniqueColorModeHelper.ApplyColorModeForTechnique(image, "engraving");

        var pixel = image[0, 0];
        pixel.R.Should().Be(pixel.G);
        pixel.G.Should().Be(pixel.B);
        pixel.A.Should().Be(255);
    }

    [Fact]
    public void ApplyColorModeForTechnique_Engraving_PreservesTransparency()
    {
        using var image = new Image<Rgba32>(1, 1);
        image[0, 0] = new Rgba32(200, 100, 50, 0); // fully transparent pixel

        PrintTechniqueColorModeHelper.ApplyColorModeForTechnique(image, "engraving");

        // Alpha must stay 0 — transparent pixels should not become opaque
        image[0, 0].A.Should().Be(0);
    }

    [Fact]
    public void ApplyColorModeForTechnique_ScreenPrint_LeavesPixelUnchanged()
    {
        using var image = new Image<Rgba32>(1, 1);
        image[0, 0] = new Rgba32(100, 150, 200, 255);

        PrintTechniqueColorModeHelper.ApplyColorModeForTechnique(image, "screen_print");

        var pixel = image[0, 0];
        pixel.R.Should().Be(100);
        pixel.G.Should().Be(150);
        pixel.B.Should().Be(200);
    }
}
