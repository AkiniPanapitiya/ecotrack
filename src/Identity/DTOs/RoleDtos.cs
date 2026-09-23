using EcoTrack.IdentityService.Models;

namespace EcoTrack.IdentityService.DTOs;

public class UserListDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ChangeRoleRequestDto
{
    public string NewRole { get; set; } = string.Empty;
}

public class ChangeRoleResponseDto
{
    public Guid UserId { get; set; }
    public string PreviousRole { get; set; } = string.Empty;
    public string NewRole { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
