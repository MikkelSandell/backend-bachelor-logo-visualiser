namespace LogoVisualizer.Api.Services;

using LogoVisualizer.Api.DTOs;

/// <summary>
/// Default implementation of IExportValidator.
/// Validates export requests.
/// </summary>
public class ExportValidator : IExportValidator
{
    public string? ValidatePngRequest(List<ZonePlacement>? placements, List<TextPlacement>? textPlacements)
    {
        var hasLogos = placements?.Count > 0;
        var hasText = textPlacements?.Count > 0;

        if (!hasLogos && !hasText)
            return "At least one logo or text placement is required.";

        return null;
    }

    public string? ValidateBackgroundImageUrl(string? backgroundImageUrl)
    {
        if (string.IsNullOrWhiteSpace(backgroundImageUrl))
            return "BackgroundImageUrl is required.";

        return null;
    }

    public string? ValidatePdfRequest(List<ExportPngRequest>? pages)
    {
        if (pages == null || pages.Count == 0)
            return "At least one export page is required.";

        return null;
    }
}
