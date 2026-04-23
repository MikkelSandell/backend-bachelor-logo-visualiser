using LogoVisualizer.Api.DTOs;
using LogoVisualizer.Data;
using LogoVisualizer.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace LogoVisualizer.Api.Services;

public interface IProductDataService
{
    Task<IReadOnlyList<AdaptedProductDto>> GetAllAdaptedAsync(CancellationToken ct = default);
    Task<AdaptedProductDto?> GetAdaptedByIdAsync(string id, CancellationToken ct = default);
}

public class ProductDataService : IProductDataService
{
    private readonly AppDbContext _db;
    private readonly IMidoceanProductService _jsonFallback;
    private readonly ILogger<ProductDataService> _logger;

    public ProductDataService(AppDbContext db, IMidoceanProductService jsonFallback, ILogger<ProductDataService> logger)
    {
        _db = db;
        _jsonFallback = jsonFallback;
        _logger = logger;
    }

    public async Task<IReadOnlyList<AdaptedProductDto>> GetAllAdaptedAsync(CancellationToken ct = default)
    {
        try
        {
            var products = await _db.Products
                .Include(p => p.PrintZones)
                    .ThenInclude(z => z.AllowedTechniques)
                        .ThenInclude(t => t.PrintTechnique)
                .AsNoTracking()
                .ToListAsync(ct);

            if (products.Count > 0)
                return products.Select(MapToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Database unavailable, falling back to JSON data.");
        }

        _logger.LogInformation("No products in database — using JSON fallback.");
        return _jsonFallback.GetAllAdapted();
    }

    public async Task<AdaptedProductDto?> GetAdaptedByIdAsync(string id, CancellationToken ct = default)
    {
        if (int.TryParse(id, out var dbId))
        {
            try
            {
                var product = await _db.Products
                    .Include(p => p.PrintZones)
                        .ThenInclude(z => z.AllowedTechniques)
                            .ThenInclude(t => t.PrintTechnique)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == dbId, ct);

                if (product is not null)
                    return MapToDto(product);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Database unavailable, falling back to JSON data.");
            }
        }

        // Fall back to JSON — treat id as a Midocean master_code
        return _jsonFallback.GetAdaptedByMasterCode(id);
    }

    private static AdaptedProductDto MapToDto(Product product) => new(
        product.Id.ToString(),
        product.Title,
        product.ImagePath,
        product.ImageWidth,
        product.ImageHeight,
        product.PrintZones.Select(z => new AdaptedPrintZoneDto(
            z.Id.ToString(),
            z.Name,
            z.X, z.Y, z.Width, z.Height,
            (double)z.MaxPhysicalWidthMm,
            (double)z.MaxPhysicalHeightMm,
            z.AllowedTechniques.Select(t => t.PrintTechnique.Name).ToList(),
            z.MaxColors ?? 0,
            product.ImagePath
        )).ToList()
    );
}
