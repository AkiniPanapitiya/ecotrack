using EcoTrack.IdentityService.DTOs;
using EcoTrack.IdentityService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EcoTrack.IdentityService.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public class RoleController : ControllerBase
{
    private readonly IRoleService _roleService;

    public RoleController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    /// GET /api/admin/users — List users with optional filters + sort (Admin only)
    [HttpGet("users")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllUsers(
        [FromQuery] string? role = null,
        [FromQuery] bool? status = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDesc = false,
        CancellationToken cancellationToken = default)
    {
        var (success, statusCode, message, users) = await _roleService.GetAllUsersAsync(
            role, status, sortBy, sortDesc, cancellationToken);
        if (!success)
            return StatusCode(statusCode, new { message });
        return Ok(new { message, users });
    }

    /// PUT /api/admin/users/{userId}/role — Change a user's role (Admin only)
    [HttpPut("users/{userId:guid}/role")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ChangeUserRole(
        Guid userId,
        [FromBody] ChangeRoleRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(adminId))
            return Unauthorized(new { message = "Invalid token." });

        var (success, statusCode, message, response) = await _roleService.ChangeUserRoleAsync(
            userId, adminId, dto, cancellationToken);
        if (!success)
            return StatusCode(statusCode, new { message });
        return Ok(new { message, response });
    }

    /// PATCH /api/admin/users/{userId}/active — Toggle user active status (Admin only)
    [HttpPatch("users/{userId:guid}/active")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleActiveStatus(
        Guid userId,
        [FromBody] object body,
        CancellationToken cancellationToken = default)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(adminId))
            return Unauthorized(new { message = "Invalid token." });

        var user = await _roleService.GetUserByIdAsync(userId, cancellationToken);
        if (user == null)
            return NotFound(new { message = "User not found." });

        // Block self-toggle — can't deactivate yourself
        var adminUser = await _roleService.GetUserByIdAsync(Guid.Parse(adminId), cancellationToken);
        if (adminUser != null && adminUser.Id == userId)
            return BadRequest(new { message = "You cannot change your own active status." });

        var newStatus = (bool)((System.Text.Json.JsonElement)body).GetProperty("isActive").GetBoolean();
        var success = await _roleService.UpdateActiveStatusAsync(userId, newStatus, cancellationToken);
        if (!success)
            return StatusCode(500, new { message = "Failed to update active status." });

        await _roleService.LogAuditAsync(adminId, $"ACTIVATE_STATUS_CHANGED: Set {user.Email} active={newStatus}");

        return Ok(new { message = $"User {(newStatus ? "activated" : "deactivated")} successfully.", isActive = newStatus });
    }
}
