using System.Security.Claims;
using EcoTrack.LogisticsService.DTOs;
using EcoTrack.LogisticsService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcoTrack.LogisticsService.Controllers;

/// <summary>
/// ECO-26 / E4.2: Drop-off & Dispatch Record, nested under the pickup it belongs to.
/// </summary>
[ApiController]
[Route("api/pickup/{pickupId:guid}/dropoff")]
[Authorize]
[Produces("application/json")]
public class DropoffController : ControllerBase
{
    private readonly IDropoffService _dropoffService;

    public DropoffController(IDropoffService dropoffService)
    {
        _dropoffService = dropoffService;
    }

    /// POST /api/pickup/{pickupId}/dropoff — Record drop-off and mark pickup Collected (assigned Recycler only)
    [HttpPost]
    [Authorize(Roles = "Recycler")]
    [ProducesResponseType(typeof(DropoffRecordDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RecordDropoff(
        Guid pickupId,
        [FromBody] CreateDropoffRecordRequestDto dto,
        CancellationToken cancellationToken)
    {
        var recyclerIdString = User.FindFirstValue(ClaimTypes.NameIdentifier)
                               ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(recyclerIdString, out var recyclerId))
        {
            return Unauthorized(new { message = "User ID not found in token." });
        }

        var (success, statusCode, message, record) = await _dropoffService.RecordDropoffAsync(
            pickupId, recyclerId, dto, cancellationToken);

        if (!success)
            return StatusCode(statusCode, new { message });

        return StatusCode(StatusCodes.Status201Created, record);
    }

    /// GET /api/pickup/{pickupId}/dropoff — View the drop-off record (pickup owner or assigned recycler)
    [HttpGet]
    [ProducesResponseType(typeof(DropoffRecordDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDropoff(Guid pickupId, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? User.FindFirstValue("sub");
        var requestingUserId = Guid.TryParse(userIdClaim, out var parsed) ? parsed : Guid.Empty;

        var (success, statusCode, message, record) = await _dropoffService.GetDropoffAsync(
            pickupId, requestingUserId, cancellationToken);

        if (!success)
            return StatusCode(statusCode, new { message });

        return Ok(record);
    }
}
