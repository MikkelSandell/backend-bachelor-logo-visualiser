namespace LogoVisualizer.Api.Services;

using LogoVisualizer.Api.DTOs;

/// <summary>
/// Validates export requests (PNG and PDF).
/// </summary>
public interface IExportValidator
{
    /// <summary>
    /// Validates PNG export request has at least one placement.
    /// </summary>
    /// <param name="placements">Logo placements</param>
    /// <param name="textPlacements">Text placements</param>
    /// <returns>Validation error if invalid; null if valid</returns>
    string? ValidatePngRequest(List<ZonePlacement>? placements, List<TextPlacement>? textPlacements);

    /// <summary>
    /// Validates background image URL is provided.
    /// </summary>
    /// <param name="backgroundImageUrl">Background image URL</param>
    /// <returns>Validation error if invalid; null if valid</returns>
    string? ValidateBackgroundImageUrl(string? backgroundImageUrl);

    /// <summary>
    /// Validates PDF export request has at least one page.
    /// </summary>
    /// <param name="pages">List of export pages</param>
    /// <returns>Validation error if invalid; null if valid</returns>
    string? ValidatePdfRequest(List<ExportPngRequest>? pages);
}
