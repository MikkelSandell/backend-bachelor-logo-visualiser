namespace LogoVisualizer.Data.Models;

public class AuditLog
{
    public int Id { get; set; }
    public string Operation { get; set; } = null!;              // "Create", "Update", "Delete"
    public string EntityType { get; set; } = null!;             // "PrintZone", "Product"
    public int EntityId { get; set; }                           // ID af objektet der blev ændret
    public string? UserId { get; set; }                         // Fra JWT claim (sub)
    public string? UserEmail { get; set; }                      // Fra JWT claim (email)
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Description { get; set; } = null!;            // "Zone 'Forside' created"
}
