using LogoVisualizer.Api.DTOs;
using LogoVisualizer.Data;
using LogoVisualizer.Data.Models;
using LogoVisualizer.Data.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogoVisualizer.Api.Controllers;

[ApiController]
[Route("api/products/{productId:int}/zones")]
public class PrintZonesController : ControllerBase
{
    private readonly IPrintZoneRepository _zones;
    private readonly IProductRepository _products;
    private readonly AppDbContext _db;

    public PrintZonesController(IPrintZoneRepository zones, IProductRepository products, AppDbContext db)
    {
        _zones = zones;
        _products = products;
        _db = db;
    }

    /// <summary>Lists all print zones for a product, including allowed techniques. Public.</summary>
    [HttpGet]
    [ProducesResponseType<List<PrintZoneDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAll(int productId, CancellationToken ct)
    {
        if (!await _products.ExistsAsync(productId, ct)) return NotFound();
        var zones = await _zones.GetByProductIdAsync(productId, ct);
        return Ok(zones.Select(PrintZoneDto.FromEntity));
    }

    /// <summary>Returns a single print zone by ID. Public.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<PrintZoneDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int productId, int id, CancellationToken ct)
    {
        var zone = await _zones.GetByIdAsync(id, ct);
        if (zone is null || zone.ProductId != productId) return NotFound();
        return Ok(PrintZoneDto.FromEntity(zone));
    }

    /// <summary>Creates a new print zone on the given product. Requires admin JWT.</summary>
    [Authorize]
    [HttpPost]
    [ProducesResponseType<PrintZoneDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(int productId, [FromBody] CreatePrintZoneRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        if (!await _products.ExistsAsync(productId, ct)) return NotFound();

        var zone = new PrintZone
        {
            ProductId = productId,
            Name = request.Name,
            X = request.X,
            Y = request.Y,
            Width = request.Width,
            Height = request.Height,
            MaxPhysicalWidthMm = request.MaxPhysicalWidthMm,
            MaxPhysicalHeightMm = request.MaxPhysicalHeightMm,
            MaxColors = request.MaxColors,
            ImageUrl = request.ImageUrl,
        };

        var techniques = await ResolveTechniquesAsync(request, ct);
        zone.AllowedTechniques = techniques
            .Select(t => new PrintZoneTechnique { PrintTechniqueId = t.Id })
            .ToList();

        var created = await _zones.CreateAsync(zone, ct);

        // Reload with navigation props for the response DTO
        var full = await _zones.GetByIdAsync(created.Id, ct);
        return CreatedAtAction(nameof(GetById), new { productId, id = created.Id }, PrintZoneDto.FromEntity(full!));
    }

    /// <summary>Updates an existing print zone. Requires admin JWT.</summary>
    [Authorize]
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int productId, int id, [FromBody] UpdatePrintZoneRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var zone = await _zones.GetByIdAsync(id, ct);
        if (zone is null || zone.ProductId != productId) return NotFound();

        zone.Name = request.Name;
        zone.X = request.X;
        zone.Y = request.Y;
        zone.Width = request.Width;
        zone.Height = request.Height;
        zone.MaxPhysicalWidthMm = request.MaxPhysicalWidthMm;
        zone.MaxPhysicalHeightMm = request.MaxPhysicalHeightMm;
        zone.MaxColors = request.MaxColors;
        if (request.ImageUrl is not null) zone.ImageUrl = request.ImageUrl;

        // Replace technique associations
        zone.AllowedTechniques.Clear();
        var techniques = await ResolveTechniquesAsync(request, ct);
        foreach (var t in techniques)
            zone.AllowedTechniques.Add(new PrintZoneTechnique { PrintZoneId = zone.Id, PrintTechniqueId = t.Id });

        await _zones.UpdateAsync(zone, ct);
        return NoContent();
    }

    /// <summary>Deletes a print zone. Requires admin JWT.</summary>
    [Authorize]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int productId, int id, CancellationToken ct)
    {
        var zone = await _zones.GetByIdAsync(id, ct);
        if (zone is null || zone.ProductId != productId) return NotFound();
        await _zones.DeleteAsync(id, ct);
        return NoContent();
    }

    // Resolves PrintTechnique entities from either names ("screen_print" / "Screen Print")
    // or IDs, with names taking priority.
    private async Task<List<PrintTechnique>> ResolveTechniquesAsync(CreatePrintZoneRequest request, CancellationToken ct)
    {
        var all = await _db.PrintTechniques.ToListAsync(ct);

        if (request.AllowedTechniqueNames.Count > 0)
        {
            return request.AllowedTechniqueNames
                .Select(name =>
                {
                    var normalized = name.Replace('_', ' ');
                    return all.FirstOrDefault(t =>
                        string.Equals(t.Name, normalized, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(t.Name.Replace(" ", "_"), name, StringComparison.OrdinalIgnoreCase));
                })
                .OfType<PrintTechnique>()
                .ToList();
        }

        return all.Where(t => request.AllowedTechniqueIds.Contains(t.Id)).ToList();
    }
}
