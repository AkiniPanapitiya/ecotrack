using System.Globalization;
using System.Text;
using EcoTrack.MarketplaceService.DTOs;

namespace EcoTrack.MarketplaceService.Services;

/// Builds the CSV and PDF downloads for the Sales, Inventory & Revenue report (Story 21, E6.3).
/// The PDF is written by hand (standard Courier/Helvetica fonts) so no extra NuGet package is needed.
public static class SalesReportExporter
{
    private static readonly string[] Headers =
    {
        "Category", "Total Sales", "Sales Value (LKR)", "Revenue (LKR)", "Pending (LKR)",
        "Refunded (LKR)", "In Stock", "Reserved", "Sold", "Total Listings"
    };

    // ── CSV ────────────────────────────────────────────────────────────────

    public static byte[] ToCsv(SalesReportResponseDto report)
    {
        var sb = new StringBuilder();
        sb.AppendLine(CsvField("EcoTrack Sales, Inventory & Revenue Report"));
        sb.AppendLine(string.Join(",", CsvField("Scope"), CsvField(report.Scope)));
        sb.AppendLine(string.Join(",", CsvField("Category"), CsvField(report.Category ?? "All")));
        sb.AppendLine(string.Join(",", CsvField("From"), CsvField(FormatDate(report.From))));
        sb.AppendLine(string.Join(",", CsvField("To"), CsvField(FormatDate(report.To))));
        sb.AppendLine(string.Join(",", CsvField("Generated (UTC)"), CsvField(report.GeneratedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture))));
        sb.AppendLine();

        sb.AppendLine(string.Join(",", Headers.Select(CsvField)));
        foreach (var row in report.Categories)
            sb.AppendLine(string.Join(",", RowValues(row.Category, row.TotalSales, row.SalesValue, row.Revenue, row.PendingAmount,
                row.RefundedAmount, row.InStock, row.Reserved, row.Sold, row.TotalListings).Select(CsvField)));

        var t = report.Totals;
        sb.AppendLine(string.Join(",", RowValues("TOTAL", t.TotalSales, t.SalesValue, t.Revenue, t.PendingAmount,
            t.RefundedAmount, t.InStock, t.Reserved, t.Sold, t.TotalListings).Select(CsvField)));

        // UTF-8 with BOM so Excel opens it with the right encoding
        var preamble = Encoding.UTF8.GetPreamble();
        var body = Encoding.UTF8.GetBytes(sb.ToString());
        return preamble.Concat(body).ToArray();
    }

    private static string CsvField(string value)
    {
        // Stop spreadsheet apps from treating a value as a formula
        if (value.Length > 0 && "=+-@".Contains(value[0]) && !decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out _))
            value = "'" + value;

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }

    // ── PDF ────────────────────────────────────────────────────────────────

    private const int PageWidth = 842;   // A4 landscape
    private const int PageHeight = 595;
    private const int Margin = 40;
    private const int LineHeight = 14;
    private static readonly int[] ColumnWidths = { 24, 6, 15, 15, 15, 15, 9, 9, 6, 9 };
    private static readonly string[] PdfHeaders =
    {
        "Category", "Sales", "Sales value", "Revenue", "Pending", "Refunded", "In stock", "Reserved", "Sold", "Listings"
    };

    public static byte[] ToPdf(SalesReportResponseDto report)
    {
        var header = new List<(string Font, int Size, string Text)>
        {
            ("F1", 16, "EcoTrack - Sales, Inventory & Revenue Report"),
            ("F2", 10, $"Scope: {report.Scope}    Category: {report.Category ?? "All"}    From: {FormatDate(report.From)}    To: {FormatDate(report.To)}"),
            ("F2", 10, $"Generated: {report.GeneratedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)} UTC"),
            ("F2", 10, "Amounts in LKR. Sales and revenue use the order date. Inventory is the current listing count."),
            ("F3", 9, "")
        };

        var table = new List<(string Font, int Size, string Text)>
        {
            ("F3", 9, TableLine(PdfHeaders)),
            ("F3", 9, new string('-', ColumnWidths.Sum() + ColumnWidths.Length - 1))
        };
        if (report.Categories.Count == 0)
            table.Add(("F3", 9, "No report data for the selected filters."));
        foreach (var row in report.Categories)
            table.Add(("F3", 9, TableLine(RowValues(row.Category, row.TotalSales, row.SalesValue, row.Revenue, row.PendingAmount,
                row.RefundedAmount, row.InStock, row.Reserved, row.Sold, row.TotalListings))));
        table.Add(("F3", 9, new string('-', ColumnWidths.Sum() + ColumnWidths.Length - 1)));
        var t = report.Totals;
        table.Add(("F3", 9, TableLine(RowValues("TOTAL", t.TotalSales, t.SalesValue, t.Revenue, t.PendingAmount,
            t.RefundedAmount, t.InStock, t.Reserved, t.Sold, t.TotalListings))));

        // Split into pages; every page repeats the title line and the table header
        var linesPerPage = (PageHeight - 2 * Margin) / LineHeight;
        var pages = new List<List<(string Font, int Size, string Text)>>();
        var current = new List<(string Font, int Size, string Text)>(header);
        foreach (var line in table)
        {
            if (current.Count >= linesPerPage)
            {
                pages.Add(current);
                current = new List<(string Font, int Size, string Text)> { header[0], ("F3", 9, ""), table[0], table[1] };
            }
            current.Add(line);
        }
        pages.Add(current);

        return BuildPdf(pages);
    }

    private static string TableLine(string[] values)
    {
        var parts = new List<string>();
        for (var i = 0; i < values.Length; i++)
        {
            var width = ColumnWidths[i];
            var value = values[i].Length > width ? values[i][..(width - 1)] + "~" : values[i];
            parts.Add(i == 0 ? value.PadRight(width) : value.PadLeft(width));
        }
        return string.Join(" ", parts);
    }

    private static byte[] BuildPdf(List<List<(string Font, int Size, string Text)>> pages)
    {
        // Objects: 1 Catalog, 2 Pages, 3-5 fonts, then a Page + Content pair per page
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "", // Pages, filled in below
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Courier /Encoding /WinAnsiEncoding >>"
        };

        var pageRefs = new List<string>();
        for (var p = 0; p < pages.Count; p++)
        {
            var content = new StringBuilder();
            var y = PageHeight - Margin;
            foreach (var (font, size, text) in pages[p])
            {
                y -= LineHeight;
                if (text.Length == 0) continue;
                content.Append(CultureInfo.InvariantCulture, $"BT /{font} {size} Tf {Margin} {y} Td ({PdfEscape(text)}) Tj ET\n");
            }
            content.Append(CultureInfo.InvariantCulture, $"BT /F2 8 Tf {Margin} {Margin / 2} Td (Page {p + 1} of {pages.Count}) Tj ET\n");

            var pageObj = objects.Count + 1;
            var contentObj = pageObj + 1;
            pageRefs.Add($"{pageObj} 0 R");
            objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {PageWidth} {PageHeight}] " +
                        $"/Resources << /Font << /F1 3 0 R /F2 4 0 R /F3 5 0 R >> >> /Contents {contentObj} 0 R >>");
            var stream = content.ToString();
            objects.Add($"<< /Length {Encoding.ASCII.GetByteCount(stream)} >>\nstream\n{stream}endstream");
        }
        objects[1] = $"<< /Type /Pages /Kids [{string.Join(" ", pageRefs)}] /Count {pages.Count} >>";

        var output = new StringBuilder("%PDF-1.4\n");
        var offsets = new List<int>();
        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(output.ToString()));
            output.Append(CultureInfo.InvariantCulture, $"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
        }

        var xrefOffset = Encoding.ASCII.GetByteCount(output.ToString());
        output.Append(CultureInfo.InvariantCulture, $"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets)
            output.Append(CultureInfo.InvariantCulture, $"{offset:D10} 00000 n \n");
        output.Append(CultureInfo.InvariantCulture, $"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF\n");

        return Encoding.ASCII.GetBytes(output.ToString());
    }

    private static string PdfEscape(string text)
    {
        var sb = new StringBuilder();
        foreach (var c in text)
        {
            if (c == '\\' || c == '(' || c == ')')
                sb.Append('\\').Append(c);
            else if (c < 32 || c > 126)
                sb.Append('?'); // the standard fonts only cover plain ASCII here
            else
                sb.Append(c);
        }
        return sb.ToString();
    }

    // ── Shared ─────────────────────────────────────────────────────────────

    private static string[] RowValues(string category, int totalSales, decimal salesValue, decimal revenue, decimal pending,
        decimal refunded, int inStock, int reserved, int sold, int totalListings)
    {
        return new[]
        {
            category,
            totalSales.ToString(CultureInfo.InvariantCulture),
            FormatAmount(salesValue),
            FormatAmount(revenue),
            FormatAmount(pending),
            FormatAmount(refunded),
            inStock.ToString(CultureInfo.InvariantCulture),
            reserved.ToString(CultureInfo.InvariantCulture),
            sold.ToString(CultureInfo.InvariantCulture),
            totalListings.ToString(CultureInfo.InvariantCulture)
        };
    }

    private static string FormatAmount(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);

    private static string FormatDate(DateTime? value) =>
        value.HasValue ? value.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : "Any";
}
