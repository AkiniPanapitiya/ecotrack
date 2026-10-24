using EcoTrack.MarketplaceService.DTOs;
using EcoTrack.MarketplaceService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EcoTrack.MarketplaceService.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
[Produces("application/json")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    /// GET /api/payments — List payment records (Admin: all, Recycler: own sales, User: own purchases)
    [HttpGet]
    [Authorize(Roles = "User,Recycler,Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPayments(
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var (callerId, role) = GetCaller();
        if (callerId == null)
            return Unauthorized(new { message = "Invalid token." });

        var (success, statusCode, message, result) = await _paymentService.GetPaymentsAsync(
            callerId.Value, role, status, page, pageSize, cancellationToken);

        if (!success)
            return StatusCode(statusCode, new { message });

        return Ok(new { message, payments = result.Payments, totalCount = result.TotalCount, page = result.Page, pageSize = result.PageSize, hasMore = result.HasMore });
    }

    /// GET /api/payments/order/{orderId} — Get the payment record linked to an order
    [HttpGet("order/{orderId:guid}")]
    [Authorize(Roles = "User,Recycler,Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPaymentForOrder(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var (callerId, role) = GetCaller();
        if (callerId == null)
            return Unauthorized(new { message = "Invalid token." });

        var (success, statusCode, message, payment) = await _paymentService.GetPaymentForOrderAsync(
            orderId, callerId.Value, role, cancellationToken);

        if (!success)
            return StatusCode(statusCode, new { message });

        return Ok(new { message, payment });
    }

    /// PATCH /api/payments/{id}/status — Mark a payment as Paid or Refunded (Recycler who sold the item, or Admin)
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Recycler,Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdatePaymentStatus(
        Guid id,
        [FromBody] UpdatePaymentStatusRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var (callerId, role) = GetCaller();
        if (callerId == null)
            return Unauthorized(new { message = "Invalid token." });

        var (success, statusCode, message, payment) = await _paymentService.UpdatePaymentStatusAsync(
            id, dto.Status, callerId.Value, role, cancellationToken);

        if (!success)
            return StatusCode(statusCode, new { message });

        return Ok(new { message, payment });
    }

    private (Guid? CallerId, string Role) GetCaller()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        var role = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role") ?? string.Empty;
        return Guid.TryParse(id, out var callerId) ? (callerId, role) : (null, role);
    }
}
