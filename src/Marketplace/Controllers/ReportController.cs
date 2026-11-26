using EcoTrack.MarketplaceService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EcoTrack.MarketplaceService.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
[Produces("application/json")]
public class ReportController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportController(IReportService reportService)
    {
        _reportService = reportService;
    }

    /// GET /api/reports/sales — Sales, inventory and revenue by category (Admin: all listings, Recycler: own listings)
    /// Optional filters: category, from, to (yyyy-MM-dd, inclusive, by order date)
    [HttpGet("sales")]
    [Authorize(Roles = "Recycler,Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetSalesReport(
        [FromQuery] string? category = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var (callerId, role) = GetCaller();
        if (callerId == null)
            return Unauthorized(new { message = "Invalid token." });

        var (success, statusCode, message, report) = await _reportService.GetSalesReportAsync(
            callerId.Value, role, category, from, to, cancellationToken);

        if (!success)
            return StatusCode(statusCode, new { message });

        return Ok(new { message, report });
    }

    /// GET /api/reports/sales/categories — Categories available for the report filter
    [HttpGet("sales/categories")]
    [Authorize(Roles = "Recycler,Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken = default)
    {
        var (callerId, role) = GetCaller();
        if (callerId == null)
            return Unauthorized(new { message = "Invalid token." });

        var (success, statusCode, message, categories) = await _reportService.GetCategoriesAsync(
            callerId.Value, role, cancellationToken);

        if (!success)
            return StatusCode(statusCode, new { message });

        return Ok(new { message, categories });
    }

    /// GET /api/reports/sales/export?format=csv|pdf — Download the report with the same filters
    [HttpGet("sales/export")]
    [Authorize(Roles = "Recycler,Admin")]
    [Produces("text/csv", "application/pdf", "application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportSalesReport(
        [FromQuery] string? format = "csv",
        [FromQuery] string? category = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var (callerId, role) = GetCaller();
        if (callerId == null)
            return Unauthorized(new { message = "Invalid token." });

        var (success, statusCode, message, file) = await _reportService.ExportSalesReportAsync(
            callerId.Value, role, format, category, from, to, cancellationToken);

        if (!success || file == null)
            return StatusCode(statusCode, new { message });

        return File(file.Content, file.ContentType, file.FileName);
    }

    private (Guid? CallerId, string Role) GetCaller()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        var role = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role") ?? string.Empty;
        return Guid.TryParse(id, out var callerId) ? (callerId, role) : (null, role);
    }
}
