using System.Text;
using EcoTrack.MarketplaceService.DTOs;
using EcoTrack.MarketplaceService.Repositories;
using EcoTrack.MarketplaceService.Services;
using Moq;
using Xunit;

namespace EcoTrack.MarketplaceService.Tests;

/// Story 21 — Sales, Inventory & Revenue Report (E6.3)
public class ReportServiceTests
{
    private readonly Mock<IReportRepository> _reportRepoMock;
    private readonly ReportService _reportService;
    private readonly CancellationToken _ct = CancellationToken.None;

    private readonly Guid _recyclerId = Guid.NewGuid();
    private readonly Guid _adminId = Guid.NewGuid();
    private readonly Guid _buyerId = Guid.NewGuid();

    public ReportServiceTests()
    {
        _reportRepoMock = new Mock<IReportRepository>();
        _reportService = new ReportService(_reportRepoMock.Object);
    }

    private static SalesReportCategoryRowDto MakeRow(string category, int sales = 2, decimal salesValue = 30000m,
        decimal revenue = 15000m, decimal pending = 15000m, decimal refunded = 0m, int inStock = 3, int reserved = 2, int sold = 0)
    {
        return new SalesReportCategoryRowDto
        {
            Category = category,
            TotalSales = sales,
            SalesValue = salesValue,
            Revenue = revenue,
            PendingAmount = pending,
            RefundedAmount = refunded,
            InStock = inStock,
            Reserved = reserved,
            Sold = sold,
            TotalListings = inStock + reserved + sold
        };
    }

    private void SetupRows(List<SalesReportCategoryRowDto> rows)
    {
        _reportRepoMock
            .Setup(r => r.GetSalesByCategoryAsync(It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), _ct))
            .ReturnsAsync(rows);
    }

    // ── Scenario 1: View Report ────────────────────────────────────────────

    [Fact]
    public async Task GetSalesReport_Admin_ReturnsAllCategoriesWithTotals()
    {
        SetupRows(new List<SalesReportCategoryRowDto>
        {
            MakeRow("Laptop", sales: 2, salesValue: 90000m, revenue: 45000m, pending: 45000m, inStock: 1, reserved: 2),
            MakeRow("Mobile", sales: 1, salesValue: 28000m, revenue: 0m, pending: 28000m, refunded: 5000m, inStock: 4, reserved: 1)
        });

        var (success, statusCode, message, report) = await _reportService.GetSalesReportAsync(
            _adminId, "Admin", null, null, null, _ct);

        Assert.True(success);
        Assert.Equal(200, statusCode);
        Assert.Equal("Sales report generated.", message);
        Assert.NotNull(report);
        Assert.Equal(2, report.Categories.Count);
        Assert.Equal("All listings", report.Scope);
        Assert.Equal(3, report.Totals.TotalSales);
        Assert.Equal(118000m, report.Totals.SalesValue);
        Assert.Equal(45000m, report.Totals.Revenue);
        Assert.Equal(73000m, report.Totals.PendingAmount);
        Assert.Equal(5000m, report.Totals.RefundedAmount);
        Assert.Equal(5, report.Totals.InStock);
        Assert.Equal(3, report.Totals.Reserved);
        Assert.Equal(8, report.Totals.TotalListings);
    }

    [Fact]
    public async Task GetSalesReport_Admin_DoesNotScopeToRecycler()
    {
        SetupRows(new List<SalesReportCategoryRowDto>());

        await _reportService.GetSalesReportAsync(_adminId, "Admin", null, null, null, _ct);

        _reportRepoMock.Verify(r => r.GetSalesByCategoryAsync(null, null, null, null, _ct), Times.Once);
    }

    [Fact]
    public async Task GetSalesReport_Recycler_ScopesToOwnListings()
    {
        SetupRows(new List<SalesReportCategoryRowDto> { MakeRow("Laptop") });

        var (success, _, _, report) = await _reportService.GetSalesReportAsync(
            _recyclerId, "Recycler", null, null, null, _ct);

        Assert.True(success);
        Assert.Equal("Your listings", report!.Scope);
        _reportRepoMock.Verify(r => r.GetSalesByCategoryAsync(_recyclerId, null, null, null, _ct), Times.Once);
    }

    [Fact]
    public async Task GetSalesReport_Buyer_Returns403()
    {
        var (success, statusCode, message, report) = await _reportService.GetSalesReportAsync(
            _buyerId, "User", null, null, null, _ct);

        Assert.False(success);
        Assert.Equal(403, statusCode);
        Assert.Equal("Only a recycler or an admin can view the sales report.", message);
        Assert.Null(report);
        _reportRepoMock.Verify(r => r.GetSalesByCategoryAsync(It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), _ct), Times.Never);
    }

    [Fact]
    public async Task GetSalesReport_NoData_ReturnsZeroTotalsNotError()
    {
        SetupRows(new List<SalesReportCategoryRowDto>());

        var (success, statusCode, message, report) = await _reportService.GetSalesReportAsync(
            _adminId, "Admin", null, null, null, _ct);

        Assert.True(success);
        Assert.Equal(200, statusCode);
        Assert.Equal("No report data for the selected filters.", message);
        Assert.Empty(report!.Categories);
        Assert.Equal(0, report.Totals.TotalSales);
        Assert.Equal(0m, report.Totals.Revenue);
        Assert.Equal(0, report.Totals.InStock);
    }

    [Fact]
    public async Task GetSalesReport_SetsGeneratedAt()
    {
        SetupRows(new List<SalesReportCategoryRowDto>());
        var before = DateTime.UtcNow;

        var (_, _, _, report) = await _reportService.GetSalesReportAsync(_adminId, "Admin", null, null, null, _ct);

        Assert.InRange(report!.GeneratedAt, before, DateTime.UtcNow);
    }

    // ── Scenario 2: Filter by Category ─────────────────────────────────────

    [Fact]
    public async Task GetSalesReport_CategoryFilter_PassedToRepository()
    {
        SetupRows(new List<SalesReportCategoryRowDto> { MakeRow("Laptop") });

        var (success, _, _, report) = await _reportService.GetSalesReportAsync(
            _adminId, "Admin", "Laptop", null, null, _ct);

        Assert.True(success);
        Assert.Equal("Laptop", report!.Category);
        Assert.Single(report.Categories);
        Assert.Equal("Laptop", report.Categories[0].Category);
        _reportRepoMock.Verify(r => r.GetSalesByCategoryAsync(null, "Laptop", null, null, _ct), Times.Once);
    }

    [Fact]
    public async Task GetSalesReport_CategoryFilter_IsTrimmed()
    {
        SetupRows(new List<SalesReportCategoryRowDto>());

        await _reportService.GetSalesReportAsync(_adminId, "Admin", "  Mobile  ", null, null, _ct);

        _reportRepoMock.Verify(r => r.GetSalesByCategoryAsync(null, "Mobile", null, null, _ct), Times.Once);
    }

    [Theory]
    [InlineData("All")]
    [InlineData("all")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetSalesReport_AllOrBlankCategory_MeansNoFilter(string category)
    {
        SetupRows(new List<SalesReportCategoryRowDto>());

        var (_, _, _, report) = await _reportService.GetSalesReportAsync(_adminId, "Admin", category, null, null, _ct);

        Assert.Null(report!.Category);
        _reportRepoMock.Verify(r => r.GetSalesByCategoryAsync(null, null, null, null, _ct), Times.Once);
    }

    [Fact]
    public async Task GetSalesReport_RecyclerWithCategory_PassesBothFilters()
    {
        SetupRows(new List<SalesReportCategoryRowDto>());

        await _reportService.GetSalesReportAsync(_recyclerId, "Recycler", "Monitor", null, null, _ct);

        _reportRepoMock.Verify(r => r.GetSalesByCategoryAsync(_recyclerId, "Monitor", null, null, _ct), Times.Once);
    }

    // ── Date range filter ──────────────────────────────────────────────────

    [Fact]
    public async Task GetSalesReport_DateRange_ToDateIsInclusive()
    {
        SetupRows(new List<SalesReportCategoryRowDto>());
        var from = new DateTime(2026, 10, 1);
        var to = new DateTime(2026, 10, 15);

        var (success, _, _, report) = await _reportService.GetSalesReportAsync(_adminId, "Admin", null, from, to, _ct);

        Assert.True(success);
        Assert.Equal(from, report!.From);
        Assert.Equal(to, report.To);
        // Query runs up to the start of the day after 'to'
        _reportRepoMock.Verify(r => r.GetSalesByCategoryAsync(null, null, from, new DateTime(2026, 10, 16), _ct), Times.Once);
    }

    [Fact]
    public async Task GetSalesReport_DateRange_TimePartIsIgnored()
    {
        SetupRows(new List<SalesReportCategoryRowDto>());

        await _reportService.GetSalesReportAsync(_adminId, "Admin", null,
            new DateTime(2026, 10, 1, 13, 45, 0), new DateTime(2026, 10, 1, 8, 0, 0), _ct);

        _reportRepoMock.Verify(r => r.GetSalesByCategoryAsync(null, null,
            new DateTime(2026, 10, 1), new DateTime(2026, 10, 2), _ct), Times.Once);
    }

    [Fact]
    public async Task GetSalesReport_OnlyFromDate_LeavesToOpen()
    {
        SetupRows(new List<SalesReportCategoryRowDto>());
        var from = new DateTime(2026, 9, 1);

        await _reportService.GetSalesReportAsync(_adminId, "Admin", null, from, null, _ct);

        _reportRepoMock.Verify(r => r.GetSalesByCategoryAsync(null, null, from, null, _ct), Times.Once);
    }

    [Fact]
    public async Task GetSalesReport_OnlyToDate_LeavesFromOpen()
    {
        SetupRows(new List<SalesReportCategoryRowDto>());

        await _reportService.GetSalesReportAsync(_adminId, "Admin", null, null, new DateTime(2026, 9, 30), _ct);

        _reportRepoMock.Verify(r => r.GetSalesByCategoryAsync(null, null, null, new DateTime(2026, 10, 1), _ct), Times.Once);
    }

    [Fact]
    public async Task GetSalesReport_FromAfterTo_Returns400()
    {
        var (success, statusCode, message, report) = await _reportService.GetSalesReportAsync(
            _adminId, "Admin", null, new DateTime(2026, 10, 10), new DateTime(2026, 10, 1), _ct);

        Assert.False(success);
        Assert.Equal(400, statusCode);
        Assert.Equal("The 'from' date must be on or before the 'to' date.", message);
        Assert.Null(report);
        _reportRepoMock.Verify(r => r.GetSalesByCategoryAsync(It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), _ct), Times.Never);
    }

    [Fact]
    public async Task GetSalesReport_SameFromAndTo_IsAllowed()
    {
        SetupRows(new List<SalesReportCategoryRowDto>());
        var day = new DateTime(2026, 10, 5);

        var (success, statusCode, _, _) = await _reportService.GetSalesReportAsync(_adminId, "Admin", null, day, day, _ct);

        Assert.True(success);
        Assert.Equal(200, statusCode);
    }

    // ── Categories for the filter dropdown ─────────────────────────────────

    [Fact]
    public async Task GetCategories_Admin_ReturnsAllCategories()
    {
        _reportRepoMock.Setup(r => r.GetCategoriesAsync(null, _ct))
            .ReturnsAsync(new List<string> { "Laptop", "Mobile", "Monitor" });

        var (success, statusCode, _, categories) = await _reportService.GetCategoriesAsync(_adminId, "Admin", _ct);

        Assert.True(success);
        Assert.Equal(200, statusCode);
        Assert.Equal(3, categories.Count);
    }

    [Fact]
    public async Task GetCategories_Recycler_ScopesToOwnListings()
    {
        _reportRepoMock.Setup(r => r.GetCategoriesAsync(_recyclerId, _ct))
            .ReturnsAsync(new List<string> { "Laptop" });

        var (success, _, _, categories) = await _reportService.GetCategoriesAsync(_recyclerId, "Recycler", _ct);

        Assert.True(success);
        Assert.Single(categories);
        _reportRepoMock.Verify(r => r.GetCategoriesAsync(_recyclerId, _ct), Times.Once);
    }

    [Fact]
    public async Task GetCategories_Buyer_Returns403()
    {
        var (success, statusCode, _, categories) = await _reportService.GetCategoriesAsync(_buyerId, "User", _ct);

        Assert.False(success);
        Assert.Equal(403, statusCode);
        Assert.Empty(categories);
    }

    // ── Scenario 3: Export Report ──────────────────────────────────────────

    [Fact]
    public async Task Export_Csv_ReturnsCsvFileWithReportData()
    {
        SetupRows(new List<SalesReportCategoryRowDto>
        {
            MakeRow("Laptop", sales: 2, salesValue: 90000m, revenue: 45000m, pending: 45000m, inStock: 1, reserved: 2)
        });

        var (success, statusCode, message, file) = await _reportService.ExportSalesReportAsync(
            _adminId, "Admin", "csv", null, null, null, _ct);

        Assert.True(success);
        Assert.Equal(200, statusCode);
        Assert.Equal("Sales report exported.", message);
        Assert.NotNull(file);
        Assert.Equal("text/csv", file.ContentType);
        Assert.EndsWith(".csv", file.FileName);
        Assert.StartsWith("sales-report-", file.FileName);

        var csv = Encoding.UTF8.GetString(file.Content);
        Assert.Contains("Category,Total Sales,Sales Value (LKR),Revenue (LKR)", csv);
        Assert.Contains("Laptop,2,90000.00,45000.00,45000.00,0.00,1,2,0,3", csv);
        Assert.Contains("TOTAL,2,90000.00,45000.00,45000.00,0.00,1,2,0,3", csv);
    }

    [Fact]
    public async Task Export_DefaultFormat_IsCsv()
    {
        SetupRows(new List<SalesReportCategoryRowDto>());

        var (success, _, _, file) = await _reportService.ExportSalesReportAsync(
            _adminId, "Admin", null, null, null, null, _ct);

        Assert.True(success);
        Assert.Equal("text/csv", file!.ContentType);
    }

    [Fact]
    public async Task Export_Csv_IncludesAppliedFilters()
    {
        SetupRows(new List<SalesReportCategoryRowDto> { MakeRow("Laptop") });

        var (_, _, _, file) = await _reportService.ExportSalesReportAsync(
            _adminId, "Admin", "csv", "Laptop", new DateTime(2026, 10, 1), new DateTime(2026, 10, 31), _ct);

        var csv = Encoding.UTF8.GetString(file!.Content);
        Assert.Contains("Category,Laptop", csv);
        Assert.Contains("From,2026-10-01", csv);
        Assert.Contains("To,2026-10-31", csv);
        _reportRepoMock.Verify(r => r.GetSalesByCategoryAsync(null, "Laptop", new DateTime(2026, 10, 1), new DateTime(2026, 11, 1), _ct), Times.Once);
    }

    [Fact]
    public async Task Export_Csv_EscapesCommasAndQuotes()
    {
        SetupRows(new List<SalesReportCategoryRowDto> { MakeRow("TV, \"Smart\"") });

        var (_, _, _, file) = await _reportService.ExportSalesReportAsync(
            _adminId, "Admin", "csv", null, null, null, _ct);

        var csv = Encoding.UTF8.GetString(file!.Content);
        Assert.Contains("\"TV, \"\"Smart\"\"\",", csv);
    }

    [Fact]
    public async Task Export_Csv_BlocksFormulaInjection()
    {
        SetupRows(new List<SalesReportCategoryRowDto> { MakeRow("=HYPERLINK(1)") });

        var (_, _, _, file) = await _reportService.ExportSalesReportAsync(
            _adminId, "Admin", "csv", null, null, null, _ct);

        var csv = Encoding.UTF8.GetString(file!.Content);
        Assert.Contains("'=HYPERLINK(1)", csv);
    }

    [Fact]
    public async Task Export_Pdf_ReturnsValidPdfFile()
    {
        SetupRows(new List<SalesReportCategoryRowDto>
        {
            MakeRow("Laptop", sales: 2, salesValue: 90000m, revenue: 45000m),
            MakeRow("Mobile (used)", sales: 1, salesValue: 28000m, revenue: 0m)
        });

        var (success, statusCode, _, file) = await _reportService.ExportSalesReportAsync(
            _adminId, "Admin", "PDF", null, null, null, _ct);

        Assert.True(success);
        Assert.Equal(200, statusCode);
        Assert.Equal("application/pdf", file!.ContentType);
        Assert.EndsWith(".pdf", file.FileName);

        var pdf = Encoding.ASCII.GetString(file.Content);
        Assert.StartsWith("%PDF-1.4", pdf);
        Assert.EndsWith("%%EOF\n", pdf);
        Assert.Contains("Sales, Inventory & Revenue Report", pdf);
        Assert.Contains("Laptop", pdf);
        Assert.Contains("90000.00", pdf);
        Assert.Contains("Mobile \\(used\\)", pdf); // brackets are escaped in PDF text
    }

    [Fact]
    public async Task Export_Pdf_XrefOffsetIsCorrect()
    {
        SetupRows(new List<SalesReportCategoryRowDto> { MakeRow("Laptop") });

        var (_, _, _, file) = await _reportService.ExportSalesReportAsync(
            _adminId, "Admin", "pdf", null, null, null, _ct);

        var pdf = Encoding.ASCII.GetString(file!.Content);
        var startXref = pdf.LastIndexOf("startxref\n", StringComparison.Ordinal);
        var offset = int.Parse(pdf.Substring(startXref + 10).Split('\n')[0]);
        Assert.Equal("xref", pdf.Substring(offset, 4));
    }

    [Fact]
    public async Task Export_Pdf_ManyCategories_SplitsIntoPages()
    {
        var rows = Enumerable.Range(1, 80).Select(i => MakeRow($"Category {i:D2}")).ToList();
        SetupRows(rows);

        var (_, _, _, file) = await _reportService.ExportSalesReportAsync(
            _adminId, "Admin", "pdf", null, null, null, _ct);

        var pdf = Encoding.ASCII.GetString(file!.Content);
        Assert.Contains("Page 1 of", pdf);
        Assert.Contains("Page 2 of", pdf);
        Assert.Contains("Category 80", pdf);
    }

    [Fact]
    public async Task Export_Pdf_NoData_StillProducesFile()
    {
        SetupRows(new List<SalesReportCategoryRowDto>());

        var (success, _, _, file) = await _reportService.ExportSalesReportAsync(
            _adminId, "Admin", "pdf", null, null, null, _ct);

        Assert.True(success);
        Assert.Contains("No report data for the selected filters.", Encoding.ASCII.GetString(file!.Content));
    }

    [Fact]
    public async Task Export_InvalidFormat_Returns400()
    {
        var (success, statusCode, message, file) = await _reportService.ExportSalesReportAsync(
            _adminId, "Admin", "xlsx", null, null, null, _ct);

        Assert.False(success);
        Assert.Equal(400, statusCode);
        Assert.Equal("Format must be csv or pdf.", message);
        Assert.Null(file);
    }

    [Fact]
    public async Task Export_Buyer_Returns403()
    {
        var (success, statusCode, _, file) = await _reportService.ExportSalesReportAsync(
            _buyerId, "User", "csv", null, null, null, _ct);

        Assert.False(success);
        Assert.Equal(403, statusCode);
        Assert.Null(file);
    }

    [Fact]
    public async Task Export_FromAfterTo_Returns400()
    {
        var (success, statusCode, _, file) = await _reportService.ExportSalesReportAsync(
            _adminId, "Admin", "csv", null, new DateTime(2026, 10, 10), new DateTime(2026, 10, 1), _ct);

        Assert.False(success);
        Assert.Equal(400, statusCode);
        Assert.Null(file);
    }
}
