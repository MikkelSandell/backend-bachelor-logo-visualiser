using LogoVisualizer.Api.DTOs;
using LogoVisualizer.Api.Services;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace LogoVisualizer.Api.Controllers;

/// <summary>
/// Generates a PNG composite of the product image with the customer's logo
/// overlaid inside the selected print zone.
/// Works with both Midocean (viewer) and DB-backed (admin) products.
/// Rate-limited — no authentication required.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ExportController : ControllerBase
{
    private readonly IMidoceanProductService _midocean;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ExportController> _logger;

    public ExportController(IMidoceanProductService midocean, IWebHostEnvironment env, ILogger<ExportController> logger)
    {
        _midocean = midocean;
        _env = env;
        _logger = logger;
    }

    /// <summary>
    /// Composites the uploaded logo onto the product image within the specified print zone
    /// and returns a PNG file for download.
    /// Supports Midocean products (string IDs) for the Viewer app.
    /// </summary>
    [HttpPost("png")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileResult))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GeneratePng([FromBody] ExportPngRequestMidocean request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        // Fetch the adapted product from Midocean
        var product = _midocean.GetAdaptedByMasterCode(request.ProductId);
        if (product is null)
            return NotFound(new { error = $"Product '{request.ProductId}' not found." });

        // Find the requested print zone
        var zone = product.PrintZones.FirstOrDefault(z => z.Id == request.ZoneId);
        if (zone is null)
            return NotFound(new { error = $"Print zone '{request.ZoneId}' not found for product '{request.ProductId}'." });

        // Resolve logo file from the uploaded logos directory
        var logoDir = Path.Combine(_env.ContentRootPath, "uploads", "logos");

        // Ensure the uploads directory exists
        if (!Directory.Exists(logoDir))
        {
            Directory.CreateDirectory(logoDir);
            return BadRequest(new { error = "No logos have been uploaded yet. Please upload a logo first." });
        }

        var logoFiles = Directory.GetFiles(logoDir, $"{request.LogoId}.*");
        if (logoFiles.Length == 0)
            return BadRequest(new { error = $"Logo '{request.LogoId}' not found. Re-upload the logo and try again." });

        var logoPath = logoFiles[0];

        // Load product image from URL (Midocean CDN)
        // For now, we'll return an error since we can't easily download CDN images in real-time
        // In production, cache the images locally or use a different approach
        if (string.IsNullOrWhiteSpace(product.ImageUrl))
            return Problem("Product image URL is not available.");

        _logger.LogInformation("Compositing logo {LogoId} onto product {ProductId} zone {ZoneId}", 
            request.LogoId, request.ProductId, request.ZoneId);

        // Validate that the requested logo placement stays within the print zone bounds
        if (request.LogoX < zone.X || request.LogoY < zone.Y
            || request.LogoX + request.LogoWidth > zone.X + zone.Width
            || request.LogoY + request.LogoHeight > zone.Y + zone.Height)
        {
            return BadRequest(new { error = "Logo placement exceeds the print zone boundaries." });
        }

        try
        {
            // Load the logo image from disk
            using var logoImage = await Image.LoadAsync(logoPath, ct);

            // Load the product image from the CDN URL
            using var httpClient = new HttpClient();
            using var response = await httpClient.GetAsync(product.ImageUrl, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to download product image from {ImageUrl}", product.ImageUrl);
                return Problem($"Failed to download product image. Status: {response.StatusCode}");
            }

            // Load the product image from the downloaded stream
            using var productImageStream = await response.Content.ReadAsStreamAsync(ct);
            using var productImage = await Image.LoadAsync(productImageStream, ct);

            // Validate image dimensions match expectations
            if (productImage.Width != product.ImageWidth || productImage.Height != product.ImageHeight)
            {
                _logger.LogWarning(
                    "Product image dimensions ({ActualWidth}x{ActualHeight}) differ from expected ({ExpectedWidth}x{ExpectedHeight})",
                    productImage.Width, productImage.Height, product.ImageWidth, product.ImageHeight);
                // Continue anyway - dimensions might be slightly different
            }

            // Resize logo to requested dimensions
            logoImage.Mutate(ctx => ctx.Resize(request.LogoWidth, request.LogoHeight));

            // Draw logo onto product image at the specified position
            productImage.Mutate(ctx => ctx.DrawImage(logoImage, new Point(request.LogoX, request.LogoY), 1f));

            // Encode to PNG and return
            var outputStream = new MemoryStream();
            await productImage.SaveAsPngAsync(outputStream, ct);
            outputStream.Position = 0;

            _logger.LogInformation("Successfully generated composite image for product {ProductId}", request.ProductId);

            return File(outputStream, "image/png", $"mockup-{request.ProductId}.png");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error compositing logo for product {ProductId}", request.ProductId);
            return Problem("An error occurred while generating the mockup. Please try again.");
        }
    }
}
