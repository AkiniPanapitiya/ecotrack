using EcoTrack.IdentityService.DTOs;
using EcoTrack.IdentityService.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;

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

    /// <summary>
    /// ECO-13: Authenticate User/Recycler and issue JWT bearer token.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var (success, statusCode, message, response) = await _authService.LoginAsync(request, ipAddress, cancellationToken);

        if (!success)
        {
            return StatusCode(statusCode, new { message });
        }

        return Ok(response);
    }
    /// <summary>
    /// ECO-63: Logout - invalidate the current JWT by blacklisting its jti.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var jti = User.FindFirstValue(JwtRegisteredClaimNames.Jti);
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var expClaim = User.FindFirstValue(JwtRegisteredClaimNames.Exp);

        if (string.IsNullOrEmpty(jti) || string.IsNullOrEmpty(userIdClaim) || string.IsNullOrEmpty(expClaim))
        {
            return Unauthorized(new { message = "Invalid token." });
        }

        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(long.Parse(expClaim)).UtcDateTime;
        var (success, statusCode, message) = await _authService.LogoutAsync(
            jti, Guid.Parse(userIdClaim), expiresAt, cancellationToken);

        return StatusCode(statusCode, new { message });
    }

    /// <summary>
    /// ECO-68: Request a Forgot Password
    /// </summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var (success, statusCode, message) = await _authService.ForgotPasswordAsync(request, cancellationToken);

        return StatusCode(statusCode, new { message });
    }

}
