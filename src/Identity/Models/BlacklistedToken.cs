namespace EcoTrack.IdentityService.Models;

public class BlacklistedToken
{
    public Guid Id { get; set; }
    public string Jti { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}