using LogoVisualizer.Data.Models;

namespace LogoVisualizer.Data.Repositories;

public interface IPrintZoneRepository
{
    Task<IEnumerable<PrintZone>> GetByProductIdAsync(int productId, CancellationToken ct = default);
    Task<PrintZone?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<PrintZone> CreateAsync(PrintZone zone, CancellationToken ct = default);
    Task UpdateAsync(PrintZone zone, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
