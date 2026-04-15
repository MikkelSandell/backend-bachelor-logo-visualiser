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

    /// <summary>Returns all 10 Midocean sample products with full print position data.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<MidoceanProductDto>>(StatusCodes.Status200OK)]
    public IActionResult GetAll() => Ok(_service.GetAll());

    /// <summary>Returns a single Midocean product by its master_code (e.g. S11500). Public.</summary>
    [HttpGet("{masterCode}")]
    [ProducesResponseType<MidoceanProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetByMasterCode(string masterCode)
    {
        var product = _service.GetByMasterCode(masterCode);
        if (product is null) return NotFound(new { error = $"No product found with master_code '{masterCode}'." });
        return Ok(product);
    }
}
