using LogoVisualizer.Data.Models;

namespace LogoVisualizer.Data.Repositories;

public interface IAuditLogRepository
{
    Task LogAsync(string operation, string entityType, int entityId, string description, string? userId = null, string? userEmail = null, CancellationToken ct = default);
    Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityType, int entityId, CancellationToken ct = default);
    Task<IEnumerable<AuditLog>> GetRecentAsync(int count = 100, CancellationToken ct = default);
    Task<IEnumerable<AuditLog>> GetByUserAsync(string userId, CancellationToken ct = default);
}
