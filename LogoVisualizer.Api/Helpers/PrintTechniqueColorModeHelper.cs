using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace LogoVisualizer.Api.Helpers;

public static class PrintTechniqueColorModeHelper
{
    private static readonly HashSet<string> MonochromeOnlyTechniques =
    [
        "engraving"
    ];

    public static bool ShouldRenderMonochrome(string? selectedTechniqueName)
    {
        if (string.IsNullOrWhiteSpace(selectedTechniqueName))
            return false;

        var normalized = selectedTechniqueName.Trim().ToLowerInvariant();
        return MonochromeOnlyTechniques.Contains(normalized);
    }

    public static void ApplyColorModeForTechnique(Image logoImage, string? selectedTechniqueName)
    {
        if (!ShouldRenderMonochrome(selectedTechniqueName))
            return;

        logoImage.Mutate(ctx => ctx.Grayscale());
    }
}