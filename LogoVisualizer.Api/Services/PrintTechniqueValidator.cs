namespace LogoVisualizer.Api.Services;

/// <summary>
/// Default implementation of IPrintTechniqueValidator.
/// Validates print technique names against known techniques.
/// </summary>
public class PrintTechniqueValidator : IPrintTechniqueValidator
{
    public (bool IsValid, int TechniqueId, string? Error) ValidateTechniqueName(
        string? techniqueName,
        Dictionary<string, int> knownTechniques)
    {
        if (string.IsNullOrWhiteSpace(techniqueName))
            return (false, 0, "Technique name is required.");

        var normalized = techniqueName.Trim().ToLowerInvariant();
        if (knownTechniques.TryGetValue(normalized, out var techniqueId))
            return (true, techniqueId, null);

        return (false, 0, $"Unknown print technique '{techniqueName}'.");
    }

    public (List<int> TechniqueIds, List<string> Errors) ValidateTechniqueNames(
        IEnumerable<string>? techniqueNames,
        Dictionary<string, int> knownTechniques)
    {
        var ids = new List<int>();
        var errors = new List<string>();

        if (techniqueNames == null)
            return (ids, errors);

        var nameList = techniqueNames.ToList();
        foreach (var name in nameList)
        {
            var (isValid, techniqueId, error) = ValidateTechniqueName(name, knownTechniques);
            if (isValid)
                ids.Add(techniqueId);
            else if (error != null)
                errors.Add(error);
        }

        return (ids, errors);
    }
}
