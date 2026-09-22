using System.Security.Claims;
using EcoTrack.IdentityService.DTOs;
using EcoTrack.IdentityService.Models;
using EcoTrack.IdentityService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcoTrack.IdentityService.Controllers;

[ApiController]
[Route("api/kyc")]
[Produces("application/json")]
public class KycController : ControllerBase
{
    private readonly IRecyclerKycService _kycService;

    public KycController(IRecyclerKycService kycService)
    {
        _kycService = kycService;
    }

    // ---------------------------------------------------------------
    // POST /api/kyc/upload — Recycler only
    // Validate file type/size, save as Pending
    // ---------------------------------------------------------------
    [HttpPost("upload")]
    [Authorize(Roles = "Recycler")]
    [ProducesResponseType(typeof(UploadDocumentResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UploadDocument(
        [FromForm] UploadDocumentRequestDto dto, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "Invalid token." });
        }

        // Save uploaded file to disk (simple file copy — adjust path as needed)
        string? filePath = null;
        if (dto.File != null && dto.File.Length > 0)
        {
            var uploadDir = Path.Combine(AppContext.BaseDirectory, "uploads", "kyc");
            Directory.CreateDirectory(uploadDir);
            var uniqueName = $"{Guid.NewGuid()}_{dto.File.FileName}";
            filePath = Path.Combine(uploadDir, uniqueName);
            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await dto.File.CopyToAsync(stream, cancellationToken);
            }
        }

        var (success, statusCode, message, response) = await _kycService.UploadDocumentAsync(
            userId.Value, dto, filePath, cancellationToken);

        return StatusCode(statusCode, new { message, response });
    }

    // ---------------------------------------------------------------
    // GET /api/kyc/my-status — Recycler only
    // Get my own verification status
    // ---------------------------------------------------------------
    [HttpGet("my-status")]
    [Authorize(Roles = "Recycler")]
    [ProducesResponseType(typeof(MyVerificationStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyVerificationStatus(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "Invalid token." });
        }

        var (success, statusCode, message, response) = await _kycService.GetMyVerificationStatusAsync(
            userId.Value, cancellationToken);

        return StatusCode(statusCode, new { message, response });
    }

    // ---------------------------------------------------------------
    // GET /api/kyc/pending — Admin only
    // List all pending KYC submissions
    // ---------------------------------------------------------------
    [HttpGet("pending")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(List<PendingSubmissionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingSubmissions(CancellationToken cancellationToken)
    {
        var (success, statusCode, message, submissions) = await _kycService.GetPendingSubmissionsAsync(cancellationToken);
        return StatusCode(statusCode, new { message, submissions });
    }

    // ---------------------------------------------------------------
    // PUT /api/kyc/review/{documentId} — Admin only
    // Verify or Reject with review note. Reject must include re-upload reason.
    // ---------------------------------------------------------------
    [HttpPut("review/{documentId:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(RecyclerDocument), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ReviewDocument(
        Guid documentId, [FromBody] AdminReviewRequestDto dto, CancellationToken cancellationToken)
    {
        var adminId = GetCurrentUserId();
        if (adminId == null)
        {
            return Unauthorized(new { message = "Invalid token." });
        }

        var (success, statusCode, message, document) = await _kycService.ReviewDocumentAsync(
            documentId, dto, adminId.Value, cancellationToken);

        return StatusCode(statusCode, new { message, document });
    }

    private Guid? GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var guid) ? guid : null;
    }
}
