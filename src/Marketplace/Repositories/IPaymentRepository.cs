using EcoTrack.MarketplaceService.DTOs;

namespace EcoTrack.MarketplaceService.Repositories;

public interface IPaymentRepository
{
    Task<PaymentRecordResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PaymentRecordResponseDto?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<List<PaymentRecordResponseDto>> GetPagedAsync(Guid? buyerId, Guid? recyclerId, string? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetCountAsync(Guid? buyerId, Guid? recyclerId, string? status, CancellationToken cancellationToken = default);
    Task<bool> MarkAsPaidAsync(Guid id, Guid updatedBy, CancellationToken cancellationToken = default);
    Task<bool> MarkAsRefundedAsync(Guid id, Guid updatedBy, CancellationToken cancellationToken = default);
}
