using EcoTrack.LogisticsService.DTOs;

namespace EcoTrack.LogisticsService.Services;

public interface IDriverService
{
    Task<(bool Success, int StatusCode, string Message, DriverDto? Driver)> AddDriverAsync(
        Guid recyclerId, CreateDriverRequestDto dto, CancellationToken cancellationToken = default);

    Task<IEnumerable<DriverDto>> GetMyDriversAsync(
        Guid recyclerId, CancellationToken cancellationToken = default);

    Task<(bool Success, int StatusCode, string Message, PickupDriverDto? Assignment)> AssignDriverAsync(
        Guid recyclerId, Guid pickupId, Guid driverId, CancellationToken cancellationToken = default);

    Task<(bool Success, int StatusCode, string Message, PickupDriverDto? Assignment)> GetPickupDriverAsync(
        Guid pickupId, Guid requestingUserId, CancellationToken cancellationToken = default);
}