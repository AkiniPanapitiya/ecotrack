using EcoTrack.MarketplaceService.DTOs;
using EcoTrack.MarketplaceService.Models;
using EcoTrack.MarketplaceService.Repositories;

namespace EcoTrack.MarketplaceService.Services;

/// Escrow & Payment Record (Story 20, E6.2).
/// Record only — no real payment gateway is used.
/// Pending (created with the order) -> Paid (Recycler/Admin) -> Refunded (Recycler/Admin, cancels the order)
public class PaymentService : IPaymentService
{
    private const string AdminRole = "Admin";
    private const string RecyclerRole = "Recycler";

    private readonly IPaymentRepository _paymentRepository;

    public PaymentService(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<(bool Success, int StatusCode, string Message, PaymentRecordResponseDto? Payment)> GetPaymentForOrderAsync(
        Guid orderId, Guid callerId, string callerRole, CancellationToken cancellationToken = default)
    {
        var payment = await _paymentRepository.GetByOrderIdAsync(orderId, cancellationToken);
        if (payment == null)
            return (false, 404, "Payment record not found for this order.", null);

        if (!CanView(payment, callerId, callerRole))
            return (false, 403, "You are not allowed to view this payment record.", null);

        return (true, 200, "Payment record retrieved.", payment);
    }

    public async Task<(bool Success, int StatusCode, string Message, PaymentListResponseDto Payments)> GetPaymentsAsync(
        Guid callerId, string callerRole, string? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 50) pageSize = 50;

        string? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            statusFilter = NormaliseStatus(status);
            if (statusFilter == null)
                return (false, 400, "Status must be Pending, Paid or Refunded.", new PaymentListResponseDto { Page = page, PageSize = pageSize });
        }

        // Admin sees all, Recycler sees their sales, User (buyer) sees their own payments
        Guid? buyerId = null;
        Guid? recyclerId = null;
        if (callerRole == RecyclerRole)
            recyclerId = callerId;
        else if (callerRole != AdminRole)
            buyerId = callerId;

        var payments = await _paymentRepository.GetPagedAsync(buyerId, recyclerId, statusFilter, page, pageSize, cancellationToken);
        var totalCount = await _paymentRepository.GetCountAsync(buyerId, recyclerId, statusFilter, cancellationToken);

        return (
            true,
            200,
            "Payment records retrieved.",
            new PaymentListResponseDto
            {
                Payments = payments,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                HasMore = page * pageSize < totalCount
            }
        );
    }

    public async Task<(bool Success, int StatusCode, string Message, PaymentRecordResponseDto? Payment)> UpdatePaymentStatusAsync(
        Guid paymentId, string? newStatus, Guid callerId, string callerRole, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(newStatus))
            return (false, 400, "Status is required.", null);

        var target = NormaliseStatus(newStatus);
        if (target != PaymentStatus.Paid && target != PaymentStatus.Refunded)
            return (false, 400, "Status can only be changed to Paid or Refunded.", null);

        if (callerRole != AdminRole && callerRole != RecyclerRole)
            return (false, 403, "Only the recycler or an admin can update a payment record.", null);

        var payment = await _paymentRepository.GetByIdAsync(paymentId, cancellationToken);
        if (payment == null)
            return (false, 404, "Payment record not found.", null);

        // A recycler can only update payments for their own listings
        if (callerRole == RecyclerRole && payment.RecyclerId != callerId)
            return (false, 403, "You can only update payments for your own listings.", null);

        if (target == PaymentStatus.Paid)
        {
            if (payment.Status != PaymentStatus.Pending)
                return (false, 409, $"Only a Pending payment can be marked as Paid. Current status: {payment.Status}.", null);

            var updated = await _paymentRepository.MarkAsPaidAsync(paymentId, callerId, cancellationToken);
            if (!updated)
                return (false, 409, "Payment status has changed. Please refresh and try again.", null);
        }
        else
        {
            if (payment.Status != PaymentStatus.Paid)
                return (false, 409, $"Only a Paid payment can be refunded. Current status: {payment.Status}.", null);

            var updated = await _paymentRepository.MarkAsRefundedAsync(paymentId, callerId, cancellationToken);
            if (!updated)
                return (false, 409, "Payment status has changed. Please refresh and try again.", null);
        }

        var result = await _paymentRepository.GetByIdAsync(paymentId, cancellationToken);
        var message = target == PaymentStatus.Paid
            ? "Payment marked as Paid."
            : "Payment refunded and order cancelled.";
        return (true, 200, message, result);
    }

    private static bool CanView(PaymentRecordResponseDto payment, Guid callerId, string callerRole)
    {
        if (callerRole == AdminRole)
            return true;
        if (callerRole == RecyclerRole)
            return payment.RecyclerId == callerId;
        return payment.BuyerId == callerId;
    }

    private static string? NormaliseStatus(string status)
    {
        return PaymentStatus.All.FirstOrDefault(s => string.Equals(s, status.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
