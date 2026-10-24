using EcoTrack.LogisticsService.DTOs;

namespace EcoTrack.LogisticsService.Services;

public interface IDropoffService
{
    Task<(bool Success, int StatusCode, string Message, DropoffRecordDto? Record)> RecordDropoffAsync(
        Guid pickupId, Guid requestingRecyclerId, CreateDropoffRecordRequestDto dto,
        CancellationToken cancellationToken = default);

    Task<(bool Success, int StatusCode, string Message, DropoffRecordDto? Record)> GetDropoffAsync(
        Guid pickupId, Guid requestingUserId, CancellationToken cancellationToken = default);
}
