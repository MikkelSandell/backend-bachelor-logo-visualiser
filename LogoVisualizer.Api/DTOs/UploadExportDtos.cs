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
}

/// <summary>
/// Export request for the viewer app. Supports one or more logos across multiple
/// print zones, all composited onto a single product-side image.
/// </summary>
public class ExportPngRequest
{
    public required string ProductId { get; set; }

    /// <summary>
    /// Full URL of the product background image for the exported side.
    /// The frontend resolves this (handles FRONT/BACK/arm side differences) and sends it directly.
    /// </summary>
    public required string BackgroundImageUrl { get; set; }

    public required List<ZonePlacement> Placements { get; set; }
}
