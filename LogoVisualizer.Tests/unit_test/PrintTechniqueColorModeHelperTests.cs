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
    public void ApplyColorModeForTechnique_Engraving_PreservesSemiTransparentAlpha()
    {
        using var image = new Image<Rgba32>(1, 1);
        image[0, 0] = new Rgba32(200, 100, 50, 128); // semi-transparent

        PrintTechniqueColorModeHelper.ApplyColorModeForTechnique(image, "engraving");

        var pixel = image[0, 0];
        // Grayscale + Contrast must not change a non-zero alpha to 0 or 255
        pixel.A.Should().Be(128);
        // Must still be grayscale
        pixel.R.Should().Be(pixel.G);
        pixel.G.Should().Be(pixel.B);
    }

    [Fact]
    public void ApplyColorModeForTechnique_PadPrint_AppliesPosterizeEffect()
    {
        // GaussianBlur(2f) requires the image to be larger than the kernel radius.
        // Use 10x10 filled with a uniform colour so blur leaves the centre pixel unchanged.
        using var image = new Image<Rgba32>(10, 10);
        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < 10; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < 10; x++)
                    row[x] = new Rgba32(100, 150, 200, 255);
            }
        });

        PrintTechniqueColorModeHelper.ApplyColorModeForTechnique(image, "pad_print");

        // PosterizeManual(5): step = 255/4 = 63.75
        //   R=100 → round(100/63.75)=2 → round(2×63.75=127.5)=128
        //   G=150 → round(150/63.75)=2 → 128
        //   B=200 → round(200/63.75)=3 → round(3×63.75=191.25)=191
        // Uniform image → GaussianBlur leaves centre pixel unchanged.
        var pixel = image[5, 5];
        pixel.R.Should().Be(128);
        pixel.G.Should().Be(128);
        pixel.B.Should().Be(191);
    }

    // =====================================================================
    // PosterizeManual — direct boundary tests
    // =====================================================================

    [Fact]
    public void PosterizeManual_PureBlack_RemainsUnchanged()
    {
        using var image = new Image<Rgba32>(1, 1);
        image[0, 0] = new Rgba32(0, 0, 0, 255);

        PrintTechniqueColorModeHelper.PosterizeManual(image, 4);

        var pixel = image[0, 0];
        pixel.R.Should().Be(0);
        pixel.G.Should().Be(0);
        pixel.B.Should().Be(0);
    }

    [Fact]
    public void PosterizeManual_PureWhite_RemainsUnchanged()
    {
        using var image = new Image<Rgba32>(1, 1);
        image[0, 0] = new Rgba32(255, 255, 255, 255);

        PrintTechniqueColorModeHelper.PosterizeManual(image, 4);

        var pixel = image[0, 0];
        pixel.R.Should().Be(255);
        pixel.G.Should().Be(255);
        pixel.B.Should().Be(255);
    }

    [Fact]
    public void PosterizeManual_TwoLevels_SnapsToBinaryValues()
    {
        // levels=2 → step=255 → every channel snaps to either 0 or 255.
        using var image = new Image<Rgba32>(1, 1);
        image[0, 0] = new Rgba32(64, 192, 0, 255);
        //   R=64:  round(64/255)=0  → 0
        //   G=192: round(192/255)=1 → 255
        //   B=0:   round(0/255)=0   → 0

        PrintTechniqueColorModeHelper.PosterizeManual(image, 2);

        var pixel = image[0, 0];
        pixel.R.Should().Be(0);
        pixel.G.Should().Be(255);
        pixel.B.Should().Be(0);
    }

    [Fact]
    public void ApplyColorModeForTechnique_ScreenPrint_AppliesPosterizeEffect()
    {
        // GaussianBlur requires the image to be larger than the kernel radius.
        // Use 10x10 filled with a uniform colour so blur leaves the centre pixel unchanged.
        using var image = new Image<Rgba32>(10, 10);
        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < 10; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < 10; x++)
                    row[x] = new Rgba32(100, 150, 200, 255);
            }
        });

        PrintTechniqueColorModeHelper.ApplyColorModeForTechnique(image, "screen_print");

        // PosterizeManual(4): step = 255/3 = 85
        //   R=100 → round(100/85)=1 → 1×85 = 85
        //   G=150 → round(150/85)=2 → 2×85 = 170
        //   B=200 → round(200/85)=2 → 2×85 = 170
        // Uniform image → GaussianBlur leaves centre pixel unchanged.
        var pixel = image[5, 5];
        pixel.R.Should().Be(85);
        pixel.G.Should().Be(170);
        pixel.B.Should().Be(170);
    }
}
