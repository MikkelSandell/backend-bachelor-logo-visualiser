using LogoVisualizer.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace LogoVisualizer.Data.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly AppDbContext _db;

    public AuditLogRepository(AppDbContext db) => _db = db;

    public async Task LogAsync(string operation, string entityType, int entityId, string description, string? userId = null, string? userEmail = null, CancellationToken ct = default)
    {
        var auditLog = new AuditLog
        {
            Operation = operation,
            EntityType = entityType,
            EntityId = entityId,
            Description = description,
            UserId = userId,
            UserEmail = userEmail,
            Timestamp = DateTime.UtcNow
        };

        _db.AuditLogs.Add(auditLog);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityType, int entityId, CancellationToken ct = default) =>
        await _db.AuditLogs
            .Where(al => al.EntityType == entityType && al.EntityId == entityId)
            .OrderByDescending(al => al.Timestamp)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<IEnumerable<AuditLog>> GetRecentAsync(int count = 100, CancellationToken ct = default) =>
        await _db.AuditLogs
            .OrderByDescending(al => al.Timestamp)
            .Take(count)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<IEnumerable<AuditLog>> GetByUserAsync(string userId, CancellationToken ct = default) =>
        await _db.AuditLogs
            .Where(al => al.UserId == userId)
            .OrderByDescending(al => al.Timestamp)
            .AsNoTracking()
            .ToListAsync(ct);
}
