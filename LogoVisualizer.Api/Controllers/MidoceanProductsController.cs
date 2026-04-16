using LogoVisualizer.Api.DTOs;
using LogoVisualizer.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace LogoVisualizer.Api.Controllers;

[ApiController]
[Route("api/midocean-products")]
public class MidoceanProductsController : ControllerBase
{
    private readonly IMidoceanProductService _service;

    public MidoceanProductsController(IMidoceanProductService service)
    {
        _service = service;
    }

    /// <summary>All 10 Midocean sample products — raw supplier format.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<MidoceanProductDto>>(StatusCodes.Status200OK)]
    public IActionResult GetAll() => Ok(_service.GetAll());

    /// <summary>Single Midocean product by master_code — raw supplier format.</summary>
    [HttpGet("{masterCode}")]
    [ProducesResponseType<MidoceanProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetByMasterCode(string masterCode)
    {
        var product = _service.GetByMasterCode(masterCode);
        if (product is null) return NotFound(new { error = $"No product found with master_code '{masterCode}'." });
        return Ok(product);
    }

    /// <summary>
    /// All 10 Midocean products adapted to the frontend Product shape
    /// (id, title, imageUrl, imageWidth, imageHeight, printZones).
    /// Use this endpoint from the viewer and admin apps.
    /// </summary>
    [HttpGet("as-products")]
    [ProducesResponseType<IReadOnlyList<AdaptedProductDto>>(StatusCodes.Status200OK)]
    public IActionResult GetAllAdapted() => Ok(_service.GetAllAdapted());

    /// <summary>Single Midocean product adapted to the frontend Product shape.</summary>
    [HttpGet("{masterCode}/as-product")]
    [ProducesResponseType<AdaptedProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetAdaptedByMasterCode(string masterCode)
    {
        var product = _service.GetAdaptedByMasterCode(masterCode);
        if (product is null) return NotFound(new { error = $"No product found with master_code '{masterCode}'." });
        return Ok(product);
    }
}
