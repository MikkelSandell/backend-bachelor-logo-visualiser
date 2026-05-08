namespace LogoVisualizer.Api.Helpers;

public readonly record struct LogoRenderRect(int X, int Y, int Width, int Height);

public static class LogoPlacementCalculator
{
    public static LogoRenderRect CalculateFitToZone(
        int sourceWidth,
        int sourceHeight,
        int zoneX,
        int zoneY,
        int zoneWidth,
        int zoneHeight)
    {
        if (zoneWidth <= 0) throw new ArgumentOutOfRangeException(nameof(zoneWidth));
        if (zoneHeight <= 0) throw new ArgumentOutOfRangeException(nameof(zoneHeight));

        var safeSourceWidth = Math.Max(1, sourceWidth);
        var safeSourceHeight = Math.Max(1, sourceHeight);

        var scale = Math.Min(
            zoneWidth / (double)safeSourceWidth,
            zoneHeight / (double)safeSourceHeight);

        var resizedWidth = Math.Max(1, (int)Math.Round(safeSourceWidth * scale, MidpointRounding.AwayFromZero));
        var resizedHeight = Math.Max(1, (int)Math.Round(safeSourceHeight * scale, MidpointRounding.AwayFromZero));

        // Guard against rounding overshoot.
        resizedWidth = Math.Min(zoneWidth, resizedWidth);
        resizedHeight = Math.Min(zoneHeight, resizedHeight);

        var centeredX = zoneX + (zoneWidth - resizedWidth) / 2;
        var centeredY = zoneY + (zoneHeight - resizedHeight) / 2;

        var finalX = Math.Clamp(centeredX, zoneX, zoneX + zoneWidth - resizedWidth);
        var finalY = Math.Clamp(centeredY, zoneY, zoneY + zoneHeight - resizedHeight);

        return new LogoRenderRect(finalX, finalY, resizedWidth, resizedHeight);
    }
}