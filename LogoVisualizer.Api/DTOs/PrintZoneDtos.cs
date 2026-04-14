using LogoVisualizer.Data.Models;

namespace LogoVisualizer.Api.DTOs;

// ---------------------------------------------------------------------------
// Response DTOs
// ---------------------------------------------------------------------------

public record PrintTechniqueDto(int Id, string Name)
{
    public static PrintTechniqueDto FromEntity(PrintTechnique t) => new(t.Id, t.Name);
}

public record PrintZoneDto(
    int Id,
    string Name,
    int X,
    int Y,
    int Width,
    int Height,
    decimal MaxPhysicalWidthMm,
    decimal MaxPhysicalHeightMm,
    int? MaxColors,
    List<PrintTechniqueDto> AllowedTechniques)
{
    public static PrintZoneDto FromEntity(PrintZone z) =>
        new(
            z.Id,
            z.Name,
            z.X,
            z.Y,
            z.Width,
            z.Height,
            z.MaxPhysicalWidthMm,
            z.MaxPhysicalHeightMm,
            z.MaxColors,
            z.AllowedTechniques
                .Select(pzt => PrintTechniqueDto.FromEntity(pzt.PrintTechnique))
                .ToList()
        );
}

// ---------------------------------------------------------------------------
// Request DTOs
// ---------------------------------------------------------------------------

public class CreatePrintZoneRequest
{
    public required string Name { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public decimal MaxPhysicalWidthMm { get; set; }
    public decimal MaxPhysicalHeightMm { get; set; }
    public int? MaxColors { get; set; }

    /// <summary>IDs from the PrintTechniques lookup table (/api/techniques).</summary>
    public List<int> AllowedTechniqueIds { get; set; } = [];
}

public class UpdatePrintZoneRequest : CreatePrintZoneRequest { }
