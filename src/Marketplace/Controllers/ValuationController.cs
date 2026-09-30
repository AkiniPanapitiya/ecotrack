using EcoTrack.MarketplaceService.DTOs;
using EcoTrack.MarketplaceService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EcoTrack.MarketplaceService.Controllers;

[ApiController]
[Route("api/valuations")]
[Authorize]
[Produces("application/json")]
public class ValuationController : ControllerBase
{
    private readonly IValuationService _valuationService;

    public ValuationController(IValuationService valuationService)
    {
        _valuationService = valuationService;
    }

    /// GET /api/valuations — Get all valuations for current recycler
    [HttpGet]
    [Authorize(Roles = "Recycler")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllValuations(CancellationToken cancellationToken = default)
    {
        var recyclerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(recyclerId))
            return Unauthorized(new { message = "Invalid token." });

        var valuations = await _valuationService.GetValuationsByRecyclerAsync(recyclerId, cancellationToken);
        return Ok(valuations);
    }

    /// POST /api/valuations — Create a valuation (Recycler only)
    [HttpPost]
    [Authorize(Roles = "Recycler")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateValuation(
        [FromBody] CreateValuationRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var recyclerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(recyclerId))
            return Unauthorized(new { message = "Invalid token." });

        var (success, statusCode, message, valuation) = await _valuationService.CreateValuationAsync(
            dto.PickupItemId, dto, recyclerId, cancellationToken);

        if (!success)
            return StatusCode(statusCode, new { message });

        return Ok(new { message, valuation });
    }

    /// PUT /api/valuations/{pickupItemId} — Update a valuation (Recycler only)
    [HttpPut("{pickupItemId:guid}")]
    [Authorize(Roles = "Recycler")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateValuation(
        Guid pickupItemId,
        [FromBody] UpdateValuationRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var (success, statusCode, message, valuation) = await _valuationService.UpdateValuationAsync(
            pickupItemId, dto, cancellationToken);

        if (!success)
            return StatusCode(statusCode, new { message });

        return Ok(new { message, valuation });
    }

    /// GET /api/valuations/{pickupItemId} — Get valuation by pickup item
    [HttpGet("{pickupItemId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetValuation(
        Guid pickupItemId,
        CancellationToken cancellationToken = default)
    {
        var (success, statusCode, message, valuation) = await _valuationService.GetValuationByPickupItemAsync(
            pickupItemId, cancellationToken);

        if (!success)
            return StatusCode(statusCode, new { message });

        return Ok(new { message, valuation });
    }
}
