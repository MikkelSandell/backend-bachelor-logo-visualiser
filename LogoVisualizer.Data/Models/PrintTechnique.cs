namespace LogoVisualizer.Data.Models;

public class PrintTechnique
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<PrintZoneTechnique> PrintZoneTechniques { get; set; } = new List<PrintZoneTechnique>();
}
