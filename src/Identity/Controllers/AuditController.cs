using EcoTrack.IdentityService.DTOs;
using EcoTrack.IdentityService.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcoTrack.IdentityService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class AuditController : ControllerBase
{
    private readonly IAuditRepository _auditRepository;

    public AuditController(IAuditRepository auditRepository)
    {
        _auditRepository = auditRepository;
    }

    [HttpGet("report")]
    [ProducesResponseType(typeof(AuditReportResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReport(
        [FromQuery] string? role,
        [FromQuery] string? action,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var request = new AuditReportRequestDto
        {
            Role = role,
            Action = action,
            FromDate = fromDate,
            ToDate = toDate,
            Limit = limit
        };

        var report = await _auditRepository.GetAuditReportAsync(request, cancellationToken);
        return Ok(report);
    }
}