using System.Security.Claims;
using EcoTrack.LogisticsService.DTOs;
using EcoTrack.LogisticsService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcoTrack.LogisticsService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class DisposalController : ControllerBase
{
    private readonly IDisposalCertificateService _certService;

    public DisposalController(IDisposalCertificateService certService)
    {
        _certService = certService;
    }

    /// POST /api/disposal/certificate — Mark item as Disposed and create certification (Recycler only)
    [HttpPost("certificate")]
    [Authorize(Roles = "Recycler")]
    [ProducesResponseType(typeof(DisposalCertificateDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateCertificate(
        [FromBody] CreateDisposalCertificateRequestDto dto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var recyclerId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var recyclerName = User.FindFirst("FullName")?.Value
                            ?? User.FindFirst(ClaimTypes.Name)?.Value
                            ?? "Recycler";

        var (success, statusCode, message, certificate) = await _certService.CreateCertificateAsync(
            recyclerId, recyclerName, dto.PickupItemId, dto.DisposalMethod, cancellationToken);

        if (!success)
            return StatusCode(statusCode, new { message });

        return StatusCode(StatusCodes.Status201Created, certificate);
    }

    /// GET /api/disposal/certificate/{pickupItemId} — View certification for an item
    /// Allowed: original item owner (User) or issuing recycler
    [HttpGet("certificate/{pickupItemId:guid}")]
    [ProducesResponseType(typeof(ItemWithCertificationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCertificate(
        Guid pickupItemId,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var requestingUserId = Guid.TryParse(userIdClaim, out var parsed) ? parsed : Guid.Empty;

        var (success, statusCode, message, item) = await _certService.GetItemWithCertificateAsync(
            pickupItemId, requestingUserId, cancellationToken);

        if (!success)
            return StatusCode(statusCode, new { message });

        return Ok(item);
    }
}
