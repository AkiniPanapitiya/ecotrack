using EcoTrack.MarketplaceService.DTOs;

namespace EcoTrack.MarketplaceService.Services;

public interface IPaymentService
{
    Task<(bool Success, int StatusCode, string Message, PaymentRecordResponseDto? Payment)> GetPaymentForOrderAsync(
        Guid orderId, Guid callerId, string callerRole, CancellationToken cancellationToken = default);

    Task<(bool Success, int StatusCode, string Message, PaymentListResponseDto Payments)> GetPaymentsAsync(
        Guid callerId, string callerRole, string? status, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<(bool Success, int StatusCode, string Message, PaymentRecordResponseDto? Payment)> UpdatePaymentStatusAsync(
        Guid paymentId, string? newStatus, Guid callerId, string callerRole, CancellationToken cancellationToken = default);
}
