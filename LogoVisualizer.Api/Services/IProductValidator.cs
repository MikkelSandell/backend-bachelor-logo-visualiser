namespace LogoVisualizer.Api.Services;

/// <summary>
/// Validates product properties: title, image dimensions, etc.
/// </summary>
public interface IProductValidator
{
    /// <summary>
    /// Validates product title: not empty, not too long.
    /// </summary>
    /// <param name="title">Product title</param>
    /// <param name="minLength">Minimum title length (default 1)</param>
    /// <param name="maxLength">Maximum title length (default 500)</param>
    /// <returns>Validation error if invalid; null if valid</returns>
    string? ValidateTitle(string? title, int minLength = 1, int maxLength = 500);

    /// <summary>
    /// Validates product image dimensions are positive.
    /// </summary>
    /// <param name="width">Image width in pixels</param>
    /// <param name="height">Image height in pixels</param>
    /// <returns>Validation error if invalid; null if valid</returns>
    string? ValidateImageDimensions(int width, int height);
}
