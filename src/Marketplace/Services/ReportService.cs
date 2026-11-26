using EcoTrack.MarketplaceService.DTOs;
using EcoTrack.MarketplaceService.Repositories;

namespace EcoTrack.MarketplaceService.Services;

/// Sales, Inventory & Revenue Report (Story 21, E6.3).
/// Admin sees every listing, a Recycler sees only their own listings.
/// Filters: category and an inclusive date range (by order date). Export: CSV or PDF.
public class ReportService : IReportService
{
    private const string AdminRole = "Admin";
    private const string RecyclerRole = "Recycler";

    private readonly IReportRepository _reportRepository;

    public ReportService(IReportRepository reportRepository)
    {
        _reportRepository = reportRepository;
    }

    public async Task<(bool Success, int StatusCode, string Message, SalesReportResponseDto? Report)> GetSalesReportAsync(
        Guid callerId, string callerRole, string? category, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        if (callerRole != AdminRole && callerRole != RecyclerRole)
            return (false, 403, "Only a recycler or an admin can view the sales report.", null);

        fromDate = fromDate?.Date;
        toDate = toDate?.Date;
        if (fromDate.HasValue && toDate.HasValue && fromDate > toDate)
            return (false, 400, "The 'from' date must be on or before the 'to' date.", null);

        var categoryFilter = NormaliseCategory(category);
        Guid? recyclerId = callerRole == RecyclerRole ? callerId : null;

        // The 'to' date is inclusive, so query up to the start of the next day
        var rows = await _reportRepository.GetSalesByCategoryAsync(
            recyclerId, categoryFilter, fromDate, toDate?.AddDays(1), cancellationToken);

        var report = new SalesReportResponseDto
        {
            Category = categoryFilter,
            From = fromDate,
            To = toDate,
            Scope = recyclerId.HasValue ? "Your listings" : "All listings",
            GeneratedAt = DateTime.UtcNow,
            Categories = rows,
            Totals = new SalesReportTotalsDto
            {
                TotalSales = rows.Sum(r => r.TotalSales),
                SalesValue = rows.Sum(r => r.SalesValue),
                Revenue = rows.Sum(r => r.Revenue),
                PendingAmount = rows.Sum(r => r.PendingAmount),
                RefundedAmount = rows.Sum(r => r.RefundedAmount),
                InStock = rows.Sum(r => r.InStock),
                Reserved = rows.Sum(r => r.Reserved),
                Sold = rows.Sum(r => r.Sold),
                TotalListings = rows.Sum(r => r.TotalListings)
            }
        };

        var message = rows.Count == 0 ? "No report data for the selected filters." : "Sales report generated.";
        return (true, 200, message, report);
    }

    public async Task<(bool Success, int StatusCode, string Message, List<string> Categories)> GetCategoriesAsync(
        Guid callerId, string callerRole, CancellationToken cancellationToken = default)
    {
        if (callerRole != AdminRole && callerRole != RecyclerRole)
            return (false, 403, "Only a recycler or an admin can view the sales report.", new List<string>());

        Guid? recyclerId = callerRole == RecyclerRole ? callerId : null;
        var categories = await _reportRepository.GetCategoriesAsync(recyclerId, cancellationToken);
        return (true, 200, "Report categories retrieved.", categories);
    }

    public async Task<(bool Success, int StatusCode, string Message, SalesReportExportDto? File)> ExportSalesReportAsync(
        Guid callerId, string callerRole, string? format, string? category, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        var exportFormat = string.IsNullOrWhiteSpace(format) ? "csv" : format.Trim().ToLowerInvariant();
        if (exportFormat != "csv" && exportFormat != "pdf")
            return (false, 400, "Format must be csv or pdf.", null);

        var (success, statusCode, message, report) = await GetSalesReportAsync(
            callerId, callerRole, category, fromDate, toDate, cancellationToken);
        if (!success || report == null)
            return (false, statusCode, message, null);

        var fileName = $"sales-report-{report.GeneratedAt:yyyyMMdd-HHmm}.{exportFormat}";
        var file = exportFormat == "pdf"
            ? new SalesReportExportDto { Content = SalesReportExporter.ToPdf(report), ContentType = "application/pdf", FileName = fileName }
            : new SalesReportExportDto { Content = SalesReportExporter.ToCsv(report), ContentType = "text/csv", FileName = fileName };

        return (true, 200, "Sales report exported.", file);
    }

    private static string? NormaliseCategory(string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return null;
        var trimmed = category.Trim();
        return string.Equals(trimmed, "All", StringComparison.OrdinalIgnoreCase) ? null : trimmed;
    }
}
