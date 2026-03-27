namespace CoreBanker.Services
{
    public class VaultService : ApiClientBase
    {
        public VaultService(HttpClient httpClient) : base(httpClient) { }

        public async Task<List<BranchVaultDto>> GetVaultsAsync(CancellationToken cancellationToken = default)
        {
            var result = await GetAsync<List<BranchVaultDto>>("/api/Vault", cancellationToken);
            return result ?? new List<BranchVaultDto>();
        }

        public async Task<List<TellerTillSummaryDto>> GetTillsAsync(string? branchId = null, string currency = "GHS", CancellationToken cancellationToken = default)
        {
            var query = $"/api/Vault/tills?currency={Uri.EscapeDataString(currency)}";
            if (!string.IsNullOrWhiteSpace(branchId))
            {
                query += $"&branchId={Uri.EscapeDataString(branchId)}";
            }

            var result = await GetAsync<List<TellerTillSummaryDto>>(query, cancellationToken);
            return result ?? new List<TellerTillSummaryDto>();
        }

        public async Task<TellerTillSummaryDto?> OpenTillAsync(OpenTillRequest request, CancellationToken cancellationToken = default)
        {
            return await PostAsync<OpenTillRequest, TellerTillSummaryDto>("/api/Vault/tills/open", request, cancellationToken);
        }

        public async Task<TellerTillSummaryDto?> AllocateTillCashAsync(TillCashTransferRequest request, CancellationToken cancellationToken = default)
        {
            return await PostAsync<TillCashTransferRequest, TellerTillSummaryDto>("/api/Vault/tills/allocate", request, cancellationToken);
        }

        public async Task<TellerTillSummaryDto?> ReturnTillCashAsync(TillCashTransferRequest request, CancellationToken cancellationToken = default)
        {
            return await PostAsync<TillCashTransferRequest, TellerTillSummaryDto>("/api/Vault/tills/return", request, cancellationToken);
        }

        public async Task<TellerTillSummaryDto?> CloseTillAsync(CloseTillRequest request, CancellationToken cancellationToken = default)
        {
            return await PostAsync<CloseTillRequest, TellerTillSummaryDto>("/api/Vault/tills/close", request, cancellationToken);
        }

        public async Task<BranchVaultDto?> RecordVaultCountAsync(VaultCountRequest request, CancellationToken cancellationToken = default)
        {
            return await PostAsync<VaultCountRequest, BranchVaultDto>("/api/Vault/count", request, cancellationToken);
        }

        public async Task<BranchVaultDto?> ProcessVaultTransactionAsync(VaultTransactionRequest request, CancellationToken cancellationToken = default)
        {
            return await PostAsync<VaultTransactionRequest, BranchVaultDto>("/api/Vault/transaction", request, cancellationToken);
        }
    }

    public class BranchVaultDto
    {
        public string Id { get; set; } = string.Empty;
        public string BranchId { get; set; } = string.Empty;
        public string BranchCode { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string Currency { get; set; } = "GHS";
        public decimal CashOnHand { get; set; }
        public decimal? VaultLimit { get; set; }
        public decimal? MinBalance { get; set; }
        public DateTime? LastCountDate { get; set; }
        public string? LastCountBy { get; set; }
        public string? LastCountByName { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class TellerTillSummaryDto
    {
        public string TellerId { get; set; } = string.Empty;
        public string TellerName { get; set; } = string.Empty;
        public string? BranchId { get; set; }
        public string BranchCode { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string Currency { get; set; } = "GHS";
        public bool IsOpen { get; set; }
        public DateTime? OpenedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal MidDayCashLimit { get; set; }
        public decimal AllocatedFromVault { get; set; }
        public decimal ReturnedToVault { get; set; }
        public decimal CashReceipts { get; set; }
        public decimal CashDispensed { get; set; }
        public decimal CurrentBalance { get; set; }
        public decimal Variance { get; set; }
        public string Status { get; set; } = "CLOSED";
        public string? LastAction { get; set; }
        public DateTime? LastActionAt { get; set; }
        public string? Notes { get; set; }
    }

    public class CashDenominationLineDto
    {
        public string Denomination { get; set; } = string.Empty;
        public int Pieces { get; set; }
        public int FitPieces { get; set; }
        public int UnfitPieces { get; set; }
        public int SuspectPieces { get; set; }
        public decimal TotalValue { get; set; }
        public decimal SuspectValue { get; set; }
    }

    public class OpenTillRequest
    {
        public string TellerId { get; set; } = string.Empty;
        public string? BranchId { get; set; }
        public string Currency { get; set; } = "GHS";
        public decimal OpeningBalance { get; set; }
        public decimal? MidDayCashLimit { get; set; }
        public string? Notes { get; set; }
        public string? WitnessOfficer { get; set; }
    }

    public class TillCashTransferRequest
    {
        public string TellerId { get; set; } = string.Empty;
        public string? BranchId { get; set; }
        public string Currency { get; set; } = "GHS";
        public decimal Amount { get; set; }
        public string? Reference { get; set; }
        public string? Narration { get; set; }
        public string? ControlReference { get; set; }
        public string? WitnessOfficer { get; set; }
        public string? SealNumber { get; set; }
        public List<CashDenominationLineDto> Denominations { get; set; } = new();
    }

    public class CloseTillRequest
    {
        public string TellerId { get; set; } = string.Empty;
        public string? BranchId { get; set; }
        public string Currency { get; set; } = "GHS";
        public decimal PhysicalCashCount { get; set; }
        public string? Notes { get; set; }
        public string? ControlReference { get; set; }
        public string? WitnessOfficer { get; set; }
        public string? SealNumber { get; set; }
        public List<CashDenominationLineDto> Denominations { get; set; } = new();
    }

    public class VaultCountRequest
    {
        public string BranchId { get; set; } = string.Empty;
        public string Currency { get; set; } = "GHS";
        public decimal Amount { get; set; }
        public string? ControlReference { get; set; }
        public string? CountReason { get; set; }
        public string? WitnessOfficer { get; set; }
        public string? SealNumber { get; set; }
        public List<CashDenominationLineDto> Denominations { get; set; } = new();
    }

    public class VaultTransactionRequest
    {
        public string BranchId { get; set; } = string.Empty;
        public string Currency { get; set; } = "GHS";
        public decimal Amount { get; set; }
        public string Type { get; set; } = "Deposit";
        public string? Reference { get; set; }
        public string? Narration { get; set; }
        public string? ControlReference { get; set; }
        public string? WitnessOfficer { get; set; }
        public string? SealNumber { get; set; }
        public List<CashDenominationLineDto> Denominations { get; set; } = new();
    }
}
