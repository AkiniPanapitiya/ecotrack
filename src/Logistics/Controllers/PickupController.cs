using System.Security.Claims;
using EcoTrack.LogisticsService.DTOs;
using EcoTrack.LogisticsService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcoTrack.LogisticsService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PickupController : ControllerBase
{
    private readonly IPickupService _pickupService;

    public PickupController(IPickupService pickupService)
    {
        _pickupService = pickupService;
    }

    /// <summary>
    /// ECO-15: Schedule a new e-waste pickup request.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(PickupRequestDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePickup([FromBody] CreatePickupRequestDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Try extract userId from Claims, default to a generated/demo Guid if running public
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userId = Guid.TryParse(userIdClaim, out var parsedId) ? parsedId : Guid.NewGuid();

        var (success, statusCode, message, data) = await _pickupService.CreatePickupRequestAsync(userId, dto, cancellationToken);
        if (!success)
        {
            return StatusCode(statusCode, new { message });
        }

        return StatusCode(StatusCodes.Status201Created, data);
    }

    /// <summary>
    /// ECO-15: Fetch a specific pickup request by its ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PickupRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPickupById(Guid id, CancellationToken cancellationToken)
    {
        var pickup = await _pickupService.GetPickupByIdAsync(id, cancellationToken);
        if (pickup == null)
        {
            return NotFound(new { message = "Pickup request not found." });
        }

        return Ok(pickup);
    }

    /// <summary>
    /// ECO-15: Fetch all pickup requests submitted by a specific user.
    /// </summary>
    [HttpGet("user/{userId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<PickupRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserPickups(Guid userId, CancellationToken cancellationToken)
    {
        var pickups = await _pickupService.GetPickupsByUserAsync(userId, cancellationToken);
        return Ok(pickups);
    }

    //ECO-74 Get all pending pickups for recyclers
    [HttpGet("pending")]
    [ProducesResponseType(typeof(IEnumerable<PickupRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingPickups(CancellationToken cancellationToken)
    {
        var pickups = await _pickupService.GetPendingPickupsAsync(cancellationToken);
        return Ok(pickups);
    }

    //ECO-74 Get recycler schedule
    [HttpGet("recycler/{recyclerId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<PickupRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecyclerSchedule(Guid recyclerId, CancellationToken cancellationToken)
    {
        var pickups = await _pickupService.GetRecyclerScheduleAsync(recyclerId, cancellationToken);
        return Ok(pickups);
    }

    //ECO-74 Confirm schedule
    [HttpPut("{id:guid}/schedule")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ConfirmSchedule(Guid id, [FromBody] ConfirmScheduleRequestDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var (success, statusCode, message) = await _pickupService.ConfirmScheduleAsync(id, dto, cancellationToken);
        return StatusCode(statusCode, new { message });
    }
    }
