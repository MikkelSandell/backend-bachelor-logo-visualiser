namespace LogoVisualizer.Api.Services;

/// <summary>
/// Default implementation of IProductValidator.
/// Validates product metadata.
/// </summary>
public class ProductValidator : IProductValidator
{
    public string? ValidateTitle(string? title, int minLength = 1, int maxLength = 500)
    {
        if (string.IsNullOrWhiteSpace(title))
            return "Product title is required.";

        if (title.Length < minLength)
            return $"Product title must be at least {minLength} character(s).";

        if (title.Length > maxLength)
            return $"Product title must not exceed {maxLength} characters.";

        return null;
    }

    public string? ValidateImageDimensions(int width, int height)
    {
        if (width <= 0 || height <= 0)
            return $"Image dimensions must be positive (width: {width}, height: {height}).";

        return null;
    }
}
