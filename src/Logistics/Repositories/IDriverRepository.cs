using EcoTrack.LogisticsService.Models;

namespace EcoTrack.LogisticsService.Repositories;

public interface IDriverRepository
{
    Task<bool> CreateAsync(Driver driver, CancellationToken cancellationToken = default);
    Task<IEnumerable<Driver>> GetActiveByRecyclerIdAsync(Guid recyclerId, CancellationToken cancellationToken = default);
    Task<Driver?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> AssignToPickupAsync(Guid pickupId, Guid driverId, CancellationToken cancellationToken = default);
    Task<Driver?> GetAssignedDriverAsync(Guid pickupId, CancellationToken cancellationToken = default);
}