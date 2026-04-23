using LogoVisualizer.Api.DTOs;
using LogoVisualizer.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace LogoVisualizer.Api.Controllers;

[ApiController]
[Route("api/midocean-products")]
public class MidoceanProductsController : ControllerBase
{
    private readonly IMidoceanProductService _jsonService;
    private readonly IProductDataService _productData;

    public MidoceanProductsController(IMidoceanProductService jsonService, IProductDataService productData)
    {
        _jsonService = jsonService;
        _productData = productData;
    }

    /// <summary>All Midocean sample products — raw supplier format (always from JSON).</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<MidoceanProductDto>>(StatusCodes.Status200OK)]
    public IActionResult GetAll() => Ok(_jsonService.GetAll());

    /// <summary>Single Midocean product by master_code — raw supplier format (always from JSON).</summary>
    [HttpGet("{masterCode}")]
    [ProducesResponseType<MidoceanProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetByMasterCode(string masterCode)
    {
        var product = _jsonService.GetByMasterCode(masterCode);
        if (product is null) return NotFound(new { error = $"No product found with master_code '{masterCode}'." });
        return Ok(product);
    }

    /// <summary>
    /// All products adapted to the frontend Product shape — DB first, JSON fallback.
    /// </summary>
    [HttpGet("as-products")]
    [ProducesResponseType<IReadOnlyList<AdaptedProductDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAdapted(CancellationToken ct)
        => Ok(await _productData.GetAllAdaptedAsync(ct));

    /// <summary>Single product adapted to the frontend Product shape — DB first, JSON fallback.</summary>
    [HttpGet("{id}/as-product")]
    [ProducesResponseType<AdaptedProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAdaptedById(string id, CancellationToken ct)
    {
        var product = await _productData.GetAdaptedByIdAsync(id, ct);
        if (product is null) return NotFound(new { error = $"No product found with id '{id}'." });
        return Ok(product);
    }
}
