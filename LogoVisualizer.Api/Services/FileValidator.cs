namespace LogoVisualizer.Api.Services;

/// <summary>
/// Default implementation of IFileValidator.
/// Validates uploaded files for type, size, and format.
/// </summary>
public class FileValidator : IFileValidator
{
    public string? ValidateContentType(string? contentType, IEnumerable<string> allowedTypes)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return "Content type is required.";

        var normalizedType = contentType.ToLowerInvariant().Trim();
        var allowedLower = allowedTypes.Select(t => t.ToLowerInvariant()).ToList();

        if (!allowedLower.Contains(normalizedType))
            return $"Unsupported file type '{contentType}'. Allowed types: {string.Join(", ", allowedTypes)}.";

        return null;
    }

    public string? ValidateFileSize(long fileSizeBytes, long maxFileSizeBytes)
    {
        if (fileSizeBytes > maxFileSizeBytes)
            return $"File size ({fileSizeBytes} bytes) exceeds maximum of {maxFileSizeBytes} bytes ({maxFileSizeBytes / (1024 * 1024)} MB).";

        return null;
    }

    public string? ValidateFileNotEmpty(long fileSizeBytes)
    {
        if (fileSizeBytes <= 0)
            return "File is empty.";

        return null;
    }

    public string? ValidateFileExtension(string? fileName, string? contentType)
    {
        if (string.IsNullOrWhiteSpace(fileName) || string.IsNullOrWhiteSpace(contentType))
            return null;  // Cannot validate if inputs missing

        var normalizedType = contentType.ToLowerInvariant();
        var normalizedFileName = fileName.ToLowerInvariant();

        var expectedExtensions = normalizedType switch
        {
            "image/png" => new[] { ".png" },
            "image/jpeg" => new[] { ".jpg", ".jpeg" },
            "image/svg+xml" => new[] { ".svg" },
            "image/gif" => new[] { ".gif" },
            "image/webp" => new[] { ".webp" },
            _ => Array.Empty<string>()
        };

        if (expectedExtensions.Length == 0)
            return null;  // Unknown type, skip validation

        var hasValidExtension = expectedExtensions.Any(ext => normalizedFileName.EndsWith(ext));
        if (!hasValidExtension)
            return $"File extension does not match content type '{contentType}'.";

        return null;
    }
}
