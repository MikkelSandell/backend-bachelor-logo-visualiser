using LogoVisualizer.Api.DTOs;
using LogoVisualizer.Api.Helpers;
using LogoVisualizer.Api.Services;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using SkiaSharp;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using Svg.Skia;
using UploadExportRequest = LogoVisualizer.Api.DTOs.ExportPngRequest;

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

        var result = await GenerateMockupPngAsync(request, ct);
        if (result.Error is not null) return result.Error;

        return File(result.Png!, "image/png", $"mockup-{request.ProductId}.png");
    }

    [HttpPost("pdf")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileResult))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GeneratePdf([FromBody] MultiPagePdfExportRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        if (request.Pages is null || request.Pages.Count == 0)
            return BadRequest(new { error = "At least one export page is required." });

        var pngPages = new List<byte[]>(request.Pages.Count);
        foreach (var page in request.Pages)
        {
            if (page is null)
                return BadRequest(new { error = "Each page must be a valid export request object." });

            var pageResult = await GenerateMockupPngAsync(page, ct);
            if (pageResult.Error is not null) return pageResult.Error;
            pngPages.Add(pageResult.Png!);
        }

        try
        {
            var pdfBytes = CreateMultiPagePdfFromPngs(pngPages);
            return File(pdfBytes, "application/pdf", "logo-visualisering.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF export.");
            return Problem("An error occurred while generating the PDF. Please try again.");
        }
    }

    private async Task<(byte[]? Png, IActionResult? Error)> GenerateMockupPngAsync(UploadExportRequest request, CancellationToken ct)
    {

        if ((request.Placements is null || request.Placements.Count == 0)
            && (request.TextPlacements is null || request.TextPlacements.Count == 0))
            return (null, BadRequest(new { error = "At least one logo or text placement is required." }));

        if (string.IsNullOrWhiteSpace(request.BackgroundImageUrl))
            return (null, BadRequest(new { error = "BackgroundImageUrl is required." }));

        // Fetch the adapted product — supports numeric DB IDs and Midocean master codes
        var product = await _productData.GetAdaptedByIdAsync(request.ProductId, ct);
        if (product is null)
            return (null, NotFound(new { error = $"Product '{request.ProductId}' not found." }));

        var logoDir = Path.Combine(_env.ContentRootPath, "uploads", "logos");

        // Resolve and validate every logo placement before touching the image
        var resolved = new List<(string logoPath, ZonePlacement p)>();
        foreach (var placement in request.Placements ?? [])
        {
            var zone = product.PrintZones.FirstOrDefault(z => z.Id == placement.ZoneId);
            if (zone is null)
                return (null, NotFound(new { error = $"Zone '{placement.ZoneId}' not found." }));

            if (!Directory.Exists(logoDir))
                return (null, BadRequest(new { error = "No logos have been uploaded yet." }));

            var logoFiles = Directory.GetFiles(logoDir, $"{placement.LogoId}.*");
            if (logoFiles.Length == 0)
                return (null, BadRequest(new { error = $"Logo '{placement.LogoId}' not found. Re-upload it and try again." }));

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
                return (null, Problem($"Failed to download product image. Status: {bgResponse.StatusCode}"));
            }

            using var bgStream = await bgResponse.Content.ReadAsStreamAsync(ct);
            using var productImage = await Image.LoadAsync(bgStream, ct);

            // Composite each logo in order
            foreach (var (logoPath, placement) in resolved)
            {
                var zone = product.PrintZones.First(z => z.Id == placement.ZoneId);

                // Use the position and size the user set in the viewer.
                // Fall back to auto-fit only when the frontend sends no explicit size
                // (e.g. an export triggered before any interaction).
                var renderRect = (placement.LogoWidth > 0 && placement.LogoHeight > 0)
                    ? new LogoRenderRect(placement.LogoX, placement.LogoY, placement.LogoWidth, placement.LogoHeight)
                    : await CalculateRenderRectAsync(logoPath, zone, ct);

                using var logoImage = await LoadLogoImageForCompositingAsync(
                    logoPath,
                    renderRect.Width,
                    renderRect.Height,
                    ct);

                PrintTechniqueColorModeHelper.ApplyColorModeForTechnique(
                    logoImage,
                    placement.SelectedTechniqueName);

                if (placement.ColorCount > 0 && placement.MaxColors > 0 && placement.ColorCount < placement.MaxColors)
                    ApplyColorCount(logoImage, placement.ColorCount, placement.MaxColors);

                productImage.Mutate(ctx =>
                    ctx.DrawImage(logoImage, new Point(renderRect.X, renderRect.Y), 1f));
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

            using var output = new MemoryStream();
            await productImage.SaveAsPngAsync(output, ct);
            return (output.ToArray(), null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error compositing logos for product {ProductId}", request.ProductId);
            return (null, Problem("An error occurred while generating the mockup. Please try again."));
        }
    }

    private static byte[] CreateMultiPagePdfFromPngs(IReadOnlyList<byte[]> pngPages)
    {
        if (pngPages is null || pngPages.Count == 0)
            throw new ArgumentException("At least one PNG page is required.", nameof(pngPages));

        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        var pdf = Document.Create(container =>
        {
            foreach (var png in pngPages)
            {
                container.Page(page =>
                {
                    page.Margin(0);
                    page.Size(PageSizes.A4);
                    page.Content().Image(png).FitArea();
                });
            }
        });

        return pdf.GeneratePdf();
    }

    /// <summary>
    /// Applies colour-count quantisation to match the viewer's canvas preview.
    /// ColorCount=1 → black silhouette, =2 → greyscale, &gt;2 → posterise.
    /// </summary>
    private static void ApplyColorCount(Image image, int colorCount, int maxColors)
    {
        if (colorCount == 1)
        {
            // Black silhouette: greyscale first, then zero out R/G/B while keeping alpha.
            image.Mutate(ctx => ctx.Grayscale());
            if (image is Image<SixLabors.ImageSharp.PixelFormats.Rgba32> rgba)
            {
                rgba.ProcessPixelRows(accessor =>
                {
                    for (var y = 0; y < accessor.Height; y++)
                    {
                        var row = accessor.GetRowSpan(y);
                        for (var x = 0; x < row.Length; x++)
                        {
                            row[x].R = 0; row[x].G = 0; row[x].B = 0;
                        }
                    }
                });
            }
        }
        else if (colorCount == 2)
        {
            image.Mutate(ctx => ctx.Grayscale());
        }
        else
        {
            // Mirror frontend: levels = round((colorCount / maxColors) * 8), min 2.
            var levels = Math.Max(2, (int)Math.Round(colorCount / (double)maxColors * 8));
            PrintTechniqueColorModeHelper.PosterizeManual(image, levels);
        }
    }

    private static async Task<LogoRenderRect> CalculateRenderRectAsync(string logoPath, AdaptedPrintZoneDto zone, CancellationToken ct)
    {
        if (!string.Equals(Path.GetExtension(logoPath), ".svg", StringComparison.OrdinalIgnoreCase))
        {
            var info = await Image.IdentifyAsync(logoPath, ct)
                ?? throw new InvalidOperationException("Unable to identify logo image.");

            return LogoPlacementCalculator.CalculateFitToZone(
                info.Width,
                info.Height,
                zone.X,
                zone.Y,
                zone.Width,
                zone.Height);
        }

        using var fs = System.IO.File.OpenRead(logoPath);
        var svg = new SKSvg();
        var picture = svg.Load(fs) ?? throw new InvalidOperationException("Invalid SVG logo file.");

        var sourceWidth = Math.Max(1, (int)Math.Ceiling(picture.CullRect.Width));
        var sourceHeight = Math.Max(1, (int)Math.Ceiling(picture.CullRect.Height));

        return LogoPlacementCalculator.CalculateFitToZone(
            sourceWidth,
            sourceHeight,
            zone.X,
            zone.Y,
            zone.Width,
            zone.Height);
    }

    private static async Task<Image> LoadLogoImageForCompositingAsync(string logoPath, int targetWidth, int targetHeight, CancellationToken ct)
    {
        if (!string.Equals(Path.GetExtension(logoPath), ".svg", StringComparison.OrdinalIgnoreCase))
        {
            var raster = await Image.LoadAsync(logoPath, ct);
            raster.Mutate(ctx => ctx.Resize(targetWidth, targetHeight));
            return raster;
        }

        var width = Math.Max(1, targetWidth);
        var height = Math.Max(1, targetHeight);

        using var fs = System.IO.File.OpenRead(logoPath);
        var svg = new SKSvg();
        var picture = svg.Load(fs) ?? throw new InvalidOperationException("Invalid SVG logo file.");

        using var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);

        var sx = width / picture.CullRect.Width;
        var sy = height / picture.CullRect.Height;
        canvas.Scale(sx, sy);
        canvas.DrawPicture(picture);
        canvas.Flush();

        using var skImage = SKImage.FromBitmap(bitmap);
        using var data = skImage.Encode(SKEncodedImageFormat.Png, 100);
        var pngBytes = data.ToArray();
        using var pngStream = new MemoryStream(pngBytes);
        return await Image.LoadAsync(pngStream, ct);
    }
}
