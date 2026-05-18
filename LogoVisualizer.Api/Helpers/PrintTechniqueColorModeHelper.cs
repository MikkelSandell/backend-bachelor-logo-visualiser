using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace LogoVisualizer.Api.Helpers;

public static class PrintTechniqueColorModeHelper
{
    public static bool ShouldRenderMonochrome(string? selectedTechniqueName) =>
        string.Equals(selectedTechniqueName?.Trim(), "engraving", StringComparison.OrdinalIgnoreCase);

    public static void ApplyColorModeForTechnique(Image logoImage, string? selectedTechniqueName)
    {
        if (string.IsNullOrWhiteSpace(selectedTechniqueName)) return;

        switch (selectedTechniqueName.Trim().ToLowerInvariant())
        {
            case "engraving":
                logoImage.Mutate(ctx => ctx.Grayscale().Contrast(0.5f));
                break;

            case "screen_print":
                PosterizeManual(logoImage, 4);
                logoImage.Mutate(ctx => ctx.GaussianBlur(3f));
                break;

            case "pad_print":
                PosterizeManual(logoImage, 5);
                logoImage.Mutate(ctx => ctx.GaussianBlur(2f));
                break;

            case "embroidery":
                PosterizeManual(logoImage, 5);
                logoImage.Mutate(ctx => ctx.Contrast(0.5f));
                break;

            case "sublimation":
                logoImage.Mutate(ctx => ctx.Contrast(0.3f));
                break;

            // digital_print: full colour, no effect
        }
    }

    /// <summary>
    /// Quantises each colour channel to <paramref name="levels"/> discrete values.
    /// Mirrors the frontend's canvas posterisation: step = 255 / (levels - 1),
    /// new = round(round(value / step) * step).
    /// </summary>
    internal static void PosterizeManual(Image image, int levels)
    {
        if (image is not Image<Rgba32> rgba) return;
        var safeLevel = Math.Max(2, levels);
        var step = 255.0 / (safeLevel - 1);

        rgba.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                {
                    ref var px = ref row[x];
                    px.R = (byte)Math.Round(Math.Round(px.R / step) * step);
                    px.G = (byte)Math.Round(Math.Round(px.G / step) * step);
                    px.B = (byte)Math.Round(Math.Round(px.B / step) * step);
                }
            }
        });
    }
}
