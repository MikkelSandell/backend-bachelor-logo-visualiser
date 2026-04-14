using LogoVisualizer.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace LogoVisualizer.Data.Repositories;

public class PrintZoneRepository : IPrintZoneRepository
{
    private readonly AppDbContext _db;

    public PrintZoneRepository(AppDbContext db) => _db = db;

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
        return zone;
    }

    public async Task UpdateAsync(PrintZone zone, CancellationToken ct = default)
    {
        _db.PrintZones.Update(zone);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var zone = await _db.PrintZones.FindAsync([id], ct);
        if (zone is null) return false;
        _db.PrintZones.Remove(zone);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
