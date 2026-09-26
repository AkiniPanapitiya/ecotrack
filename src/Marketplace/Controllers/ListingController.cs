using EcoTrack.MarketplaceService.DTOs;
using EcoTrack.MarketplaceService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EcoTrack.MarketplaceService.Controllers;

[ApiController]
[Route("api/listings")]
[Authorize]
[Produces("application/json")]
public class ListingController : ControllerBase
{
    private readonly IListingService _listingService;

    public ListingController(IListingService listingService)
    {
        _listingService = listingService;
    }

    /// POST /api/listings — Create a listing from a valued item (Recycler only)
    [HttpPost]
    [Authorize(Roles = "Recycler")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateListing(
        [FromBody] CreateListingRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var recyclerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(recyclerId))
            return Unauthorized(new { message = "Invalid token." });

        var (success, statusCode, message, listing) = await _listingService.CreateListingAsync(
            dto.ValuationId, dto, recyclerId, cancellationToken);

        if (!success)
            return StatusCode(statusCode, new { message });

        return Ok(new { message, listing });
    }

    /// GET /api/listings — Browse/search available listings ( authenticated users, public browse)
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> BrowseListings(
        [FromQuery] string? keyword = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var (success, statusCode, message, listings, totalCount, returnedPage, returnedPageSize) =
            await _listingService.BrowseListingsAsync(keyword, page, pageSize, cancellationToken);

        // Always returns successfully — empty list is valid
        return Ok(new
        {
            message,
            listings,
            pagination = new
            {
                page = returnedPage,
                pageSize = returnedPageSize,
                totalCount,
                hasMore = returnedPage * returnedPageSize < totalCount
            }
        });
    }

    /// GET /api/listings/{id} — Get listing by ID (authenticated users)
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetListing(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (success, statusCode, message, listing) = await _listingService.GetListingByIdAsync(
            id, cancellationToken);

        if (!success)
            return StatusCode(statusCode, new { message });

        return Ok(new { message, listing });
    }

    /// PUT /api/listings/{id} — Update a listing (Recycler only, owns the listing)
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Recycler")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateListing(
        Guid id,
        [FromBody] UpdateListingRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var recyclerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(recyclerId))
            return Unauthorized(new { message = "Invalid token." });

        // Verify ownership before update
        var existing = await _listingService.GetListingByIdAsync(id, cancellationToken);
        if (!existing.Success)
            return StatusCode(existing.StatusCode, new { message = existing.Message });

        if (existing.Listing!.RecyclerId.ToString() != recyclerId)
            return Forbid();

        var (success, statusCode, message, listing) = await _listingService.UpdateListingAsync(
            id, dto, cancellationToken);

        if (!success)
            return StatusCode(statusCode, new { message });

        return Ok(new { message, listing });
    }

    /// DELETE /api/listings/{id} — Soft-delete a listing (Recycler only, owns the listing)
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Recycler")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteListing(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var recyclerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(recyclerId))
            return Unauthorized(new { message = "Invalid token." });

        // Verify ownership before delete
        var existing = await _listingService.GetListingByIdAsync(id, cancellationToken);
        if (!existing.Success)
            return StatusCode(existing.StatusCode, new { message = existing.Message });

        if (existing.Listing!.RecyclerId.ToString() != recyclerId)
            return Forbid();

        var (success, statusCode, message) = await _listingService.DeleteListingAsync(
            id, cancellationToken);

        if (!success)
            return StatusCode(statusCode, new { message });

        return Ok(new { message });
    }
}
