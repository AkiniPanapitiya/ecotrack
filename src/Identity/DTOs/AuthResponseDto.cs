namespace EcoTrack.IdentityService.DTOs;

public class AuthResponseDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Token { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? VerificationStatus { get; set; }
    public string Message { get; set; } = string.Empty;
}
