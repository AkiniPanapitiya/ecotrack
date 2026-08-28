using System.Security.Claims;
using EcoTrack.IdentityService.DTOs;
using EcoTrack.IdentityService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcoTrack.IdentityService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;

    public ProfileController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    /// <summary>
    /// ECO-14: Fetch the authenticated user's profile details.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "Invalid token claims." });
        }

        var profile = await _profileService.GetProfileAsync(userId.Value, cancellationToken);
        if (profile == null)
        {
            return NotFound(new { message = "User profile not found." });
        }

        return Ok(profile);
    }

    /// <summary>
    /// ECO-14: Update basic profile details (Name, Phone, Address, Recycler specs).
    /// </summary>
    [HttpPut]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "Invalid token claims." });
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var (success, statusCode, message, profile) = await _profileService.UpdateProfileAsync(
            userId.Value, dto, ipAddress, cancellationToken);

        if (!success)
        {
            return StatusCode(statusCode, new { message });
        }

        return Ok(new { message, profile });
    }

    private Guid? GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var guid) ? guid : null;
    }
}
