using EcoTrack.MarketplaceService.DTOs;
using EcoTrack.MarketplaceService.Repositories;
using EcoTrack.MarketplaceService.Services;
using Moq;
using Xunit;

namespace EcoTrack.MarketplaceService.Tests;

/// Story 20 — Escrow & Payment Record (E6.2)
public class PaymentServiceTests
{
    private readonly Mock<IPaymentRepository> _paymentRepoMock;
    private readonly PaymentService _paymentService;
    private readonly CancellationToken _ct = CancellationToken.None;

    private readonly Guid _buyerId = Guid.NewGuid();
    private readonly Guid _recyclerId = Guid.NewGuid();
    private readonly Guid _adminId = Guid.NewGuid();

    public PaymentServiceTests()
    {
        _paymentRepoMock = new Mock<IPaymentRepository>();
        _paymentService = new PaymentService(_paymentRepoMock.Object);
    }

    private PaymentRecordResponseDto MakePayment(string status = "Pending", string orderStatus = "Placed")
    {
        return new PaymentRecordResponseDto
        {
            Id = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            BuyerId = _buyerId,
            RecyclerId = _recyclerId,
            Amount = 15000m,
            Status = status,
            PaidAt = status == "Pending" ? null : DateTime.UtcNow,
            RefundedAt = status == "Refunded" ? DateTime.UtcNow : null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            OrderStatus = orderStatus,
            ListingTitle = "Test Laptop"
        };
    }

    private static PaymentRecordResponseDto WithStatus(PaymentRecordResponseDto p, string status, string orderStatus)
    {
        return new PaymentRecordResponseDto
        {
            Id = p.Id,
            OrderId = p.OrderId,
            BuyerId = p.BuyerId,
            RecyclerId = p.RecyclerId,
            Amount = p.Amount,
            Status = status,
            PaidAt = p.PaidAt ?? DateTime.UtcNow,
            RefundedAt = status == "Refunded" ? DateTime.UtcNow : null,
            CreatedAt = p.CreatedAt,
            UpdatedAt = DateTime.UtcNow,
            OrderStatus = orderStatus,
            ListingTitle = p.ListingTitle
        };
    }

    // ── Scenario 1: Create Payment Record ──────────────────────────────────

    [Fact]
    public async Task GetPaymentForOrder_NewlyPlacedOrder_ReturnsPendingRecord()
    {
        var payment = MakePayment("Pending");
        _paymentRepoMock.Setup(r => r.GetByOrderIdAsync(payment.OrderId, _ct)).ReturnsAsync(payment);

        var (success, statusCode, message, result) = await _paymentService.GetPaymentForOrderAsync(
            payment.OrderId, _buyerId, "User", _ct);

        Assert.True(success);
        Assert.Equal(200, statusCode);
        Assert.Equal("Payment record retrieved.", message);
        Assert.NotNull(result);
        Assert.Equal("Pending", result.Status);
        Assert.Equal(payment.OrderId, result.OrderId);
        Assert.Equal(15000m, result.Amount);
        Assert.Null(result.PaidAt);
    }

    [Fact]
    public async Task GetPaymentForOrder_NoRecord_Returns404()
    {
        var orderId = Guid.NewGuid();
        _paymentRepoMock.Setup(r => r.GetByOrderIdAsync(orderId, _ct)).ReturnsAsync((PaymentRecordResponseDto?)null);

        var (success, statusCode, message, result) = await _paymentService.GetPaymentForOrderAsync(
            orderId, _buyerId, "User", _ct);

        Assert.False(success);
        Assert.Equal(404, statusCode);
        Assert.Equal("Payment record not found for this order.", message);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPaymentForOrder_OtherBuyer_Returns403()
    {
        var payment = MakePayment();
        _paymentRepoMock.Setup(r => r.GetByOrderIdAsync(payment.OrderId, _ct)).ReturnsAsync(payment);

        var (success, statusCode, _, result) = await _paymentService.GetPaymentForOrderAsync(
            payment.OrderId, Guid.NewGuid(), "User", _ct);

        Assert.False(success);
        Assert.Equal(403, statusCode);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPaymentForOrder_OtherRecycler_Returns403()
    {
        var payment = MakePayment();
        _paymentRepoMock.Setup(r => r.GetByOrderIdAsync(payment.OrderId, _ct)).ReturnsAsync(payment);

        var (success, statusCode, _, _) = await _paymentService.GetPaymentForOrderAsync(
            payment.OrderId, Guid.NewGuid(), "Recycler", _ct);

        Assert.False(success);
        Assert.Equal(403, statusCode);
    }

    [Theory]
    [InlineData("Recycler")]
    [InlineData("Admin")]
    public async Task GetPaymentForOrder_SellerOrAdmin_Returns200(string role)
    {
        var payment = MakePayment();
        _paymentRepoMock.Setup(r => r.GetByOrderIdAsync(payment.OrderId, _ct)).ReturnsAsync(payment);
        var callerId = role == "Recycler" ? _recyclerId : _adminId;

        var (success, statusCode, _, result) = await _paymentService.GetPaymentForOrderAsync(
            payment.OrderId, callerId, role, _ct);

        Assert.True(success);
        Assert.Equal(200, statusCode);
        Assert.NotNull(result);
    }

    // ── Scenario 2: Mark as Paid ───────────────────────────────────────────

    [Theory]
    [InlineData("Recycler")]
    [InlineData("Admin")]
    public async Task UpdateStatus_PendingToPaid_UpdatesStatusToPaid(string role)
    {
        var payment = MakePayment("Pending");
        var callerId = role == "Recycler" ? _recyclerId : _adminId;
        _paymentRepoMock.SetupSequence(r => r.GetByIdAsync(payment.Id, _ct))
            .ReturnsAsync(payment)
            .ReturnsAsync(WithStatus(payment, "Paid", "Placed"));
        _paymentRepoMock.Setup(r => r.MarkAsPaidAsync(payment.Id, callerId, _ct)).ReturnsAsync(true);

        var (success, statusCode, message, result) = await _paymentService.UpdatePaymentStatusAsync(
            payment.Id, "Paid", callerId, role, _ct);

        Assert.True(success);
        Assert.Equal(200, statusCode);
        Assert.Equal("Payment marked as Paid.", message);
        Assert.NotNull(result);
        Assert.Equal("Paid", result.Status);
        Assert.NotNull(result.PaidAt);
        _paymentRepoMock.Verify(r => r.MarkAsPaidAsync(payment.Id, callerId, _ct), Times.Once);
        _paymentRepoMock.Verify(r => r.MarkAsRefundedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), _ct), Times.Never);
    }

    [Fact]
    public async Task UpdateStatus_StatusIsCaseInsensitive()
    {
        var payment = MakePayment("Pending");
        _paymentRepoMock.Setup(r => r.GetByIdAsync(payment.Id, _ct)).ReturnsAsync(payment);
        _paymentRepoMock.Setup(r => r.MarkAsPaidAsync(payment.Id, _adminId, _ct)).ReturnsAsync(true);

        var (success, statusCode, _, _) = await _paymentService.UpdatePaymentStatusAsync(
            payment.Id, " paid ", _adminId, "Admin", _ct);

        Assert.True(success);
        Assert.Equal(200, statusCode);
        _paymentRepoMock.Verify(r => r.MarkAsPaidAsync(payment.Id, _adminId, _ct), Times.Once);
    }

    [Theory]
    [InlineData("Paid")]
    [InlineData("Refunded")]
    public async Task UpdateStatus_MarkPaidWhenNotPending_Returns409(string currentStatus)
    {
        var payment = MakePayment(currentStatus);
        _paymentRepoMock.Setup(r => r.GetByIdAsync(payment.Id, _ct)).ReturnsAsync(payment);

        var (success, statusCode, message, result) = await _paymentService.UpdatePaymentStatusAsync(
            payment.Id, "Paid", _adminId, "Admin", _ct);

        Assert.False(success);
        Assert.Equal(409, statusCode);
        Assert.Contains("Only a Pending payment can be marked as Paid", message);
        Assert.Null(result);
        _paymentRepoMock.Verify(r => r.MarkAsPaidAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), _ct), Times.Never);
    }

    [Fact]
    public async Task UpdateStatus_MarkPaidConcurrentChange_Returns409()
    {
        var payment = MakePayment("Pending");
        _paymentRepoMock.Setup(r => r.GetByIdAsync(payment.Id, _ct)).ReturnsAsync(payment);
        _paymentRepoMock.Setup(r => r.MarkAsPaidAsync(payment.Id, _adminId, _ct)).ReturnsAsync(false);

        var (success, statusCode, _, _) = await _paymentService.UpdatePaymentStatusAsync(
            payment.Id, "Paid", _adminId, "Admin", _ct);

        Assert.False(success);
        Assert.Equal(409, statusCode);
    }

    [Fact]
    public async Task UpdateStatus_RecyclerNotSeller_Returns403AndNoUpdate()
    {
        var payment = MakePayment("Pending");
        _paymentRepoMock.Setup(r => r.GetByIdAsync(payment.Id, _ct)).ReturnsAsync(payment);
        var otherRecycler = Guid.NewGuid();

        var (success, statusCode, message, _) = await _paymentService.UpdatePaymentStatusAsync(
            payment.Id, "Paid", otherRecycler, "Recycler", _ct);

        Assert.False(success);
        Assert.Equal(403, statusCode);
        Assert.Equal("You can only update payments for your own listings.", message);
        _paymentRepoMock.Verify(r => r.MarkAsPaidAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), _ct), Times.Never);
    }

    [Fact]
    public async Task UpdateStatus_BuyerRole_Returns403AndNoLookup()
    {
        var (success, statusCode, _, _) = await _paymentService.UpdatePaymentStatusAsync(
            Guid.NewGuid(), "Paid", _buyerId, "User", _ct);

        Assert.False(success);
        Assert.Equal(403, statusCode);
        _paymentRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), _ct), Times.Never);
    }

    [Fact]
    public async Task UpdateStatus_PaymentNotFound_Returns404()
    {
        var id = Guid.NewGuid();
        _paymentRepoMock.Setup(r => r.GetByIdAsync(id, _ct)).ReturnsAsync((PaymentRecordResponseDto?)null);

        var (success, statusCode, message, _) = await _paymentService.UpdatePaymentStatusAsync(
            id, "Paid", _adminId, "Admin", _ct);

        Assert.False(success);
        Assert.Equal(404, statusCode);
        Assert.Equal("Payment record not found.", message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateStatus_MissingStatus_Returns400(string? status)
    {
        var (success, statusCode, message, _) = await _paymentService.UpdatePaymentStatusAsync(
            Guid.NewGuid(), status, _adminId, "Admin", _ct);

        Assert.False(success);
        Assert.Equal(400, statusCode);
        Assert.Equal("Status is required.", message);
    }

    [Theory]
    [InlineData("Pending")]
    [InlineData("Captured")]
    [InlineData("Cancelled")]
    public async Task UpdateStatus_InvalidTargetStatus_Returns400(string status)
    {
        var (success, statusCode, message, _) = await _paymentService.UpdatePaymentStatusAsync(
            Guid.NewGuid(), status, _adminId, "Admin", _ct);

        Assert.False(success);
        Assert.Equal(400, statusCode);
        Assert.Equal("Status can only be changed to Paid or Refunded.", message);
        _paymentRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), _ct), Times.Never);
    }

    // ── Scenario 3: Refund a Payment ───────────────────────────────────────

    [Theory]
    [InlineData("Recycler")]
    [InlineData("Admin")]
    public async Task UpdateStatus_PaidToRefunded_UpdatesStatusAndCancelsOrder(string role)
    {
        var payment = MakePayment("Paid");
        var callerId = role == "Recycler" ? _recyclerId : _adminId;
        _paymentRepoMock.SetupSequence(r => r.GetByIdAsync(payment.Id, _ct))
            .ReturnsAsync(payment)
            .ReturnsAsync(WithStatus(payment, "Refunded", "Cancelled"));
        _paymentRepoMock.Setup(r => r.MarkAsRefundedAsync(payment.Id, callerId, _ct)).ReturnsAsync(true);

        var (success, statusCode, message, result) = await _paymentService.UpdatePaymentStatusAsync(
            payment.Id, "Refunded", callerId, role, _ct);

        Assert.True(success);
        Assert.Equal(200, statusCode);
        Assert.Equal("Payment refunded and order cancelled.", message);
        Assert.NotNull(result);
        Assert.Equal("Refunded", result.Status);
        Assert.Equal("Cancelled", result.OrderStatus);
        Assert.NotNull(result.RefundedAt);
        _paymentRepoMock.Verify(r => r.MarkAsRefundedAsync(payment.Id, callerId, _ct), Times.Once);
        _paymentRepoMock.Verify(r => r.MarkAsPaidAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), _ct), Times.Never);
    }

    [Theory]
    [InlineData("Pending")]
    [InlineData("Refunded")]
    public async Task UpdateStatus_RefundWhenNotPaid_Returns409AndOrderNotCancelled(string currentStatus)
    {
        var payment = MakePayment(currentStatus);
        _paymentRepoMock.Setup(r => r.GetByIdAsync(payment.Id, _ct)).ReturnsAsync(payment);

        var (success, statusCode, message, result) = await _paymentService.UpdatePaymentStatusAsync(
            payment.Id, "Refunded", _adminId, "Admin", _ct);

        Assert.False(success);
        Assert.Equal(409, statusCode);
        Assert.Contains("Only a Paid payment can be refunded", message);
        Assert.Null(result);
        _paymentRepoMock.Verify(r => r.MarkAsRefundedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), _ct), Times.Never);
    }

    [Fact]
    public async Task UpdateStatus_RefundConcurrentChange_Returns409()
    {
        var payment = MakePayment("Paid");
        _paymentRepoMock.Setup(r => r.GetByIdAsync(payment.Id, _ct)).ReturnsAsync(payment);
        _paymentRepoMock.Setup(r => r.MarkAsRefundedAsync(payment.Id, _recyclerId, _ct)).ReturnsAsync(false);

        var (success, statusCode, _, _) = await _paymentService.UpdatePaymentStatusAsync(
            payment.Id, "Refunded", _recyclerId, "Recycler", _ct);

        Assert.False(success);
        Assert.Equal(409, statusCode);
    }

    [Fact]
    public async Task UpdateStatus_RefundByOtherRecycler_Returns403()
    {
        var payment = MakePayment("Paid");
        _paymentRepoMock.Setup(r => r.GetByIdAsync(payment.Id, _ct)).ReturnsAsync(payment);

        var (success, statusCode, _, _) = await _paymentService.UpdatePaymentStatusAsync(
            payment.Id, "Refunded", Guid.NewGuid(), "Recycler", _ct);

        Assert.False(success);
        Assert.Equal(403, statusCode);
        _paymentRepoMock.Verify(r => r.MarkAsRefundedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), _ct), Times.Never);
    }

    // ── GetPaymentsAsync (transaction log) ─────────────────────────────────

    [Fact]
    public async Task GetPayments_Admin_SeesAllRecords()
    {
        var list = new List<PaymentRecordResponseDto> { MakePayment(), MakePayment("Paid") };
        _paymentRepoMock.Setup(r => r.GetPagedAsync(null, null, null, 1, 10, _ct)).ReturnsAsync(list);
        _paymentRepoMock.Setup(r => r.GetCountAsync(null, null, null, _ct)).ReturnsAsync(2);

        var (success, statusCode, _, result) = await _paymentService.GetPaymentsAsync(
            _adminId, "Admin", null, 1, 10, _ct);

        Assert.True(success);
        Assert.Equal(200, statusCode);
        Assert.Equal(2, result.Payments.Count);
        Assert.Equal(2, result.TotalCount);
        Assert.False(result.HasMore);
    }

    [Fact]
    public async Task GetPayments_Recycler_FiltersBySeller()
    {
        _paymentRepoMock.Setup(r => r.GetPagedAsync(null, _recyclerId, null, 1, 10, _ct))
            .ReturnsAsync(new List<PaymentRecordResponseDto> { MakePayment() });
        _paymentRepoMock.Setup(r => r.GetCountAsync(null, _recyclerId, null, _ct)).ReturnsAsync(1);

        var (success, _, _, result) = await _paymentService.GetPaymentsAsync(
            _recyclerId, "Recycler", null, 1, 10, _ct);

        Assert.True(success);
        Assert.Single(result.Payments);
        _paymentRepoMock.Verify(r => r.GetPagedAsync(null, _recyclerId, null, 1, 10, _ct), Times.Once);
    }

    [Fact]
    public async Task GetPayments_Buyer_FiltersByBuyer()
    {
        _paymentRepoMock.Setup(r => r.GetPagedAsync(_buyerId, null, null, 1, 10, _ct))
            .ReturnsAsync(new List<PaymentRecordResponseDto>());
        _paymentRepoMock.Setup(r => r.GetCountAsync(_buyerId, null, null, _ct)).ReturnsAsync(0);

        var (success, _, _, result) = await _paymentService.GetPaymentsAsync(
            _buyerId, "User", null, 1, 10, _ct);

        Assert.True(success);
        Assert.Empty(result.Payments);
        _paymentRepoMock.Verify(r => r.GetPagedAsync(_buyerId, null, null, 1, 10, _ct), Times.Once);
    }

    [Fact]
    public async Task GetPayments_StatusFilter_IsNormalisedAndPassedThrough()
    {
        _paymentRepoMock.Setup(r => r.GetPagedAsync(null, null, "Paid", 1, 10, _ct))
            .ReturnsAsync(new List<PaymentRecordResponseDto> { MakePayment("Paid") });
        _paymentRepoMock.Setup(r => r.GetCountAsync(null, null, "Paid", _ct)).ReturnsAsync(1);

        var (success, _, _, result) = await _paymentService.GetPaymentsAsync(
            _adminId, "Admin", "paid", 1, 10, _ct);

        Assert.True(success);
        Assert.Single(result.Payments);
        _paymentRepoMock.Verify(r => r.GetPagedAsync(null, null, "Paid", 1, 10, _ct), Times.Once);
    }

    [Fact]
    public async Task GetPayments_InvalidStatusFilter_Returns400()
    {
        var (success, statusCode, message, _) = await _paymentService.GetPaymentsAsync(
            _adminId, "Admin", "Captured", 1, 10, _ct);

        Assert.False(success);
        Assert.Equal(400, statusCode);
        Assert.Equal("Status must be Pending, Paid or Refunded.", message);
        _paymentRepoMock.Verify(r => r.GetPagedAsync(It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), _ct), Times.Never);
    }

    [Fact]
    public async Task GetPayments_PaginationIsClamped()
    {
        _paymentRepoMock.Setup(r => r.GetPagedAsync(null, null, null, 1, 50, _ct))
            .ReturnsAsync(new List<PaymentRecordResponseDto>());
        _paymentRepoMock.Setup(r => r.GetCountAsync(null, null, null, _ct)).ReturnsAsync(120);

        var (_, _, _, result) = await _paymentService.GetPaymentsAsync(
            _adminId, "Admin", null, 0, 500, _ct);

        Assert.Equal(1, result.Page);
        Assert.Equal(50, result.PageSize);
        Assert.True(result.HasMore);
    }
}
