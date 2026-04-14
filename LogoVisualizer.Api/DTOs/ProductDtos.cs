using LogoVisualizer.Data.Models;

namespace LogoVisualizer.Api.DTOs;

// ---------------------------------------------------------------------------
// Response DTOs
// ---------------------------------------------------------------------------

public enum ProductStatus { FullyConfigured, MissingZones, MissingMetadata }

public record ProductSummaryDto(
    int Id,
    string Title,
    string ImageUrl,
    int ImageWidth,
    int ImageHeight,
    int ZoneCount,
    ProductStatus Status)
{
    public static ProductSummaryDto FromEntity(Product p, string baseUrl) =>
        new(
            p.Id,
            p.Title,
            $"{baseUrl}/api/files/{Uri.EscapeDataString(p.ImagePath)}",
            p.ImageWidth,
            p.ImageHeight,
            p.PrintZones.Count,
            p.PrintZones.Count == 0 ? ProductStatus.MissingZones : ProductStatus.FullyConfigured
        );
}

public record ProductDetailDto(
    int Id,
    string Title,
    string ImageUrl,
    int ImageWidth,
    int ImageHeight,
    List<PrintZoneDto> PrintZones)
{
    public static ProductDetailDto FromEntity(Product p, string baseUrl) =>
        new(
            p.Id,
            p.Title,
            $"{baseUrl}/api/files/{Uri.EscapeDataString(p.ImagePath)}",
            p.ImageWidth,
            p.ImageHeight,
            p.PrintZones.Select(PrintZoneDto.FromEntity).ToList()
        );
}

// ---------------------------------------------------------------------------
// Request DTOs
// ---------------------------------------------------------------------------

public class CreateProductRequest
{
    public required string Title { get; set; }
    public required IFormFile Image { get; set; }
    public required int ImageWidth { get; set; }
    public required int ImageHeight { get; set; }
}

public class UpdateProductRequest
{
    public required string Title { get; set; }
    public int ImageWidth { get; set; }
    public int ImageHeight { get; set; }
}

// ---------------------------------------------------------------------------
// Import / Export (supplier JSON format)
// ---------------------------------------------------------------------------

public class ImportProductDto
{
    public string? ExternalId { get; set; }
    public required string Title { get; set; }
    public string? ImageUrl { get; set; }
    public int ImageWidth { get; set; }
    public int ImageHeight { get; set; }
    public List<ImportPrintZoneDto> PrintZones { get; set; } = [];
}

public class ImportPrintZoneDto
{
    public required string Name { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public decimal MaxPhysicalWidthMm { get; set; }
    public decimal MaxPhysicalHeightMm { get; set; }
    public int? MaxColors { get; set; }
    public List<string> AllowedTechniques { get; set; } = [];
}
