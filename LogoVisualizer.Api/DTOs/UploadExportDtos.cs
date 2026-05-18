namespace LogoVisualizer.Api.DTOs;

public record LogoUploadResponse(
    string LogoId,
    string LogoUrl,
    string ContentType,
    long FileSizeBytes);

/// <summary>
/// One logo placed in one print zone, part of a multi-zone export request.
/// </summary>
public class ZonePlacement
{
    public required string ZoneId { get; set; }

    /// <summary>Logo reference ID returned by POST /api/logos/upload.</summary>
    public required string LogoId { get; set; }

    /// <summary>Logo position and size in product-image pixels (not canvas-scaled).</summary>
    public int LogoX { get; set; }
    public int LogoY { get; set; }
    public int LogoWidth { get; set; }
    public int LogoHeight { get; set; }

    /// <summary>
    /// Optional selected print technique slug (e.g. "engraving").
    /// If omitted, export keeps existing color behavior for backward compatibility.
    /// </summary>
    public string? SelectedTechniqueName { get; set; }

    /// <summary>
    /// Number of colours the user selected (1 = black silhouette, 2 = greyscale, &gt;2 = posterise).
    /// 0 means no colour-count simulation is applied.
    /// </summary>
    public int ColorCount { get; set; }

    /// <summary>Maximum allowed colours for this zone (from zone.maxColors).</summary>
    public int MaxColors { get; set; }
}

/// <summary>
/// One text item placed in a print zone, part of a multi-element export request.
/// </summary>
public class TextPlacement
{
    public required string ZoneId { get; set; }
    public required string Text { get; set; }

    /// <summary>Position in product-image pixels (top-left of the text baseline).</summary>
    public int X { get; set; }
    public int Y { get; set; }

    /// <summary>Font size in product-image pixels.</summary>
    public int FontSize { get; set; } = 24;

    /// <summary>CSS hex color string, e.g. "#ff0000".</summary>
    public string Color { get; set; } = "#000000";
}

/// <summary>
/// Export request for the viewer app. Supports one or more logos and/or text items
/// across multiple print zones, all composited onto a single product-side image.
/// </summary>
public class ExportPngRequest
{
    public required string ProductId { get; set; }

    /// <summary>
    /// Full URL of the product background image for the exported side.
    /// The frontend resolves this (handles FRONT/BACK/arm side differences) and sends it directly.
    /// </summary>
    public required string BackgroundImageUrl { get; set; }

    /// <summary>Logo placements — may be empty if only text is used.</summary>
    public List<ZonePlacement> Placements { get; set; } = [];

    /// <summary>Text placements — may be empty if only logos are used.</summary>
    public List<TextPlacement> TextPlacements { get; set; } = [];
}

public sealed class MultiPagePdfExportRequest
{
    public List<ExportPngRequest> Pages { get; set; } = [];
}
