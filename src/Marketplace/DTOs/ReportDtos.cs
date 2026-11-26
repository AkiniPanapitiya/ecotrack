namespace EcoTrack.MarketplaceService.DTOs;

/// One row of the Sales, Inventory & Revenue report (Story 21, E6.3).
public class SalesReportCategoryRowDto
{
    public string Category { get; set; } = string.Empty;

    // Sales (orders in the date range that are not Cancelled)
    public int TotalSales { get; set; }
    public decimal SalesValue { get; set; }

    // Revenue (from payment records in the date range)
    public decimal Revenue { get; set; }
    public decimal PendingAmount { get; set; }
    public decimal RefundedAmount { get; set; }

    // Inventory (current listing count by status)
    public int InStock { get; set; }
    public int Reserved { get; set; }
    public int Sold { get; set; }
    public int TotalListings { get; set; }
}

public class SalesReportTotalsDto
{
    public int TotalSales { get; set; }
    public decimal SalesValue { get; set; }
    public decimal Revenue { get; set; }
    public decimal PendingAmount { get; set; }
    public decimal RefundedAmount { get; set; }
    public int InStock { get; set; }
    public int Reserved { get; set; }
    public int Sold { get; set; }
    public int TotalListings { get; set; }
}

public class SalesReportResponseDto
{
    // Filters that were applied (null = not filtered)
    public string? Category { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }

    // "All listings" for Admin, "Your listings" for Recycler
    public string Scope { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }

    public SalesReportTotalsDto Totals { get; set; } = new();
    public List<SalesReportCategoryRowDto> Categories { get; set; } = new();
}

public class SalesReportExportDto
{
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}
