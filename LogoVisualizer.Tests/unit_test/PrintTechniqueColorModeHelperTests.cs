using FluentAssertions;
using LogoVisualizer.Api.Helpers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace LogoVisualizer.Tests;

public class PrintTechniqueColorModeHelperTests
{
    [Fact]
    public void ShouldRenderMonochrome_WithNullOrUnknownTechnique_ReturnsFalse()
    {
        PrintTechniqueColorModeHelper.ShouldRenderMonochrome(null).Should().BeFalse();
        PrintTechniqueColorModeHelper.ShouldRenderMonochrome("").Should().BeFalse();
        PrintTechniqueColorModeHelper.ShouldRenderMonochrome("screen_print").Should().BeFalse();
    }

    [Fact]
    public void ShouldRenderMonochrome_WithEngraving_ReturnsTrue()
    {
        PrintTechniqueColorModeHelper.ShouldRenderMonochrome("engraving").Should().BeTrue();
        PrintTechniqueColorModeHelper.ShouldRenderMonochrome(" Engraving ").Should().BeTrue();
    }

    [Fact]
    public void ApplyColorModeForTechnique_MissingSelectedTechnique_KeepsColorUnchanged()
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
    public void ApplyColorModeForTechnique_Engraving_ConvertsToMonochrome()
    {
        using var image = new Image<Rgba32>(1, 1);
        image[0, 0] = new Rgba32(180, 30, 90, 255);

        PrintTechniqueColorModeHelper.ApplyColorModeForTechnique(image, "engraving");

        var pixel = image[0, 0];
        pixel.R.Should().Be(pixel.G);
        pixel.G.Should().Be(pixel.B);
        pixel.A.Should().Be(255);
    }
}
