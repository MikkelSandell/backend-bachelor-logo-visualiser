using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace LogoVisualizer.Api.Controllers;

/// <summary>
/// Serves uploaded files (logos, product images) with appropriate content-type headers.
/// Public — rate-limited via IpRateLimiting.
/// </summary>
[ApiController]
[Route("api/files")]
public class FilesController : ControllerBase
{
    private static readonly HashSet<string> AllowedExtensions = [".png", ".jpg", ".jpeg", ".svg", ".webp", ".gif"];
    private static readonly HashSet<string> AllowedExternalHosts =
    [
        "printposition-img-api-v2.cdn.midocean.com"
    ];
    private static readonly HttpClient HttpClient = new();

    private readonly IWebHostEnvironment _env;
    private readonly ILogger<FilesController> _logger;

    public FilesController(IWebHostEnvironment env, ILogger<FilesController> logger)
    {
        _env = env;
        _logger = logger;
    }

    /// <summary>
    /// Serves a file from the uploads directory with appropriate content-type header.
    /// Path segments are validated to prevent directory traversal attacks.
    /// </summary>
    [HttpGet("{*path}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileResult))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFile(string path, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(path))
            return BadRequest(new { error = "File path is required." });

        var decodedPath = WebUtility.UrlDecode(path);

        // External image proxy for trusted CDN hosts only.
        if (Uri.TryCreate(decodedPath, UriKind.Absolute, out var externalUri)
            && (externalUri.Scheme == Uri.UriSchemeHttp || externalUri.Scheme == Uri.UriSchemeHttps))
        {
            if (!AllowedExternalHosts.Contains(externalUri.Host))
                return BadRequest(new { error = "External host is not allowed." });

            var extension = Path.GetExtension(externalUri.AbsolutePath).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
                return BadRequest(new { error = "External file type not allowed." });

            HttpResponseMessage response;
            try
            {
                response = await HttpClient.GetAsync(externalUri, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch external file from {Url}", externalUri);
                return NotFound(new { error = "File not found." });
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("External file request failed: {Url} -> {StatusCode}", externalUri, (int)response.StatusCode);
                return NotFound(new { error = "File not found." });
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(ct);
            var upstreamContentType = response.Content.Headers.ContentType?.MediaType;
            var contentType = !string.IsNullOrWhiteSpace(upstreamContentType)
                              && upstreamContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
                ? upstreamContentType
                : GetContentTypeFromExtension(extension);

            return File(bytes, contentType);
        }

        // Prevent directory traversal attacks
        if (decodedPath.Contains("..") || decodedPath.Contains("~"))
            return BadRequest(new { error = "Invalid file path." });

        // Construct the full file path relative to content root
        var filePath = Path.Combine(_env.ContentRootPath, decodedPath.Replace('/', Path.DirectorySeparatorChar));

        // Verify the resolved path is within the allowed uploads directory
        var uploadsDir = Path.Combine(_env.ContentRootPath, "uploads");
        var resolvedPath = Path.GetFullPath(filePath);
        var resolvedUploadsDir = Path.GetFullPath(uploadsDir);

        if (!resolvedPath.StartsWith(resolvedUploadsDir, StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "Access to this path is not allowed." });

        if (!System.IO.File.Exists(filePath))
        {
            _logger.LogWarning("File not found: {FilePath}", filePath);
            return NotFound(new { error = "File not found." });
        }

        var fileExtension = Path.GetExtension(filePath).ToLowerInvariant();
        if (!AllowedExtensions.Contains(fileExtension))
            return BadRequest(new { error = "File type not allowed." });

        var localContentType = GetContentTypeFromExtension(fileExtension);

        var fileStream = System.IO.File.OpenRead(filePath);
        return File(fileStream, localContentType, Path.GetFileName(filePath));
    }

    private static string GetContentTypeFromExtension(string fileExtension) =>
        fileExtension switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".svg" => "image/svg+xml",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            _ => "application/octet-stream"
        };
}
