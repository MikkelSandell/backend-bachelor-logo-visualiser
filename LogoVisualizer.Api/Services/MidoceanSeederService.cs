using LogoVisualizer.Api.DTOs;
using LogoVisualizer.Data;
using LogoVisualizer.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LogoVisualizer.Api.Services;

public interface IMidoceanSeederService
{
    Task SeedAsync(CancellationToken ct = default);
}

public class MidoceanSeederService : IMidoceanSeederService
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<MidoceanSeederService> _logger;

    public MidoceanSeederService(AppDbContext db, IWebHostEnvironment env, ILogger<MidoceanSeederService> logger)
    {
        _db = db;
        _env = env;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        // Skip if products already exist
        if (await _db.Products.AnyAsync(ct))
        {
            _logger.LogInformation("Database already seeded, skipping Midocean products import.");
            return;
        }

        try
        {
            var path = Path.Combine(_env.ContentRootPath, "Data", "Midocean-print-data.json");
            using var stream = File.OpenRead(path);
            var file = JsonSerializer.Deserialize<MidoceanDataFile>(stream);

            if (file?.Products == null || file.Products.Count == 0)
            {
                _logger.LogWarning("No products found in Midocean-print-data.json");
                return;
            }

            var productsToAdd = new List<Product>();

            foreach (var midoceanProduct in file.Products)
            {
                var primaryColor = midoceanProduct.ItemColorNumbers.FirstOrDefault() ?? "";

                // Create product
                var product = new Product
                {
                    Title = midoceanProduct.MasterCode,
                    ImagePath = GetImageForColor(midoceanProduct.PrintingPositions.FirstOrDefault()?.Images ?? [], primaryColor)
                                ?? GetFirstImageUrl(midoceanProduct) ?? "",
                    ImageWidth = 1000,  // Midocean images are 1000x1000
                    ImageHeight = 1000,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Add print zones from printing positions
                var zones = new List<PrintZone>();
                foreach (var position in midoceanProduct.PrintingPositions)
                {
                    var zone = new PrintZone
                    {
                        Name = position.PositionId,
                        X = GetPointX(position),
                        Y = GetPointY(position),
                        Width = GetPointWidth(position),
                        Height = GetPointHeight(position),
                        MaxPhysicalWidthMm = (decimal)position.MaxPrintSizeWidth,
                        MaxPhysicalHeightMm = (decimal)position.MaxPrintSizeHeight,
                        MaxColors = GetMaxColors(position),
                        ImageUrl = GetImageForColor(position.Images, primaryColor)
                                   ?? position.Images.FirstOrDefault()?.ImageBlank
                    };

                    // Link techniques
                    var techniques = await GetTechniqueIds(position.PrintingTechniques, ct);
                    foreach (var techniqueId in techniques)
                    {
                        zone.AllowedTechniques.Add(new PrintZoneTechnique
                        {
                            PrintTechniqueId = techniqueId
                        });
                    }

                    zones.Add(zone);
                }

                product.PrintZones = zones;
                productsToAdd.Add(product);
            }

            _db.Products.AddRange(productsToAdd);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation($"Successfully seeded {productsToAdd.Count} Midocean products with zones.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding Midocean products");
            throw;
        }
    }

    private static string? GetFirstImageUrl(MidoceanProductDto product)
    {
        return product.PrintingPositions
            .FirstOrDefault()?
            .Images
            .FirstOrDefault()?
            .ImageBlank;
    }

    private static string? GetImageForColor(List<MidoceanPositionImageDto> images, string colorCode) =>
        string.IsNullOrEmpty(colorCode)
            ? null
            : images.FirstOrDefault(img => img.ImageBlank?.Contains($"-{colorCode}_POS") == true)?.ImageBlank;

    private static int GetPointX(MidoceanPrintingPositionDto position)
    {
        var point = position.Points.OrderBy(p => p.SequenceNo).FirstOrDefault();
        return point?.DistanceFromLeft ?? 0;
    }

    private static int GetPointY(MidoceanPrintingPositionDto position)
    {
        var point = position.Points.OrderBy(p => p.SequenceNo).FirstOrDefault();
        return point?.DistanceFromTop ?? 0;
    }

    private static int GetPointWidth(MidoceanPrintingPositionDto position)
    {
        var points = position.Points.OrderBy(p => p.SequenceNo).ToList();
        if (points.Count < 2) return 0;
        return points[1].DistanceFromLeft - points[0].DistanceFromLeft;
    }

    private static int GetPointHeight(MidoceanPrintingPositionDto position)
    {
        var points = position.Points.OrderBy(p => p.SequenceNo).ToList();
        if (points.Count < 2) return 0;
        return points[1].DistanceFromTop - points[0].DistanceFromTop;
    }

    private static int GetMaxColors(MidoceanPrintingPositionDto position)
    {
        return position.PrintingTechniques
            .Select(t => int.TryParse(t.MaxColours, out var c) ? c : 0)
            .DefaultIfEmpty(0)
            .Max();
    }

    private async Task<List<int>> GetTechniqueIds(List<MidoceanPrintingTechniqueDto> techniques, CancellationToken ct)
    {
        var techniqueNames = techniques
            .Select(t => MapTechnique(t.Id))
            .OfType<string>()
            .Distinct()
            .ToList();

        if (techniqueNames.Count == 0)
            techniqueNames.Add("digital_print"); // Default if none mapped

        var dbTechniques = await _db.PrintTechniques
            .Where(pt => techniqueNames.Contains(pt.Name))
            .ToListAsync(ct);

        return dbTechniques.Select(t => t.Id).ToList();
    }

    /// Maps Midocean technique codes to the frontend's PrintTechnique enum strings.
    private static string? MapTechnique(string code) => code.ToUpperInvariant() switch
    {
        "SP" or "TR" or "ST1" or "ST2" or "SC" or "TS" => "screen_print",
        "E" or "EM" or "EMB" => "embroidery",
        "EN" or "B" or "LA" or "LAS" or "ENG" => "engraving",
        "SL" or "SA" or "SUB" or "DS" => "sublimation",
        "DTG" or "TDT" or "TT" or "DP" or "DIG" or "DC" => "digital_print",
        "TP" or "P" or "PAD" or "PP" => "pad_print",
        _ => null   // unknown codes are silently dropped
    };

    // DTOs matching Midocean JSON structure
    private sealed class MidoceanDataFile
    {
        [JsonPropertyName("products")]
        public List<MidoceanProductDto> Products { get; set; } = [];
    }
}
