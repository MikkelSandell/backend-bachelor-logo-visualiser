using LogoVisualizer.Api.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace LogoVisualizer.Api.Controllers;

/// <summary>
/// Accepts logo file uploads from the Viewer. Returns a temporary reference ID
/// that can be passed to POST /api/export/png.
/// Rate-limited — no authentication required.
/// </summary>
[ApiController]
[Route("api/logos")]
public class LogoUploadController : ControllerBase
{
    private static readonly HashSet<string> AllowedContentTypes =
        ["image/png", "image/jpeg", "image/svg+xml"];

    private const long MaxFileSizeBytes = 10_485_760; // 10 MB

    private readonly IWebHostEnvironment _env;
    private readonly ILogger<LogoUploadController> _logger;

    public LogoUploadController(IWebHostEnvironment env, ILogger<LogoUploadController> logger)
    {
        _env = env;
        _logger = logger;
    }

    /// <summary>
    /// Uploads a logo file (PNG, JPG or SVG) and returns a short-lived reference ID.
    /// The ID is valid for the session — no permanent storage is guaranteed.
    /// </summary>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10_485_760)]
    [ProducesResponseType<LogoUploadResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken ct)
    {
        if (!AllowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
            return BadRequest(new { error = "Unsupported file type. Allowed: PNG, JPG, SVG." });

        if (file.Length > MaxFileSizeBytes)
            return BadRequest(new { error = "File exceeds the 10 MB limit." });

        // SECURITY: SVG files can contain embedded scripts.
        // In production, run an SVG sanitizer (e.g., strip <script> tags) before saving.
        // For MVP this is noted but not yet implemented.
        if (file.ContentType.Equals("image/svg+xml", StringComparison.OrdinalIgnoreCase))
            _logger.LogWarning("SVG upload received — ensure SVG sanitisation is in place before production use.");

        var extension = file.ContentType.ToLowerInvariant() switch
        {
            "image/png"     => ".png",
            "image/jpeg"    => ".jpg",
            "image/svg+xml" => ".svg",
            _               => throw new InvalidOperationException("Unexpected content type after validation.")
        };

        var logoId = Guid.NewGuid().ToString("N");
        var fileName = $"{logoId}{extension}";
        var directory = Path.Combine(_env.ContentRootPath, "uploads", "logos");
        Directory.CreateDirectory(directory);

        var fullPath = Path.Combine(directory, fileName);
        await using var dest = System.IO.File.Create(fullPath);
        await file.CopyToAsync(dest, ct);

        var logoUrl = $"{Request.Scheme}://{Request.Host}/api/files/uploads/logos/{fileName}";

        return Ok(new LogoUploadResponse(logoId, logoUrl, file.ContentType, file.Length));
    }
}
