using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using BankInsight.API.DTOs;
using BankInsight.API.Services;
using FluentAssertions;
using Xunit;

namespace BankInsight.IntegrationTests.Controllers;

public class ReportExportServiceTests
{
    private readonly ReportExportService _service = new();

    [Fact]
    public void Export_Csv_Xlsx_And_Pdf_Returns_Content()
    {
        var report = new ReportExecutionResponseDTO
        {
            ReportCode = "CRB-CONSUMER-CREDIT-EXTRACT",
            ReportName = "Consumer Credit Extract",
            Category = "Credit Bureau Reports",
            SubCategory = "BoG Extracts",
            GeneratedAt = System.DateTime.UtcNow,
            Columns = new List<string> { "customerId", "currentBalance" },
            Rows = new List<Dictionary<string, object?>>
            {
                new() { ["customerId"] = "CUST001", ["currentBalance"] = 1250.55m },
                new() { ["customerId"] = "CUST002", ["currentBalance"] = 950.00m },
            },
            AppliedFilters = new Dictionary<string, string> { ["branchId"] = "BR001" },
            Summary = new List<ReportSummaryMetricDTO> { new() { Label = "Rows", Value = "2" } },
        };

        var csv = _service.Export(report, "csv", "tester", "BankInsight Demo Bank");
        var xlsx = _service.Export(report, "xlsx", "tester", "BankInsight Demo Bank");
        var pdf = _service.Export(report, "pdf", "tester", "BankInsight Demo Bank");

        Encoding.UTF8.GetString(csv.Content).Should().Contain("Consumer Credit Extract");
        csv.FileName.Should().EndWith(".csv");
        csv.ContentType.Should().Be("text/csv");

        xlsx.FileName.Should().EndWith(".xlsx");
        xlsx.ContentType.Should().Contain("spreadsheetml");
        xlsx.Content.Take(2).Should().Equal(new byte[] { 0x50, 0x4B });
        using (var archive = new ZipArchive(new MemoryStream(xlsx.Content), ZipArchiveMode.Read))
        {
            archive.GetEntry("xl/worksheets/sheet1.xml").Should().NotBeNull();
            archive.GetEntry("xl/styles.xml").Should().NotBeNull();
        }

        pdf.FileName.Should().EndWith(".pdf");
        pdf.ContentType.Should().Be("application/pdf");
        Encoding.ASCII.GetString(pdf.Content.Take(8).ToArray()).Should().Contain("%PDF");
        Encoding.ASCII.GetString(pdf.Content).Should().Contain("Consumer Credit Extract");
        Encoding.ASCII.GetString(pdf.Content).Should().Contain("BankInsight Demo Bank");
    }
}
