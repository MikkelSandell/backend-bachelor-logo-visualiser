namespace LogoVisualizer.Data.Models;

public class PrintZone
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    /// <summary>Human-readable name, e.g. "forside", "venstre bryst".</summary>
    public string Name { get; set; } = string.Empty;

    // Pixel coordinates of the zone on the product image
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    /// <summary>Maximum physical print width in millimetres.</summary>
    public decimal MaxPhysicalWidthMm { get; set; }

    /// <summary>Maximum physical print height in millimetres.</summary>
    public decimal MaxPhysicalHeightMm { get; set; }

    /// <summary>Maximum number of colours allowed. Null means unlimited.</summary>
    public int? MaxColors { get; set; }

    public ICollection<PrintZoneTechnique> AllowedTechniques { get; set; } = new List<PrintZoneTechnique>();
}
