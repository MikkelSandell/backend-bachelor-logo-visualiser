using LogoVisualizer.Api.DTOs;
using LogoVisualizer.Data.Models;
using LogoVisualizer.Data.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace LogoVisualizer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductRepository _products;
    private readonly IWebHostEnvironment _env;

    public ProductsController(IProductRepository products, IWebHostEnvironment env)
    {
        _products = products;
        _env = env;
    }

    /// <summary>Lists all products with their setup status. Public — used by both Admin and Viewer.</summary>
    [HttpGet]
    [ProducesResponseType<List<ProductSummaryDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var products = await _products.GetAllAsync(ct);
        return Ok(products.Select(p => ProductSummaryDto.FromEntity(p, baseUrl)));
    }

    /// <summary>Returns a single product with all print zones and allowed techniques. Public.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<ProductDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var product = await _products.GetByIdWithZonesAsync(id, ct);
        if (product is null) return NotFound(new { error = "Product not found." });
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        return Ok(ProductDetailDto.FromEntity(product, baseUrl));
    }

    /// <summary>Creates a new product with an uploaded product image. Requires admin JWT.</summary>
    [Authorize]
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10_485_760)] // 10 MB
    [ProducesResponseType<ProductDetailDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromForm] CreateProductRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var imagePath = await SaveProductImageAsync(request.Image, ct);
        if (imagePath is null)
            return BadRequest(new { error = "Invalid image file. Allowed types: PNG, JPG. Max size: 10 MB." });

        var product = new Product
        {
            Title = request.Title,
            ImagePath = imagePath,
            ImageWidth = request.ImageWidth,
            ImageHeight = request.ImageHeight,
        };

        var created = await _products.CreateAsync(product, ct);
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, ProductDetailDto.FromEntity(created, baseUrl));
    }

    /// <summary>
    /// Updates product and ALL its print zones. PRIMARY endpoint for Admin Tool.
    /// Sends full product object → backend replaces all zones.
    /// Validates zones before saving.
    /// Returns updated product with all zones.
    /// Requires admin JWT.
    /// </summary>
    [Authorize]
    [HttpPut("{id:int}")]
    [ProducesResponseType<ProductDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateFull(int id, [FromBody] UpdateProductDetailRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        // Load existing product
        var product = await _products.GetByIdWithZonesAsync(id, ct);
        if (product is null) return NotFound(new { error = "Product not found." });

        // Defensive: ensure request has zones list (never null)
        var incomingZones = request.PrintZones ?? [];

        // Defensive: ensure product has zones list (never null)
        if (product.PrintZones == null) 
            product.PrintZones = [];

        // Validate all zones BEFORE saving
        var validationErrors = ValidateZones(incomingZones, request.ImageWidth, request.ImageHeight);
        if (validationErrors.Count > 0)
        {
            return BadRequest(new ValidationErrorResponse { Errors = validationErrors });
        }

        // Update product metadata
        product.Title = request.Title;
        product.ImageWidth = request.ImageWidth;
        product.ImageHeight = request.ImageHeight;

        // IMPORTANT: Replace ALL zones with incoming zones
        // Strategy: Delete zones not in request, update existing by ID, create new ones (id=0)

        // Collect all incoming zone IDs (exclude new zones with id=0)
        var incomingZoneIds = incomingZones.Where(z => z.Id > 0).Select(z => z.Id).ToHashSet();

        // STEP 1: Delete zones not in incoming request
        var zonesToDelete = product.PrintZones.Where(z => !incomingZoneIds.Contains(z.Id)).ToList();
        foreach (var zone in zonesToDelete)
        {
            product.PrintZones.Remove(zone);
        }

        // STEP 2: Update or create zones
        foreach (var incomingZone in incomingZones)
        {
            if (incomingZone.Id > 0)
            {
                // Find and update existing zone
                var existingZone = product.PrintZones.FirstOrDefault(z => z.Id == incomingZone.Id);
                if (existingZone != null)
                {
                    // Update all fields
                    existingZone.Name = incomingZone.Name ?? existingZone.Name;  // Preserve if null
                    existingZone.X = incomingZone.X;
                    existingZone.Y = incomingZone.Y;
                    existingZone.Width = incomingZone.Width;
                    existingZone.Height = incomingZone.Height;
                    existingZone.MaxPhysicalWidthMm = incomingZone.MaxPhysicalWidthMm;
                    existingZone.MaxPhysicalHeightMm = incomingZone.MaxPhysicalHeightMm;
                    existingZone.MaxColors = incomingZone.MaxColors;
                    // Note: AllowedTechniques are separate lookup entities - not synced here
                }
            }
            else
            {
                // Create new zone (id=0 means new)
                var newZone = new PrintZone
                {
                    Name = incomingZone.Name ?? "Unnamed Zone",  // Fallback name
                    X = incomingZone.X,
                    Y = incomingZone.Y,
                    Width = incomingZone.Width,
                    Height = incomingZone.Height,
                    MaxPhysicalWidthMm = incomingZone.MaxPhysicalWidthMm,
                    MaxPhysicalHeightMm = incomingZone.MaxPhysicalHeightMm,
                    MaxColors = incomingZone.MaxColors,
                    AllowedTechniques = []  // Initialize empty list
                };
                product.PrintZones.Add(newZone);
            }
        }

        // STEP 3: Save updated product
        await _products.UpdateAsync(product, ct);

        // STEP 4: Reload and return full updated product
        var updated = await _products.GetByIdWithZonesAsync(id, ct);
        if (updated == null)
            return Problem("Failed to reload product after update.");

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        return Ok(ProductDetailDto.FromEntity(updated, baseUrl));
    }

    /// <summary>
    /// Validate all zones before saving.
    /// Returns aggregated list of ALL errors found.
    /// </summary>
    private static List<string> ValidateZones(List<UpdatePrintZoneDetailDto> zones, int imageWidth, int imageHeight)
    {
        var errors = new List<string>();

        // Defensive: null-safe
        if (zones == null || zones.Count == 0)
            return errors;  // Empty zones is valid

        // Check for duplicate incoming zone IDs
        var incomingIds = zones.Where(z => z.Id > 0).Select(z => z.Id).ToList();
        var duplicates = incomingIds.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key);
        foreach (var dupId in duplicates)
        {
            errors.Add($"Duplicate zone ID {dupId} in request. Each zone must have unique ID.");
        }

        // Validate each zone
        for (int i = 0; i < zones.Count; i++)
        {
            var zone = zones[i];
            var zoneName = string.IsNullOrWhiteSpace(zone.Name) ? $"Zone #{i}" : zone.Name;

            // Zone must have a name
            if (string.IsNullOrWhiteSpace(zone.Name))
                errors.Add($"Zone #{i}: name cannot be empty.");

            // Zone must have positive dimensions
            if (zone.Width <= 0 || zone.Height <= 0)
                errors.Add($"Zone '{zoneName}': must have positive width and height (width: {zone.Width}, height: {zone.Height}).");

            // Zone coordinates must be non-negative
            if (zone.X < 0 || zone.Y < 0)
                errors.Add($"Zone '{zoneName}': coordinates must be non-negative (x: {zone.X}, y: {zone.Y}).");

            // Zone must be within image bounds
            if (zone.X + zone.Width > imageWidth || zone.Y + zone.Height > imageHeight)
                errors.Add($"Zone '{zoneName}': must be within image bounds (zone: x:{zone.X} y:{zone.Y} w:{zone.Width} h:{zone.Height}, image: {imageWidth}x{imageHeight}).");
        }

        return errors;
    }

    /// <summary>Deletes a product and all its print zones. Requires admin JWT.</summary>
    [Authorize]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var deleted = await _products.DeleteAsync(id, ct);
        if (!deleted) return NotFound(new { error = "Product not found." });
        return NoContent();
    }

    /// <summary>
    /// Imports one or more products from a supplier-format JSON file. Requires admin JWT.
    /// The JSON must be an array of ImportProductDto objects.
    /// </summary>
    [Authorize]
    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5_242_880)] // 5 MB
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Import(IFormFile file, CancellationToken ct)
    {
        if (!file.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "Only .json files are accepted." });

        ImportProductDto[]? imports;
        try
        {
            await using var stream = file.OpenReadStream();
            imports = await JsonSerializer.DeserializeAsync<ImportProductDto[]>(
                stream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                ct);
        }
        catch (JsonException ex)
        {
            return BadRequest(new { error = "Invalid JSON format.", detail = ex.Message });
        }

        if (imports is null || imports.Length == 0)
            return BadRequest(new { error = "No products found in the uploaded file." });

        var created = new List<int>();
        foreach (var dto in imports)
        {
            var product = new Product
            {
                Title = dto.Title,
                ImagePath = dto.ImageUrl ?? string.Empty,
                ImageWidth = dto.ImageWidth,
                ImageHeight = dto.ImageHeight,
            };
            var saved = await _products.CreateAsync(product, ct);
            created.Add(saved.Id);
        }

        return Ok(new { imported = created.Count, productIds = created });
    }

    /// <summary>Exports a single product as a JSON file (supplier-compatible format). Requires admin JWT.</summary>
    [Authorize]
    [HttpGet("{id:int}/export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Export(int id, CancellationToken ct)
    {
        var product = await _products.GetByIdWithZonesAsync(id, ct);
        if (product is null) return NotFound();

        var dto = new ImportProductDto
        {
            Title = product.Title,
            ImageUrl = product.ImagePath,
            ImageWidth = product.ImageWidth,
            ImageHeight = product.ImageHeight,
            PrintZones = product.PrintZones.Select(z => new ImportPrintZoneDto
            {
                Name = z.Name,
                X = z.X,
                Y = z.Y,
                Width = z.Width,
                Height = z.Height,
                MaxPhysicalWidthMm = z.MaxPhysicalWidthMm,
                MaxPhysicalHeightMm = z.MaxPhysicalHeightMm,
                MaxColors = z.MaxColors,
                AllowedTechniques = z.AllowedTechniques.Select(pzt => pzt.PrintTechnique.Name).ToList()
            }).ToList()
        };

        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
        return File(System.Text.Encoding.UTF8.GetBytes(json), "application/json", $"product-{id}.json");
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    private static readonly HashSet<string> AllowedImageContentTypes =
        ["image/png", "image/jpeg"];

    private async Task<string?> SaveProductImageAsync(IFormFile file, CancellationToken ct)
    {
        if (!AllowedImageContentTypes.Contains(file.ContentType.ToLowerInvariant()))
            return null;

        if (file.Length > 10_485_760)
            return null;

        var directory = Path.Combine(_env.ContentRootPath, "uploads", "products");
        Directory.CreateDirectory(directory);

        var extension = file.ContentType == "image/png" ? ".png" : ".jpg";
        var fileName = $"{Guid.NewGuid()}{extension}";
        var fullPath = Path.Combine(directory, fileName);

        await using var destination = System.IO.File.Create(fullPath);
        await file.CopyToAsync(destination, ct);

        return $"uploads/products/{fileName}";
    }
}
