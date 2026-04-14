namespace LogoVisualizer.Data.Models;

/// <summary>Many-to-many join between PrintZone and PrintTechnique.</summary>
public class PrintZoneTechnique
{
    public int PrintZoneId { get; set; }
    public PrintZone PrintZone { get; set; } = null!;

    public int PrintTechniqueId { get; set; }
    public PrintTechnique PrintTechnique { get; set; } = null!;
}
