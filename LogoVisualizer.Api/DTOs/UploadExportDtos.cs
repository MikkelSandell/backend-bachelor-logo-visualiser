namespace LogoVisualizer.Api.DTOs;

public record LogoUploadResponse(
    string LogoId,
    string LogoUrl,
    string ContentType,
    long FileSizeBytes);

public class ExportPngRequest
{
    public required int ProductId { get; set; }
    public required int ZoneId { get; set; }

    /// <summary>Logo reference ID returned by POST /api/logos/upload.</summary>
    public required string LogoId { get; set; }

    /// <summary>Logo position (pixels) relative to the top-left of the product image.</summary>
    public int LogoX { get; set; }
    public int LogoY { get; set; }
    public int LogoWidth { get; set; }
    public int LogoHeight { get; set; }
}
