namespace LogoVisualizer.Api.Services;

/// <summary>
/// Validates print techniques: existence, format, etc.
/// </summary>
public interface IPrintTechniqueValidator
{
    /// <summary>
    /// Validates a technique name exists and returns the ID.
    /// </summary>
    /// <param name="techniqueName">Technique name (case-insensitive)</param>
    /// <param name="knownTechniques">Dictionary of known technique names (slug) to IDs</param>
    /// <returns>Tuple (IsValid, TechniqueId, ErrorMessage)</returns>
    (bool IsValid, int TechniqueId, string? Error) ValidateTechniqueName(
        string? techniqueName,
        Dictionary<string, int> knownTechniques);

    /// <summary>
    /// Validates a list of technique names and returns all valid IDs.
    /// </summary>
    /// <param name="techniqueNames">List of technique names to validate</param>
    /// <param name="knownTechniques">Dictionary of known technique names (slug) to IDs</param>
    /// <returns>Tuple (TechniqueIds, Errors). IDs only include valid ones; Errors contains all validation messages</returns>
    (List<int> TechniqueIds, List<string> Errors) ValidateTechniqueNames(
        IEnumerable<string>? techniqueNames,
        Dictionary<string, int> knownTechniques);
}
