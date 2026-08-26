using EcoTrack.IdentityService.DTOs;
using EcoTrack.IdentityService.Services;
using Microsoft.AspNetCore.Mvc;

namespace EcoTrack.IdentityService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// ECO-12: Register a new User or Recycler.
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var (success, statusCode, message, response) = await _authService.RegisterAsync(request, ipAddress, cancellationToken);

        if (!success)
        {
            return StatusCode(statusCode, new { message });
        }

        return StatusCode(StatusCodes.Status201Created, response);
    }
}
