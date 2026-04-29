using LogoVisualizer.Api.DTOs;
using LogoVisualizer.Api.Services;
using Microsoft.AspNetCore.Mvc;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
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
    private readonly IProductDataService _productData;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ExportController> _logger;

    public ExportController(IProductDataService productData, IWebHostEnvironment env, ILogger<ExportController> logger)
    {
        _productData = productData;
        _env = env;
        _logger = logger;
    }

    /// <summary>
    /// Composites one or more logos onto the product background image and returns a PNG.
    /// All placements must be on the same product side (the caller is responsible for this).
    /// </summary>
    [HttpPost("png")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileResult))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GeneratePng([FromBody] ExportPngRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        if ((request.Placements is null || request.Placements.Count == 0)
            && (request.TextPlacements is null || request.TextPlacements.Count == 0))
            return BadRequest(new { error = "At least one logo or text placement is required." });

        if (string.IsNullOrWhiteSpace(request.BackgroundImageUrl))
            return BadRequest(new { error = "BackgroundImageUrl is required." });

        // Fetch the adapted product — supports numeric DB IDs and Midocean master codes
        var product = await _productData.GetAdaptedByIdAsync(request.ProductId, ct);
        if (product is null)
            return NotFound(new { error = $"Product '{request.ProductId}' not found." });

        var logoDir = Path.Combine(_env.ContentRootPath, "uploads", "logos");

        // Resolve and validate every logo placement before touching the image
        var resolved = new List<(string logoPath, ZonePlacement p)>();
        foreach (var placement in request.Placements ?? [])
        {
            var zone = product.PrintZones.FirstOrDefault(z => z.Id == placement.ZoneId);
            if (zone is null)
                return NotFound(new { error = $"Zone '{placement.ZoneId}' not found." });

            if (!Directory.Exists(logoDir))
                return BadRequest(new { error = "No logos have been uploaded yet." });

            var logoFiles = Directory.GetFiles(logoDir, $"{placement.LogoId}.*");
            if (logoFiles.Length == 0)
                return BadRequest(new { error = $"Logo '{placement.LogoId}' not found. Re-upload it and try again." });

            resolved.Add((logoFiles[0], placement));
        }

        _logger.LogInformation(
            "Compositing {Count} logo(s) onto product {ProductId}",
            resolved.Count, request.ProductId);

        try
        {
            // Download the background product image
            using var httpClient = new HttpClient();
            using var bgResponse = await httpClient.GetAsync(request.BackgroundImageUrl, ct);
            if (!bgResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to download background image from {Url}", request.BackgroundImageUrl);
                return Problem($"Failed to download product image. Status: {bgResponse.StatusCode}");
            }

            using var bgStream = await bgResponse.Content.ReadAsStreamAsync(ct);
            using var productImage = await Image.LoadAsync(bgStream, ct);

            // Composite each logo in order
            foreach (var (logoPath, placement) in resolved)
            {
                using var logoImage = await Image.LoadAsync(logoPath, ct);
                logoImage.Mutate(ctx => ctx.Resize(placement.LogoWidth, placement.LogoHeight));
                productImage.Mutate(ctx =>
                    ctx.DrawImage(logoImage, new Point(placement.LogoX, placement.LogoY), 1f));
            }

            // Render text placements
            if (request.TextPlacements is { Count: > 0 })
            {
                // Resolve font once — prefer Arial, fall back to any available system font
                FontFamily? fontFamily = null;
                if (!SystemFonts.TryGet("Arial", out var arialFamily))
                {
                    var fallback = SystemFonts.Families.FirstOrDefault();
                    if (fallback != default) fontFamily = fallback;
                }
                else
                {
                    fontFamily = arialFamily;
                }

                if (fontFamily is not null)
                {
                    foreach (var tp in request.TextPlacements)
                    {
                        if (string.IsNullOrWhiteSpace(tp.Text)) continue;
                        var fontSize = Math.Max(8f, tp.FontSize);
                        var font = fontFamily.Value.CreateFont(fontSize, FontStyle.Regular);
                        if (!Color.TryParse(tp.Color, out var textColor))
                            textColor = Color.Black;

                        var textOptions = new RichTextOptions(font)
                        {
                            Origin = new PointF(tp.X, tp.Y),
                        };
                        productImage.Mutate(ctx => ctx.DrawText(textOptions, tp.Text, textColor));
                    }
                }
                else
                {
                    _logger.LogWarning("No system fonts found — text placements skipped.");
                }
            }

            var output = new MemoryStream();
            await productImage.SaveAsPngAsync(output, ct);
            output.Position = 0;

            return File(output, "image/png", $"mockup-{request.ProductId}.png");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error compositing logos for product {ProductId}", request.ProductId);
            return Problem("An error occurred while generating the mockup. Please try again.");
        }
    }
}
