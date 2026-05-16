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

    /// <summary>URL of the blank product image for this specific print position (e.g. FRONT vs BACK).</summary>
    public string? ImageUrl { get; set; }

    /// <summary>Pre-set (locked) logo URL shown in the viewer. Null if no fixed logo.</summary>
    public string? FixedLogoUrl { get; set; }

    /// <summary>File ID of the fixed logo on disk — used by ExportController to resolve the file.</summary>
    public string? FixedLogoFileId { get; set; }

    /// <summary>Fixed logo position and size in product-image pixels.</summary>
    public int? FixedLogoX { get; set; }
    public int? FixedLogoY { get; set; }
    public int? FixedLogoWidth { get; set; }
    public int? FixedLogoHeight { get; set; }

    /// <summary>Print technique to simulate on the fixed logo (e.g. "engraving"). Null = full colour.</summary>
    public string? FixedLogoTechnique { get; set; }

    /// <summary>Colour count to simulate on the fixed logo. 0 = full colour, 1 = black, 2 = greyscale, etc.</summary>
    public int? FixedLogoColorCount { get; set; }

    public ICollection<PrintZoneTechnique> AllowedTechniques { get; set; } = new List<PrintZoneTechnique>();
}
