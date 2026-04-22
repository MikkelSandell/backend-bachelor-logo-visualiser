using LogoVisualizer.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace LogoVisualizer.Data.Repositories;

public class PrintZoneRepository : IPrintZoneRepository
{
    private readonly AppDbContext _db;
    private readonly IAuditLogRepository _auditLog;

    public PrintZoneRepository(AppDbContext db, IAuditLogRepository auditLog)
    {
        _db = db;
        _auditLog = auditLog;
    }

    public async Task<IEnumerable<PrintZone>> GetByProductIdAsync(int productId, CancellationToken ct = default) =>
        await _db.PrintZones
            .Include(z => z.AllowedTechniques)
                .ThenInclude(pzt => pzt.PrintTechnique)
            .Where(z => z.ProductId == productId)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<PrintZone?> GetByIdAsync(int id, CancellationToken ct = default) =>
        await _db.PrintZones
            .Include(z => z.AllowedTechniques)
                .ThenInclude(pzt => pzt.PrintTechnique)
            .FirstOrDefaultAsync(z => z.Id == id, ct);

    public async Task<PrintZone> CreateAsync(PrintZone zone, CancellationToken ct = default)
    {
        _db.PrintZones.Add(zone);
        await _db.SaveChangesAsync(ct);

        // Log audit
        await _auditLog.LogAsync("Create", "PrintZone", zone.Id, $"Zone '{zone.Name}' created", ct: ct);

        return zone;
    }

    public async Task UpdateAsync(PrintZone zone, CancellationToken ct = default)
    {
        _db.PrintZones.Update(zone);
        await _db.SaveChangesAsync(ct);

        // Log audit
        await _auditLog.LogAsync("Update", "PrintZone", zone.Id, $"Zone '{zone.Name}' updated", ct: ct);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var zone = await _db.PrintZones.FindAsync([id], ct);
        if (zone is null) return false;

        var zoneName = zone.Name;
        _db.PrintZones.Remove(zone);
        await _db.SaveChangesAsync(ct);

        // Log audit
        await _auditLog.LogAsync("Delete", "PrintZone", id, $"Zone '{zoneName}' deleted", ct: ct);

        return true;
    }
}
