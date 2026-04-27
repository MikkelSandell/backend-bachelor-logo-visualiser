using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace LogoVisualizer.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;

    public AuthController(IConfiguration config, IWebHostEnvironment env)
    {
        _config = config;
        _env = env;
    }

    /// <summary>
    /// Issues a long-lived JWT for the admin tool.
    /// Only available when ASPNETCORE_ENVIRONMENT=Development.
    /// </summary>
    /// <returns>A signed JWT valid for 1 year with the "admin" role claim.</returns>
    /// <response code="200">Returns the signed JWT token.</response>
    /// <response code="404">Endpoint is disabled outside of the Development environment.</response>
    [HttpPost("dev-token")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult DevToken()
    {
        if (!_env.IsDevelopment())
            return NotFound();

        var key  = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer:            _config["Jwt:Issuer"],
            audience:          _config["Jwt:Audience"],
            claims:            [new Claim(ClaimTypes.Role, "admin")],
            expires:           DateTime.UtcNow.AddYears(1),
            signingCredentials: creds
        );

        return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
    }
}
