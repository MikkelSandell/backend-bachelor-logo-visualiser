namespace LogoVisualizer.Api.Services;

/// <summary>
/// Validates print zone properties: name, coordinates, size, bounds.
/// </summary>
public interface IPrintZoneValidator
{
    /// <summary>
    /// Validates zone name: not empty, not too long.
    /// </summary>
    /// <param name="name">Zone name</param>
    /// <param name="minLength">Minimum name length (default 1)</param>
    /// <param name="maxLength">Maximum name length (default 200)</param>
    /// <returns>Validation error if invalid; null if valid</returns>
    string? ValidateName(string? name, int minLength = 1, int maxLength = 200);

    /// <summary>
    /// Validates zone coordinates are non-negative.
    /// </summary>
    /// <param name="x">X coordinate (pixels)</param>
    /// <param name="y">Y coordinate (pixels)</param>
    /// <returns>Validation error if invalid; null if valid</returns>
    string? ValidateCoordinates(int x, int y);

    /// <summary>
    /// Validates zone size (width and height are positive).
    /// </summary>
    /// <param name="width">Zone width in pixels</param>
    /// <param name="height">Zone height in pixels</param>
    /// <returns>Validation error if invalid; null if valid</returns>
    string? ValidateSize(int width, int height);

    /// <summary>
    /// Validates zone fits within image bounds.
    /// </summary>
    /// <param name="x">Zone X coordinate</param>
    /// <param name="y">Zone Y coordinate</param>
    /// <param name="width">Zone width</param>
    /// <param name="height">Zone height</param>
    /// <param name="imageWidth">Image width</param>
    /// <param name="imageHeight">Image height</param>
    /// <returns>Validation error if invalid; null if valid</returns>
    string? ValidateBounds(int x, int y, int width, int height, int imageWidth, int imageHeight);
}
