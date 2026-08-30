namespace EcoTrack.IdentityService.DTOs;

public class AuditLogDto
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public DateTime Timestamp { get; set; }
}

public class AuditReportResponseDto
{
    public int TotalEvents { get; set; }
    public int RegistrationCount { get; set; }
    public int SuccessfulLogins { get; set; }
    public int FailedLogins { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public List<AuditLogDto> Logs { get; set; } = new();
}

public class AuditReportRequestDto
{
    public string? Role { get; set; }
    public string? Action { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Limit { get; set; } = 100;
}
