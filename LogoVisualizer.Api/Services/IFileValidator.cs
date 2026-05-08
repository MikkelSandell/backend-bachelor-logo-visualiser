namespace LogoVisualizer.Api.Services;

/// <summary>
/// Validates uploaded files: content type, size, extension.
/// Used for product images, logos, and imports.
/// </summary>
public interface IFileValidator
{
    /// <summary>
    /// Validates a file's content type against allowed types.
    /// </summary>
    /// <param name="contentType">MIME type, e.g. "image/png"</param>
    /// <param name="allowedTypes">Allowed MIME types</param>
    /// <returns>Validation error if invalid; null if valid</returns>
    string? ValidateContentType(string? contentType, IEnumerable<string> allowedTypes);

    /// <summary>
    /// Validates file size against max limit.
    /// </summary>
    /// <param name="fileSizeBytes">File size in bytes</param>
    /// <param name="maxFileSizeBytes">Maximum allowed size in bytes</param>
    /// <returns>Validation error if invalid; null if valid</returns>
    string? ValidateFileSize(long fileSizeBytes, long maxFileSizeBytes);

    /// <summary>
    /// Validates file extension matches content type.
    /// </summary>
    /// <param name="fileName">File name with extension</param>
    /// <param name="contentType">MIME type to verify against</param>
    /// <returns>Validation error if invalid; null if valid</returns>
    string? ValidateFileExtension(string? fileName, string? contentType);

    /// <summary>
    /// Validates that file is not empty.
    /// </summary>
    /// <param name="fileSizeBytes">File size in bytes</param>
    /// <returns>Validation error if invalid; null if valid</returns>
    string? ValidateFileNotEmpty(long fileSizeBytes);
}
