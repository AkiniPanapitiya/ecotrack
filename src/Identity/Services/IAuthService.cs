using EcoTrack.IdentityService.DTOs;

namespace EcoTrack.IdentityService.Services;

public interface IAuthService
{
    Task<(bool Success, int StatusCode, string Message, AuthResponseDto? Response)> RegisterAsync(
        RegisterRequestDto request, string? ipAddress = null, CancellationToken cancellationToken = default);

    Task<(bool Success, int StatusCode, string Message, AuthResponseDto? Response)> LoginAsync(
        LoginRequestDto request, string? ipAddress = null, CancellationToken cancellationToken = default);

    Task<(bool Success, int StatusCode, string Message)> LogoutAsync(
    string jti, Guid userId, DateTime tokenExpiresAt, CancellationToken cancellationToken = default);

    Task<(bool Success, int StatusCode, string Message)> ForgotPasswordAsync(
    ForgotPasswordRequestDto request, CancellationToken cancellationToken = default);

    Task<(bool Success, int StatusCode, string Message)> ResetPasswordAsync(
    ResetPasswordRequestDto request, CancellationToken cancellationToken = default);
}
