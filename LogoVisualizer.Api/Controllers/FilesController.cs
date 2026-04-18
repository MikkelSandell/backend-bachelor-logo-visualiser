using Microsoft.AspNetCore.Mvc;

namespace LogoVisualizer.Api.Controllers;

/// <summary>
/// Serves uploaded files (logos, product images) with appropriate content-type headers.
/// Public — rate-limited via IpRateLimiting.
/// </summary>
[ApiController]
[Route("api/files")]
public class FilesController : ControllerBase
{
    private static readonly HashSet<string> AllowedExtensions = [".png", ".jpg", ".jpeg", ".svg"];

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
    public IActionResult GetFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return BadRequest(new { error = "File path is required." });

        // Prevent directory traversal attacks
        if (path.Contains("..") || path.Contains("~"))
            return BadRequest(new { error = "Invalid file path." });

        // Construct the full file path relative to content root
        var filePath = Path.Combine(_env.ContentRootPath, path.Replace('/', Path.DirectorySeparatorChar));

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

        var contentType = fileExtension switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".svg" => "image/svg+xml",
            _ => "application/octet-stream"
        };

        var fileStream = System.IO.File.OpenRead(filePath);
        return File(fileStream, contentType, Path.GetFileName(filePath));
    }
}
