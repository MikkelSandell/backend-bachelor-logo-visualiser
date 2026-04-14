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
        if (product is null) return NotFound();
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

    /// <summary>Updates product metadata (title and image dimensions). Requires admin JWT.</summary>
    [Authorize]
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var product = await _products.GetByIdAsync(id, ct);
        if (product is null) return NotFound();

        product.Title = request.Title;
        product.ImageWidth = request.ImageWidth;
        product.ImageHeight = request.ImageHeight;
        await _products.UpdateAsync(product, ct);
        return NoContent();
    }

    /// <summary>Deletes a product and all its print zones. Requires admin JWT.</summary>
    [Authorize]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var deleted = await _products.DeleteAsync(id, ct);
        if (!deleted) return NotFound();
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
