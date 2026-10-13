using System.Security.Claims;
using EcoTrack.LogisticsService.DTOs;
using EcoTrack.LogisticsService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcoTrack.LogisticsService.Controllers;

[ApiController]
[Route("api/drivers")]
[Authorize]
[Produces("application/json")]
public class DriverController : ControllerBase
{
    private readonly IDriverService _driverService;

    public DriverController(IDriverService driverService)
    {
        _driverService = driverService;
    }

    /// POST /api/drivers — Add a new driver (Recycler only)
    [HttpPost]
    [Authorize(Roles = "Recycler")]
    [ProducesResponseType(typeof(DriverDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddDriver([FromBody] CreateDriverRequestDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var recyclerId = GetUserId();
        if (recyclerId == null)
        {
            return Unauthorized(new { message = "User ID not found in token." });
        }

        var (success, statusCode, message, driver) = await _driverService.AddDriverAsync(recyclerId.Value, dto, cancellationToken);
        if (!success)
        {
            return StatusCode(statusCode, new { message });
        }

        return StatusCode(StatusCodes.Status201Created, driver);
    }

    /// GET /api/drivers — List my active drivers (Recycler only)
    [HttpGet]
    [Authorize(Roles = "Recycler")]
    [ProducesResponseType(typeof(IEnumerable<DriverDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyDrivers(CancellationToken cancellationToken)
    {
        var recyclerId = GetUserId();
        if (recyclerId == null)
        {
            return Unauthorized(new { message = "User ID not found in token." });
        }

        var drivers = await _driverService.GetMyDriversAsync(recyclerId.Value, cancellationToken);
        return Ok(drivers);
    }

    /// PUT /api/drivers/pickups/{pickupId} — Assign or change the driver of a pickup (Recycler only)
    [HttpPut("pickups/{pickupId:guid}")]
    [Authorize(Roles = "Recycler")]
    [ProducesResponseType(typeof(PickupDriverDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignDriver(Guid pickupId, [FromBody] AssignDriverRequestDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var recyclerId = GetUserId();
        if (recyclerId == null)
        {
            return Unauthorized(new { message = "User ID not found in token." });
        }

        var (success, statusCode, message, assignment) = await _driverService.AssignDriverAsync(
            recyclerId.Value, pickupId, dto.DriverId, cancellationToken);
        if (!success)
        {
            return StatusCode(statusCode, new { message });
        }

        return Ok(assignment);
    }

    /// GET /api/drivers/pickups/{pickupId} — See which driver is on a pickup (pickup owner or recycler)
    [HttpGet("pickups/{pickupId:guid}")]
    [ProducesResponseType(typeof(PickupDriverDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPickupDriver(Guid pickupId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "User ID not found in token." });
        }

        var (success, statusCode, message, assignment) = await _driverService.GetPickupDriverAsync(
            pickupId, userId.Value, cancellationToken);
        if (!success)
        {
            return StatusCode(statusCode, new { message });
        }

        return Ok(assignment);
    }

    private Guid? GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(value, out var id) ? id : null;
    }
}