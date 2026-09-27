using EcoTrack.LogisticsService.DTOs;

namespace EcoTrack.LogisticsService.Services;

public interface IDisposalCertificateService
{
    Task<(bool Success, int StatusCode, string Message, DisposalCertificateDto? Certificate)> CreateCertificateAsync(
        Guid recyclerId, string recyclerName, Guid pickupItemId,
        string disposalMethod, CancellationToken cancellationToken = default);

    Task<(bool Success, int StatusCode, string Message, ItemWithCertificationDto? Item)> GetItemWithCertificateAsync(
        Guid pickupItemId, Guid requestingUserId, CancellationToken cancellationToken = default);
}
