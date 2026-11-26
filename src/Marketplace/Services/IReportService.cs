using EcoTrack.MarketplaceService.DTOs;

namespace EcoTrack.MarketplaceService.Services;

public interface IReportService
{
    Task<(bool Success, int StatusCode, string Message, SalesReportResponseDto? Report)> GetSalesReportAsync(
        Guid callerId, string callerRole, string? category, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default);

    Task<(bool Success, int StatusCode, string Message, List<string> Categories)> GetCategoriesAsync(
        Guid callerId, string callerRole, CancellationToken cancellationToken = default);

    Task<(bool Success, int StatusCode, string Message, SalesReportExportDto? File)> ExportSalesReportAsync(
        Guid callerId, string callerRole, string? format, string? category, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default);
}
