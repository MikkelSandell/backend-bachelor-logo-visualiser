namespace LogoVisualizer.Api.Services;

/// <summary>
/// Default implementation of IPrintZoneValidator.
/// Validates print zone geometry and bounds.
/// </summary>
public class PrintZoneValidator : IPrintZoneValidator
{
    public string? ValidateName(string? name, int minLength = 1, int maxLength = 200)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Print zone name is required.";

        if (name.Length < minLength)
            return $"Print zone name must be at least {minLength} character(s).";

        if (name.Length > maxLength)
            return $"Print zone name must not exceed {maxLength} characters.";

        return null;
    }

    public string? ValidateCoordinates(int x, int y)
    {
        if (x < 0 || y < 0)
            return $"Zone coordinates must be non-negative (x: {x}, y: {y}).";

        return null;
    }

    public string? ValidateSize(int width, int height)
    {
        if (width <= 0 || height <= 0)
            return $"Zone dimensions must be positive (width: {width}, height: {height}).";

        return null;
    }

    public string? ValidateBounds(int x, int y, int width, int height, int imageWidth, int imageHeight)
    {
        if (x + width > imageWidth || y + height > imageHeight)
            return $"Zone must fit within image bounds (zone: x={x} y={y} width={width} height={height}, image: {imageWidth}x{imageHeight}).";

        return null;
    }
}
