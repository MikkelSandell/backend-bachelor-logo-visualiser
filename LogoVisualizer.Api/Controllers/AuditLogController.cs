using LogoVisualizer.Data.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogoVisualizer.Api.Controllers;

[ApiController]
[Route("api/audit")]
public class AuditLogController : ControllerBase
{
    private readonly IAuditLogRepository _auditLog;

    public AuditLogController(IAuditLogRepository auditLog) => _auditLog = auditLog;

    /// <summary>Get recent audit logs (admin only).</summary>
    [Authorize]
    [HttpGet("recent")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecent([FromQuery] int count = 100, CancellationToken ct = default)
    {
        var logs = await _auditLog.GetRecentAsync(count, ct);
        return Ok(logs);
    }

    /// <summary>Get audit logs for a specific entity (admin only).</summary>
    [Authorize]
    [HttpGet("entity/{entityType}/{entityId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByEntity(string entityType, int entityId, CancellationToken ct = default)
    {
        var logs = await _auditLog.GetByEntityAsync(entityType, entityId, ct);
        return Ok(logs);
    }

    /// <summary>Get audit logs for a specific user (admin only).</summary>
    [Authorize]
    [HttpGet("user/{userId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByUser(string userId, CancellationToken ct = default)
    {
        var logs = await _auditLog.GetByUserAsync(userId, ct);
        return Ok(logs);
    }
}
