namespace EcoTrack.IdentityService.Models;

public class UserAuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty; // "REGISTER", "LOGIN_SUCCESS", "LOGIN_FAILED", "PROFILE_UPDATE"
    public string Role { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
