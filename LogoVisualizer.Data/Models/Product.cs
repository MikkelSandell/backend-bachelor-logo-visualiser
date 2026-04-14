namespace LogoVisualizer.Data.Models;

public class Product
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;

    /// <summary>Relative path to the stored product image, e.g. "uploads/products/abc.png".</summary>
    public string ImagePath { get; set; } = string.Empty;

    /// <summary>Width of the product image in pixels.</summary>
    public int ImageWidth { get; set; }

    /// <summary>Height of the product image in pixels.</summary>
    public int ImageHeight { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PrintZone> PrintZones { get; set; } = new List<PrintZone>();
}
