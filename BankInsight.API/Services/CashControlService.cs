using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Services;

/*
IMPLEMENTATION GAP REPORT
- Vault management -> PARTIALLY IMPLEMENTED
- Teller tills -> PARTIALLY IMPLEMENTED
- Cash inventory tracking -> PARTIALLY IMPLEMENTED
- Denomination-level balances -> MISSING
- Cash transfers between vault and teller -> IMPLEMENTED
- Branch-to-branch cash transfers -> PARTIALLY IMPLEMENTED
- Cash-in-transit tracking -> PARTIALLY IMPLEMENTED
- Cash counts -> PARTIALLY IMPLEMENTED
- Cash reconciliation -> PARTIALLY IMPLEMENTED
- GL postings for cash movements -> PARTIALLY IMPLEMENTED
- Vault limit monitoring -> PARTIALLY IMPLEMENTED
- Cash shortage / excess handling -> PARTIALLY IMPLEMENTED
- Maker-checker approvals -> PARTIALLY IMPLEMENTED
- Audit logging -> PARTIALLY IMPLEMENTED
- IFRS cash reporting -> MISSING

Current maturity level: MODERATE / PARTIAL.
The system has operational branch-vault and teller-till workflows, and now a custody lifecycle for
inter-branch transfers, but it still lacks denomination inventory, formal cash incidents, and full
branch-dimension GL accounting.
*/
public interface ICashControlService
{
    Task<List<VaultCashPositionDto>> GetVaultCashPositionAsync(string? branchId = null, string currency = "GHS");
    Task<List<BranchCashSummaryDto>> GetBranchCashSummaryAsync(string? branchId = null, string currency = "GHS");
    Task<CashReconciliationSummaryDto> GetCashReconciliationSummaryAsync(string currency = "GHS");
    Task<List<CashTransitItemDto>> GetCashTransitItemsAsync(string currency = "GHS");
}

public class CashControlService : ICashControlService
{
    private const decimal MaterialVarianceThreshold = 0.01m;
    private readonly ApplicationDbContext _context;
    private readonly IVaultManagementService _vaultManagementService;

    public CashControlService(ApplicationDbContext context, IVaultManagementService vaultManagementService)
    {
        _context = context;
        _vaultManagementService = vaultManagementService;
    }

    public async Task<List<VaultCashPositionDto>> GetVaultCashPositionAsync(string? branchId = null, string currency = "GHS")
    {
        var summaries = await BuildBranchCashSummariesAsync(branchId, currency);
        return summaries
            .Select(summary => new VaultCashPositionDto
            {
                BranchId = summary.BranchId,
                BranchCode = summary.BranchCode,
                BranchName = summary.BranchName,
                Currency = summary.Currency,
                VaultCash = summary.VaultCash,
                TillCash = summary.TellerTillCash,
                TotalOperationalCash = summary.TotalOperationalCash,
                GlCashBalance = summary.GlCashBalance,
                GlVariance = summary.ReconciliationVariance,
                MinBalance = summary.VaultCashPosition?.MinBalance ?? 0,
                MaxHoldingLimit = summary.VaultCashPosition?.VaultLimit ?? 0,
                LastCountDate = summary.VaultCashPosition?.LastCountDate,
                Status = summary.ReconciliationStatus,
            })
            .OrderBy(item => item.BranchName)
            .ToList();
    }

    public async Task<List<BranchCashSummaryDto>> GetBranchCashSummaryAsync(string? branchId = null, string currency = "GHS")
    {
        var summaries = await BuildBranchCashSummariesAsync(branchId, currency);
        return summaries
            .OrderBy(item => item.BranchName)
            .Select(item => (BranchCashSummaryDto)item)
            .ToList();
    }

    public async Task<CashReconciliationSummaryDto> GetCashReconciliationSummaryAsync(string currency = "GHS")
    {
        var normalizedCurrency = NormalizeCurrency(currency);
        var summaries = await BuildBranchCashSummariesAsync(null, normalizedCurrency);
        var totalOperationalCash = summaries.Sum(item => item.TotalOperationalCash);
        var totalGlCashBalance = await GetOperationalCashGlBalanceAsync(normalizedCurrency);
        var totalVariance = totalOperationalCash - totalGlCashBalance;
        var alerts = BuildAlerts(summaries, totalVariance, totalGlCashBalance, totalOperationalCash, normalizedCurrency);

        return new CashReconciliationSummaryDto
        {
            Currency = normalizedCurrency,
            GeneratedAt = DateTime.UtcNow,
            TotalVaultCash = summaries.Sum(item => item.VaultCash),
            TotalTillCash = summaries.Sum(item => item.TellerTillCash),
            TotalOperationalCash = totalOperationalCash,
            TotalGlCashBalance = totalGlCashBalance,
            TotalVariance = totalVariance,
            BranchesOutOfBalance = summaries.Count(item => Math.Abs(item.ReconciliationVariance) > MaterialVarianceThreshold),
            TillsOverLimit = summaries.Sum(item => item.TillsOverLimitCount),
            TillsWithVariance = summaries.Sum(item => item.VarianceTillCount),
            StaleCashInTransitItems = summaries.Sum(item => item.StaleTransferCount),
            Branches = summaries.Select(item => (BranchCashSummaryDto)item).ToList(),
            Alerts = alerts,
        };
    }

    public async Task<List<CashTransitItemDto>> GetCashTransitItemsAsync(string currency = "GHS")
    {
        var normalizedCurrency = NormalizeCurrency(currency);
        var transfers = await _context.InterBranchTransfers
            .Include(item => item.FromBranch)
            .Include(item => item.ToBranch)
            .Where(item => item.Currency == normalizedCurrency)
            .OrderByDescending(item => item.CreatedAt)
            .Take(100)
            .ToListAsync();

        return transfers
            .Select(item =>
            {
                var hoursOpen = ((item.CompletedAt ?? DateTime.UtcNow) - item.CreatedAt).TotalHours;
                var isStale = !item.CompletedAt.HasValue && hoursOpen >= 24;
                var transitStage = item.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase)
                    ? "AWAITING_APPROVAL"
                    : item.CompletedAt.HasValue
                        ? "RECEIVED"
                        : "IN_TRANSIT";

                return new CashTransitItemDto
                {
                    TransferId = item.Id,
                    FromBranchId = item.FromBranchId,
                    FromBranchName = item.FromBranch?.Name ?? string.Empty,
                    ToBranchId = item.ToBranchId,
                    ToBranchName = item.ToBranch?.Name ?? string.Empty,
                    Currency = item.Currency,
                    Amount = item.Amount,
                    Status = item.Status,
                    TransitStage = transitStage,
                    Reference = item.Reference,
                    Narration = item.Narration,
                    CreatedAt = item.CreatedAt,
                    ApprovedAt = item.ApprovedAt,
                    CompletedAt = item.CompletedAt,
                    HoursOpen = Math.Round(hoursOpen, 2),
                    IsStale = isStale,
                };
            })
            .ToList();
    }

    private async Task<List<BranchCashSummarySnapshot>> BuildBranchCashSummariesAsync(string? branchId, string currency)
    {
        var normalizedCurrency = NormalizeCurrency(currency);
        var branches = await _context.Branches
            .Where(branch => string.IsNullOrWhiteSpace(branchId) || branch.Id == branchId)
            .OrderBy(branch => branch.Name)
            .ToListAsync();

        var vaults = await _context.BranchVaults
            .Include(vault => vault.Branch)
            .Include(vault => vault.LastCountByStaff)
            .Where(vault => vault.Currency == normalizedCurrency && (string.IsNullOrWhiteSpace(branchId) || vault.BranchId == branchId))
            .ToListAsync();

        var tillSummaries = await _vaultManagementService.GetTillSummariesAsync(branchId, normalizedCurrency);
        var transferItems = await _context.InterBranchTransfers
            .Where(item => item.Currency == normalizedCurrency && (string.IsNullOrWhiteSpace(branchId) || item.FromBranchId == branchId || item.ToBranchId == branchId))
            .ToListAsync();

        var systemGlCashBalance = await GetOperationalCashGlBalanceAsync(normalizedCurrency);
        var useBranchLevelGlComparison = !string.IsNullOrWhiteSpace(branchId) || branches.Count <= 1;
        var summaries = new List<BranchCashSummarySnapshot>();

        foreach (var branch in branches)
        {
            var branchVault = vaults.FirstOrDefault(vault => vault.BranchId == branch.Id);
            var branchTills = tillSummaries.Where(till => till.BranchId == branch.Id).ToList();
            var pendingOutbound = transferItems
                .Where(item => item.FromBranchId == branch.Id && item.Status.Equals("InTransit", StringComparison.OrdinalIgnoreCase))
                .Sum(item => item.Amount);
            var pendingInbound = transferItems
                .Where(item => item.ToBranchId == branch.Id && item.Status.Equals("InTransit", StringComparison.OrdinalIgnoreCase))
                .Sum(item => item.Amount);
            var staleTransfers = transferItems.Count(item =>
                (item.FromBranchId == branch.Id || item.ToBranchId == branch.Id) &&
                item.Status.Equals("InTransit", StringComparison.OrdinalIgnoreCase) &&
                item.CreatedAt <= DateTime.UtcNow.AddHours(-24));

            var vaultCash = branchVault?.CashOnHand ?? 0m;
            var tillCash = branchTills.Where(till => till.IsOpen).Sum(till => till.CurrentBalance);
            var totalOperationalCash = vaultCash + tillCash;
            var branchGlCashBalance = useBranchLevelGlComparison ? systemGlCashBalance : totalOperationalCash;
            var glVariance = useBranchLevelGlComparison ? totalOperationalCash - systemGlCashBalance : 0m;
            var varianceTillCount = branchTills.Count(till => Math.Abs(till.Variance) > MaterialVarianceThreshold);
            var tillsOverLimit = branchTills.Count(till => till.IsOpen && till.CurrentBalance > till.MidDayCashLimit && till.MidDayCashLimit > 0);
            var branchVaultPosition = branchVault == null
                ? null
                : new BranchVaultDto
                {
                    Id = branchVault.Id,
                    BranchId = branchVault.BranchId,
                    BranchCode = branchVault.Branch?.Code ?? branch.Code,
                    BranchName = branchVault.Branch?.Name ?? branch.Name,
                    Currency = branchVault.Currency,
                    CashOnHand = branchVault.CashOnHand,
                    VaultLimit = branchVault.VaultLimit,
                    MinBalance = branchVault.MinBalance,
                    LastCountDate = branchVault.LastCountDate,
                    LastCountBy = branchVault.LastCountBy,
                    LastCountByName = branchVault.LastCountByStaff?.Name,
                    UpdatedAt = branchVault.UpdatedAt,
                };

            summaries.Add(new BranchCashSummarySnapshot
            {
                BranchId = branch.Id,
                BranchCode = branch.Code,
                BranchName = branch.Name,
                Currency = normalizedCurrency,
                VaultCash = vaultCash,
                TellerTillCash = tillCash,
                TotalOperationalCash = totalOperationalCash,
                PendingCashInTransitOut = pendingOutbound,
                PendingCashInTransitIn = pendingInbound,
                OpenTillCount = branchTills.Count(till => till.IsOpen),
                VarianceTillCount = varianceTillCount,
                TillsOverLimitCount = tillsOverLimit,
                StaleTransferCount = staleTransfers,
                GlCashBalance = branchGlCashBalance,
                ReconciliationVariance = glVariance,
                ReconciliationStatus = ResolveStatus(branchVaultPosition, glVariance, varianceTillCount, staleTransfers, branchTills),
                VaultCashPosition = branchVaultPosition,
            });
        }

        return summaries;
    }

    private List<CashControlAlertDto> BuildAlerts(
        IEnumerable<BranchCashSummarySnapshot> summaries,
        decimal totalVariance,
        decimal totalGlCashBalance,
        decimal totalOperationalCash,
        string currency)
    {
        var alerts = new List<CashControlAlertDto>();

        foreach (var summary in summaries)
        {
            if (summary.VaultCashPosition?.VaultLimit is decimal maxHoldingLimit && summary.VaultCash > maxHoldingLimit)
            {
                alerts.Add(CreateAlert("VAULT_LIMIT_BREACH", "HIGH", summary, $"Vault cash exceeds holding limit of {maxHoldingLimit:N2}.", maxHoldingLimit, summary.VaultCash));
            }

            if (summary.VaultCashPosition?.MinBalance is decimal minBalance && summary.VaultCash < minBalance)
            {
                alerts.Add(CreateAlert("VAULT_BELOW_MINIMUM", "MEDIUM", summary, $"Vault cash is below minimum balance of {minBalance:N2}.", minBalance, summary.VaultCash));
            }

            if (Math.Abs(summary.ReconciliationVariance) > MaterialVarianceThreshold)
            {
                alerts.Add(CreateAlert("GL_RECONCILIATION_VARIANCE", "HIGH", summary, "Operational cash does not reconcile to GL cash balance.", summary.GlCashBalance, summary.TotalOperationalCash));
            }

            if (summary.VarianceTillCount > 0)
            {
                alerts.Add(CreateAlert("TILL_VARIANCE", "HIGH", summary, $"{summary.VarianceTillCount} till(s) have unresolved cash variance.", null, null));
            }

            if (summary.TillsOverLimitCount > 0)
            {
                alerts.Add(CreateAlert("TILL_LIMIT_BREACH", "MEDIUM", summary, $"{summary.TillsOverLimitCount} till(s) exceed configured limit.", null, null));
            }

            if (summary.StaleTransferCount > 0)
            {
                alerts.Add(CreateAlert("STALE_CASH_IN_TRANSIT", "HIGH", summary, $"{summary.StaleTransferCount} in-transit transfer(s) remain unresolved for more than 24 hours.", null, null));
            }
        }

        if (Math.Abs(totalVariance) > MaterialVarianceThreshold)
        {
            alerts.Add(new CashControlAlertDto
            {
                AlertType = "SYSTEM_GL_RECONCILIATION_VARIANCE",
                Severity = "HIGH",
                ScopeType = "SYSTEM",
                ScopeId = currency,
                ScopeName = "Enterprise Cash Position",
                Message = "Total operational cash does not reconcile to the GL cash balance.",
                ThresholdAmount = totalGlCashBalance,
                ActualAmount = totalOperationalCash,
                ObservedAt = DateTime.UtcNow,
            });
        }

        return alerts
            .OrderByDescending(alert => alert.Severity)
            .ThenBy(alert => alert.ScopeName)
            .ToList();
    }

    private async Task<decimal> GetOperationalCashGlBalanceAsync(string currency)
    {
        var cashGlAccounts = await _context.GlAccounts
            .Where(account =>
                account.Currency == currency &&
                (
                    account.Code == "10100" ||
                    (account.Category == "ASSET" && EF.Functions.ILike(account.Name, "%cash%"))
                ))
            .ToListAsync();

        return cashGlAccounts.Sum(account => account.Balance);
    }

    private static CashControlAlertDto CreateAlert(
        string alertType,
        string severity,
        BranchCashSummarySnapshot summary,
        string message,
        decimal? thresholdAmount,
        decimal? actualAmount)
    {
        return new CashControlAlertDto
        {
            AlertType = alertType,
            Severity = severity,
            ScopeType = "BRANCH",
            ScopeId = summary.BranchId,
            ScopeName = summary.BranchName,
            Message = message,
            ThresholdAmount = thresholdAmount,
            ActualAmount = actualAmount,
            ObservedAt = DateTime.UtcNow,
        };
    }

    private static string NormalizeCurrency(string? currency)
    {
        return string.IsNullOrWhiteSpace(currency) ? "GHS" : currency.Trim().ToUpperInvariant();
    }

    private static string ResolveStatus(
        BranchVaultDto? vault,
        decimal glVariance,
        int varianceTillCount,
        int staleTransferCount,
        IReadOnlyCollection<TellerTillSummaryDto> tills)
    {
        if (Math.Abs(glVariance) > MaterialVarianceThreshold || varianceTillCount > 0 || staleTransferCount > 0)
        {
            return "EXCEPTION";
        }

        if (vault?.VaultLimit is decimal vaultLimit && vault.CashOnHand > vaultLimit)
        {
            return "LIMIT_BREACH";
        }

        if (vault?.MinBalance is decimal minBalance && vault.CashOnHand < minBalance)
        {
            return "LOW_CASH";
        }

        if (tills.Any(till => till.IsOpen && till.CurrentBalance > till.MidDayCashLimit && till.MidDayCashLimit > 0))
        {
            return "TILL_LIMIT_BREACH";
        }

        return "BALANCED";
    }

    private sealed class BranchCashSummarySnapshot : BranchCashSummaryDto
    {
        public int TillsOverLimitCount { get; set; }
        public BranchVaultDto? VaultCashPosition { get; set; }
    }
}
