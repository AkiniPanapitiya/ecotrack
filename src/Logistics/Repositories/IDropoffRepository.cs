using EcoTrack.LogisticsService.Models;

namespace EcoTrack.LogisticsService.Repositories;

public interface IDropoffRepository
{
    /// <summary>
    /// Saves the drop-off record and moves the pickup (and its items) to 'Collected'
    /// in one transaction. Returns false if the pickup is no longer 'Scheduled'.
    /// </summary>
    Task<bool> CreateDropoffAsync(DropoffRecord record, CancellationToken cancellationToken = default);

    Task<DropoffRecord?> GetByPickupRequestIdAsync(Guid pickupRequestId, CancellationToken cancellationToken = default);
}
