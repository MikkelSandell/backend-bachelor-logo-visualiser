using LogoVisualizer.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace LogoVisualizer.Data.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _db;

    public ProductRepository(AppDbContext db) => _db = db;

    public async Task<IEnumerable<Product>> GetAllAsync(CancellationToken ct = default) =>
        await _db.Products
            .Include(p => p.PrintZones)
            .OrderBy(p => p.Title)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<Product?> GetByIdAsync(int id, CancellationToken ct = default) =>
        await _db.Products.FindAsync([id], ct);

    public async Task<Product?> GetByIdWithZonesAsync(int id, CancellationToken ct = default) =>
        await _db.Products
            .Include(p => p.PrintZones)
                .ThenInclude(z => z.AllowedTechniques)
                    .ThenInclude(pzt => pzt.PrintTechnique)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<Product> CreateAsync(Product product, CancellationToken ct = default)
    {
        product.CreatedAt = DateTime.UtcNow;
        product.UpdatedAt = DateTime.UtcNow;
        _db.Products.Add(product);
        await _db.SaveChangesAsync(ct);
        return product;
    }

    public async Task UpdateAsync(Product product, CancellationToken ct = default)
    {
        product.UpdatedAt = DateTime.UtcNow;
        _db.Products.Update(product);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var product = await _db.Products.FindAsync([id], ct);
        if (product is null) return false;
        _db.Products.Remove(product);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken ct = default) =>
        await _db.Products.AnyAsync(p => p.Id == id, ct);
}
