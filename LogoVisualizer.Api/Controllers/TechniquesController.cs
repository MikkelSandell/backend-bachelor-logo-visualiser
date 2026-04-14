using LogoVisualizer.Data;
using LogoVisualizer.Api.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogoVisualizer.Api.Controllers;

/// <summary>Read-only lookup for all available print techniques. Used by admin to populate dropdowns.</summary>
[ApiController]
[Route("api/[controller]")]
public class TechniquesController : ControllerBase
{
    private readonly AppDbContext _db;

    public TechniquesController(AppDbContext db) => _db = db;

    /// <summary>Returns all print techniques. Public.</summary>
    [HttpGet]
    [ProducesResponseType<List<PrintTechniqueDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var techniques = await _db.PrintTechniques
            .OrderBy(t => t.Name)
            .AsNoTracking()
            .ToListAsync(ct);

        return Ok(techniques.Select(PrintTechniqueDto.FromEntity));
    }
}
