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

        // Validate zone properties
        var validationErrors = ValidateZoneRequest(request);
        if (validationErrors.Count > 0)
            return BadRequest(new ValidationErrorResponse { Errors = validationErrors });

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
            FixedLogoUrl = request.FixedLogoUrl,
            FixedLogoFileId = request.FixedLogoFileId,
            FixedLogoX = request.FixedLogoX,
            FixedLogoY = request.FixedLogoY,
            FixedLogoWidth = request.FixedLogoWidth,
            FixedLogoHeight = request.FixedLogoHeight,
        };

        var resolution = await ResolveTechniquesAsync(request, ct);
        if (resolution.Errors.Count > 0)
            return BadRequest(new ValidationErrorResponse { Errors = resolution.Errors });

        zone.AllowedTechniques = resolution.Techniques
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
        zone.FixedLogoUrl = request.FixedLogoUrl;
        zone.FixedLogoFileId = request.FixedLogoFileId;
        zone.FixedLogoX = request.FixedLogoX;
        zone.FixedLogoY = request.FixedLogoY;
        zone.FixedLogoWidth = request.FixedLogoWidth;
        zone.FixedLogoHeight = request.FixedLogoHeight;

        // Replace technique associations
        zone.AllowedTechniques.Clear();
        var resolution = await ResolveTechniquesAsync(request, ct);
        if (resolution.Errors.Count > 0)
            return BadRequest(new ValidationErrorResponse { Errors = resolution.Errors });

        foreach (var t in resolution.Techniques)
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

    /// <summary>Validate a single zone creation request.</summary>
    private static List<string> ValidateZoneRequest(CreatePrintZoneRequest request)
    {
        var errors = new List<string>();

        // Zone must have a name
        if (string.IsNullOrWhiteSpace(request.Name))
            errors.Add("Zone name cannot be empty.");
        // Zone name must not exceed 200 characters
        else if (request.Name.Length > 200)
            errors.Add($"Zone name must not exceed 200 characters (length: {request.Name.Length}).");

        // Zone must have positive dimensions
        if (request.Width <= 0 || request.Height <= 0)
            errors.Add($"Zone must have positive width and height (width: {request.Width}, height: {request.Height}).");

        // Zone coordinates must be non-negative
        if (request.X < 0 || request.Y < 0)
            errors.Add($"Zone coordinates must be non-negative (x: {request.X}, y: {request.Y}).");

        return errors;
    }

    // Resolves PrintTechnique entities by exact slug name match (e.g. "screen_print").
    // Falls back to ID lookup if no names provided.
    private async Task<(List<PrintTechnique> Techniques, List<string> Errors)> ResolveTechniquesAsync(CreatePrintZoneRequest request, CancellationToken ct)
    {
        var all = await _db.PrintTechniques.ToListAsync(ct);

        if (request.AllowedTechniqueNames.Count > 0)
        {
            var errors = new List<string>();
            var techniques = new List<PrintTechnique>();

            foreach (var rawName in request.AllowedTechniqueNames)
            {
                var name = rawName?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(name))
                {
                    errors.Add($"Unknown technique '{rawName}'.");
                    continue;
                }

                var match = all.FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
                if (match is null)
                {
                    errors.Add($"Unknown technique '{name}'.");
                    continue;
                }

                if (!techniques.Any(t => t.Id == match.Id))
                {
                    techniques.Add(match);
                }
            }

            return (techniques, errors);
        }

        return (all.Where(t => request.AllowedTechniqueIds.Contains(t.Id)).ToList(), []);
    }
}
