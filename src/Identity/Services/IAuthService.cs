using EcoTrack.IdentityService.DTOs;

namespace EcoTrack.IdentityService.Services;

public interface IAuthService
{
    Task<(bool Success, int StatusCode, string Message, AuthResponseDto? Response)> RegisterAsync(
        RegisterRequestDto request, string? ipAddress = null, CancellationToken cancellationToken = default);

    Task<(bool Success, int StatusCode, string Message, AuthResponseDto? Response)> LoginAsync(
        LoginRequestDto request, string? ipAddress = null, CancellationToken cancellationToken = default);
}
