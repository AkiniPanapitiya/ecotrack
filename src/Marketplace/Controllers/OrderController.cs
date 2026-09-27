using EcoTrack.MarketplaceService.DTOs;
using EcoTrack.MarketplaceService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EcoTrack.MarketplaceService.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
[Produces("application/json")]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrderController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    /// POST /api/orders — Place an order for an Available listing (Buyer only)
    [HttpPost]
    [Authorize(Roles = "User")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PlaceOrder(
        [FromBody] CreateOrderRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(buyerId))
            return Unauthorized(new { message = "Invalid token." });

        var (success, statusCode, message, order) = await _orderService.PlaceOrderAsync(
            dto.ListingId, Guid.Parse(buyerId), cancellationToken);

        if (!success)
            return StatusCode(statusCode, new { message });

        return Ok(new { message, order });
    }

    /// GET /api/orders — Get current buyer's orders with pagination
    [HttpGet]
    [Authorize(Roles = "User")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(buyerId))
            return Unauthorized(new { message = "Invalid token." });

        var (success, statusCode, message, orders) = await _orderService.GetMyOrdersAsync(
            Guid.Parse(buyerId), page, pageSize, cancellationToken);

        return Ok(new { message, orders });
    }

    /// GET /api/orders/{id} — Get a single order by ID
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "User")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrder(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(buyerId))
            return Unauthorized(new { message = "Invalid token." });

        var all = await _orderService.GetMyOrdersAsync(
            Guid.Parse(buyerId), 1, 100, cancellationToken);

        var found = all.Orders.Orders.FirstOrDefault(o => o.Id == id);
        if (found == null)
            return NotFound(new { message = "Order not found." });

        return Ok(new { message = "Order retrieved.", order = found });
    }
}
