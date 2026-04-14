using LogoVisualizer.Api.DTOs;
using LogoVisualizer.Data.Repositories;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace LogoVisualizer.Api.Controllers;

/// <summary>
/// Generates a PNG composite of the product image with the customer's logo
/// overlaid inside the selected print zone.
/// Rate-limited — no authentication required.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ExportController : ControllerBase
{
    private readonly IProductRepository _products;
    private readonly IPrintZoneRepository _zones;
    private readonly IWebHostEnvironment _env;

    public ExportController(IProductRepository products, IPrintZoneRepository zones, IWebHostEnvironment env)
    {
        _products = products;
        _zones = zones;
        _env = env;
    }

    /// <summary>
    /// Composites the uploaded logo onto the product image within the specified print zone
    /// and returns a PNG file for download.
    /// </summary>
    [HttpPost("png")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileResult))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GeneratePng([FromBody] ExportPngRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var product = await _products.GetByIdWithZonesAsync(request.ProductId, ct);
        if (product is null) return NotFound(new { error = "Product not found." });

        var zone = await _zones.GetByIdAsync(request.ZoneId, ct);
        if (zone is null || zone.ProductId != request.ProductId)
            return NotFound(new { error = "Print zone not found for this product." });

        // Resolve logo file from the uploaded logos directory
        var logoDir = Path.Combine(_env.ContentRootPath, "uploads", "logos");
        var logoFiles = Directory.GetFiles(logoDir, $"{request.LogoId}.*");
        if (logoFiles.Length == 0)
            return BadRequest(new { error = "Logo not found. Re-upload the logo and try again." });

        var logoPath = logoFiles[0];
        var productImagePath = Path.Combine(_env.ContentRootPath, product.ImagePath.Replace('/', Path.DirectorySeparatorChar));

        if (!System.IO.File.Exists(productImagePath))
            return Problem("Product image file is missing on the server.");

        // Validate that the requested logo placement stays within the print zone bounds
        if (request.LogoX < zone.X || request.LogoY < zone.Y
            || request.LogoX + request.LogoWidth > zone.X + zone.Width
            || request.LogoY + request.LogoHeight > zone.Y + zone.Height)
        {
            return BadRequest(new { error = "Logo placement exceeds the print zone boundaries." });
        }

        // Composite the images using ImageSharp
        using var productImage = await Image.LoadAsync(productImagePath, ct);
        using var logoImage = await Image.LoadAsync(logoPath, ct);

        logoImage.Mutate(ctx => ctx.Resize(request.LogoWidth, request.LogoHeight));
        productImage.Mutate(ctx => ctx.DrawImage(logoImage, new Point(request.LogoX, request.LogoY), 1f));

        var outputStream = new MemoryStream();
        await productImage.SaveAsPngAsync(outputStream, ct);
        outputStream.Position = 0;

        return File(outputStream, "image/png", $"mockup-product-{product.Id}.png");
    }
}
