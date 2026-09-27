using EcoTrack.LogisticsService.DTOs;

namespace EcoTrack.LogisticsService.Repositories;

public interface IDisposalCertificateRepository
{
    Task<(bool Success, DisposalCertificateDto? Certificate)> CreateCertificateAsync(
        Guid pickupItemId, Guid recyclerId, string recyclerName,
        string disposalMethod, CancellationToken cancellationToken = default);

    Task<DisposalCertificateDto?> GetByPickupItemIdAsync(
        Guid pickupItemId, CancellationToken cancellationToken = default);
}
