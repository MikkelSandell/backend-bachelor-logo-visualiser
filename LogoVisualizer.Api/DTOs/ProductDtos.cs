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
    ProductStatus Status,
    List<PrintZoneDto> PrintZones)
{
    public static ProductDetailDto FromEntity(Product p, string baseUrl) =>
        new(
            p.Id,
            p.Title,
            $"{baseUrl}/api/files/{Uri.EscapeDataString(p.ImagePath)}",
            p.ImageWidth,
            p.ImageHeight,
            ComputeStatus(p),
            p.PrintZones.Select(PrintZoneDto.FromEntity).ToList()
        );

    private static ProductStatus ComputeStatus(Product p)
    {
        if (string.IsNullOrWhiteSpace(p.Title) || string.IsNullOrWhiteSpace(p.ImagePath))
            return ProductStatus.MissingMetadata;

        return p.PrintZones.Count == 0 ? ProductStatus.MissingZones : ProductStatus.FullyConfigured;
    }
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

/// <summary>
/// Admin Tool sends this to update product with full zone details.
/// All zones are replaced (add/update/delete handled by id).
/// </summary>
public class UpdateProductDetailRequest
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public int ImageWidth { get; set; }
    public int ImageHeight { get; set; }
    public List<UpdatePrintZoneDetailDto> PrintZones { get; set; } = [];
}

public class UpdatePrintZoneDetailDto
{
    public int Id { get; set; }  // 0 = new zone
    public required string Name { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public decimal MaxPhysicalWidthMm { get; set; }
    public decimal MaxPhysicalHeightMm { get; set; }
    public int? MaxColors { get; set; }
    public List<string> AllowedTechniques { get; set; } = [];  // Never null
}

// ---------------------------------------------------------------------------
// Import / Export (supplier JSON format)
// ---------------------------------------------------------------------------

public class ValidationErrorResponse
{
    public List<string> Errors { get; set; } = [];
}

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
