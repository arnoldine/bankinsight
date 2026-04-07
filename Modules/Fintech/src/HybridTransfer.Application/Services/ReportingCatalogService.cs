using HybridTransfer.Application.DTOs;

namespace HybridTransfer.Application.Services;

public sealed class ReportingCatalogService
{
    private static readonly ReportDescriptorResponse[] Reports =
    [
        new("CUSTOMER_STATEMENT", "Customer Statement", "Customer wallet statement export with transaction history and opening/closing balances.", "PDF/CSV/XLSX", "Async"),
        new("RECON_BREAKS", "Reconciliation Exceptions", "Open breaks, aged suspense items, and manual adjustment queue.", "CSV/XLSX", "Async"),
        new("AML_ALERTS", "AML Alert Register", "Open and recently closed transaction monitoring alerts for analysts and auditors.", "CSV/XLSX", "Async"),
        new("LIQUIDITY_POSITION", "Liquidity Position", "Treasury view of safeguarded funds, partner floats, and hot wallet inventory.", "CSV/PDF", "Async")
    ];

    public IReadOnlyCollection<ReportDescriptorResponse> GetAvailableReports() => Reports;
}
