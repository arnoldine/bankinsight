using System.Text.Json;
using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Services;

public interface IVaultManagementService
{
    Task<BranchVaultDto> GetOrCreateVaultAsync(string branchId, string currency);
    Task<BranchVaultDto?> GetVaultAsync(string branchId, string currency);
    Task<List<BranchVaultDto>> GetAllVaultsAsync();
    Task<List<BranchVaultDto>> GetBranchVaultsAsync(string branchId);
    Task<BranchVaultDto> RecordVaultCountAsync(VaultCountRequest request, string countedBy);
    Task<BranchVaultDto> ProcessVaultTransactionAsync(VaultTransactionRequest request, string processedBy);
    Task<List<TellerTillSummaryDto>> GetTillSummariesAsync(string? branchId = null, string currency = "GHS");
    Task<TellerTillSummaryDto> OpenTillAsync(OpenTillRequest request, string processedBy);
    Task<TellerTillSummaryDto> AllocateTillCashAsync(TillCashTransferRequest request, string processedBy);
    Task<TellerTillSummaryDto> ReturnTillCashAsync(TillCashTransferRequest request, string processedBy);
    Task<TellerTillSummaryDto> CloseTillAsync(CloseTillRequest request, string processedBy);
}

public class VaultManagementService : IVaultManagementService
{
    private const decimal DefaultTillLimit = 50000m;
    private const string TillSessionEntityType = "TILL_SESSION";
    private const string TillCashEntityType = "TILL_CASH";
    private const string CashOnHandGlCode = "10100";
    private const string CashOperationsSuspenseGlCode = "22300";

    private readonly ApplicationDbContext _context;
    private readonly IAuditLoggingService _auditLoggingService;
    private readonly ICashIncidentService _cashIncidentService;

    public VaultManagementService(ApplicationDbContext context, IAuditLoggingService auditLoggingService, ICashIncidentService cashIncidentService)
    {
        _context = context;
        _auditLoggingService = auditLoggingService;
        _cashIncidentService = cashIncidentService;
    }

    public async Task<BranchVaultDto> GetOrCreateVaultAsync(string branchId, string currency)
    {
        var vault = await _context.BranchVaults
            .Include(v => v.Branch)
            .FirstOrDefaultAsync(v => v.BranchId == branchId && v.Currency == currency);

        if (vault == null)
        {
            vault = new BranchVault
            {
                Id = $"VLT-{branchId}-{currency}-{Guid.NewGuid().ToString()[..8]}",
                BranchId = branchId,
                Currency = currency,
                CashOnHand = 0,
                UpdatedAt = DateTime.UtcNow,
            };

            _context.BranchVaults.Add(vault);
            await _context.SaveChangesAsync();

            vault = await _context.BranchVaults
                .Include(v => v.Branch)
                .FirstAsync(v => v.Id == vault.Id);
        }

        return MapToDto(vault);
    }

    public async Task<BranchVaultDto?> GetVaultAsync(string branchId, string currency)
    {
        var vault = await _context.BranchVaults
            .Include(v => v.Branch)
            .Include(v => v.LastCountByStaff)
            .FirstOrDefaultAsync(v => v.BranchId == branchId && v.Currency == currency);

        return vault == null ? null : MapToDto(vault);
    }

    public async Task<List<BranchVaultDto>> GetAllVaultsAsync()
    {
        var vaults = await _context.BranchVaults
            .Include(v => v.Branch)
            .Include(v => v.LastCountByStaff)
            .OrderBy(v => v.BranchId)
            .ThenBy(v => v.Currency)
            .ToListAsync();

        return vaults.Select(MapToDto).ToList();
    }

    public async Task<List<BranchVaultDto>> GetBranchVaultsAsync(string branchId)
    {
        var vaults = await _context.BranchVaults
            .Include(v => v.Branch)
            .Include(v => v.LastCountByStaff)
            .Where(v => v.BranchId == branchId)
            .OrderBy(v => v.Currency)
            .ToListAsync();

        return vaults.Select(MapToDto).ToList();
    }

    public async Task<BranchVaultDto> RecordVaultCountAsync(VaultCountRequest request, string countedBy)
    {
        var currency = NormalizeCurrency(request.Currency);
        var vault = await GetOrCreateVaultEntityAsync(request.BranchId, currency);
        ValidateDenominationTotal(request.Denominations, request.Amount, "vault count");

        var previousBalance = vault.CashOnHand;
        var variance = request.Amount - previousBalance;

        vault.CashOnHand = request.Amount;
        vault.LastCountDate = DateTime.UtcNow;
        vault.LastCountBy = countedBy;
        vault.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await RaiseSuspectAndUnfitCashFlagsAsync(
            request.BranchId,
            "VAULT",
            vault.Id,
            currency,
            request.Denominations,
            request.ControlReference,
            BuildCountComplianceNarration("VAULT_COUNT", request.CountReason, request.WitnessOfficer, request.SealNumber));

        if (Math.Abs(variance) > 0.009m)
        {
            await _cashIncidentService.ReportSystemIncidentAsync(
                request.BranchId,
                "VAULT",
                vault.Id,
                variance < 0 ? "SHORTAGE" : "EXCESS",
                currency,
                Math.Abs(variance),
                $"VCOUNT-{DateTime.UtcNow:yyyyMMddHHmmss}",
                $"Vault count recorded with variance of {variance:N2}.",
                countedBy);
        }

        await LogCashOperationAsync("VAULT_COUNT_RECORDED", "BRANCH_VAULT", vault.Id, countedBy, new
        {
            request.BranchId,
            Currency = currency,
            request.Amount,
            Variance = variance,
            request.ControlReference,
            request.CountReason,
            request.WitnessOfficer,
            request.SealNumber,
            DenominationSummary = BuildDenominationComplianceSummary(request.Denominations)
        });

        return await GetVaultAsync(request.BranchId, currency) ?? throw new InvalidOperationException("Vault not found after update");
    }

    public async Task<BranchVaultDto> ProcessVaultTransactionAsync(VaultTransactionRequest request, string processedBy)
    {
        var currency = NormalizeCurrency(request.Currency);
        var vault = await GetOrCreateVaultEntityAsync(request.BranchId, currency);
        ValidateDenominationTotal(request.Denominations, request.Amount, "vault transaction");
        EnsureNoSuspectNotes(request.Denominations, "vault movement");

        if (request.Type.Equals("Withdrawal", StringComparison.OrdinalIgnoreCase))
        {
            if (vault.CashOnHand < request.Amount)
            {
                throw new InvalidOperationException("Insufficient cash in vault");
            }

            vault.CashOnHand -= request.Amount;
        }
        else if (request.Type.Equals("Deposit", StringComparison.OrdinalIgnoreCase))
        {
            vault.CashOnHand += request.Amount;

            if (vault.VaultLimit.HasValue && vault.CashOnHand > vault.VaultLimit.Value)
            {
                throw new InvalidOperationException($"Transaction would exceed vault limit of {vault.VaultLimit.Value:N2}");
            }
        }
        else
        {
            throw new InvalidOperationException("Unsupported vault transaction type");
        }

        var isDeposit = request.Type.Equals("Deposit", StringComparison.OrdinalIgnoreCase);
        var ledger = CreateVaultLedger(
            isDeposit
                ? VaultTransactionType.MainVault_To_BranchVault_Allocation
                : VaultTransactionType.BranchVault_To_MainVault_Return,
            request.BranchId,
            request.Amount,
            currency,
            request.Reference,
            request.Narration ?? $"{request.Type} from cash operations",
            processedBy);
        AttachDenominations(ledger, request.Denominations);
        _context.VaultLedgers.Add(ledger);

        if (ShouldPostVaultControlJournal(request.Reference))
        {
            await AddVaultControlJournalAsync(request.BranchId, currency, request.Amount, request.Reference, request.Narration, processedBy, isDeposit);
        }

        vault.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        await RaiseSuspectAndUnfitCashFlagsAsync(
            request.BranchId,
            "VAULT",
            vault.Id,
            currency,
            request.Denominations,
            request.ControlReference ?? request.Reference,
            BuildCountComplianceNarration("VAULT_TRANSACTION", request.Narration, request.WitnessOfficer, request.SealNumber));

        await LogCashOperationAsync("VAULT_TRANSACTION_PROCESSED", "BRANCH_VAULT", vault.Id, processedBy, new
        {
            request.BranchId,
            Currency = currency,
            request.Amount,
            request.Type,
            request.Reference,
            request.ControlReference,
            request.WitnessOfficer,
            request.SealNumber,
            DenominationSummary = BuildDenominationComplianceSummary(request.Denominations)
        });

        return await GetVaultAsync(request.BranchId, currency) ?? throw new InvalidOperationException("Vault not found after update");
    }

    public async Task<List<TellerTillSummaryDto>> GetTillSummariesAsync(string? branchId = null, string currency = "GHS")
    {
        var normalizedCurrency = NormalizeCurrency(currency);
        var tellers = await GetEligibleTellersAsync(branchId);
        var summaries = new List<TellerTillSummaryDto>();

        foreach (var teller in tellers)
        {
            var resolvedBranchId = string.IsNullOrWhiteSpace(branchId) ? teller.BranchId : branchId;
            if (string.IsNullOrWhiteSpace(resolvedBranchId))
            {
                continue;
            }

            summaries.Add(await BuildTillSummaryAsync(teller, resolvedBranchId!, normalizedCurrency));
        }

        return summaries
            .OrderByDescending(t => t.IsOpen)
            .ThenBy(t => t.BranchName)
            .ThenBy(t => t.TellerName)
            .ToList();
    }

    public async Task<TellerTillSummaryDto> OpenTillAsync(OpenTillRequest request, string processedBy)
    {
        var teller = await GetStaffAsync(request.TellerId);
        var branchId = ResolveBranchId(request.BranchId, teller.BranchId);
        var currency = NormalizeCurrency(request.Currency);
        var current = await BuildTillSummaryAsync(teller, branchId, currency);

        if (current.IsOpen)
        {
            throw new InvalidOperationException("Till is already open for this teller.");
        }

        var openingBalance = request.OpeningBalance;
        if (openingBalance > 0)
        {
            var vault = await GetOrCreateVaultEntityAsync(branchId, currency);
            if (vault.CashOnHand < openingBalance)
            {
                throw new InvalidOperationException("Vault has insufficient cash for the requested till opening balance.");
            }

            vault.CashOnHand -= openingBalance;
            vault.UpdatedAt = DateTime.UtcNow;
            var ledger = CreateVaultLedger(
                VaultTransactionType.BranchVault_To_Teller_Allocation,
                branchId,
                openingBalance,
                currency,
                $"TILL-OPEN-{DateTime.UtcNow.Ticks}",
                BuildTillNarration("OPEN_TILL", teller.Id, branchId, currency, request.Notes),
                processedBy);
            _context.VaultLedgers.Add(ledger);
        }

        _context.UserActivities.Add(new UserActivity
        {
            StaffId = processedBy,
            Action = "OPEN_TILL",
            EntityType = TillSessionEntityType,
            EntityId = BuildTillKey(branchId, teller.Id, currency),
            AfterValue = JsonSerializer.Serialize(new TillSessionPayload
            {
                TellerId = teller.Id,
                BranchId = branchId,
                Currency = currency,
                OpeningBalance = openingBalance,
                MidDayCashLimit = request.MidDayCashLimit ?? DefaultTillLimit,
                Notes = request.Notes,
            }),
            CreatedAt = DateTime.UtcNow,
        });

        await _context.SaveChangesAsync();
        await LogCashOperationAsync("TILL_OPENED", "TILL_SESSION", BuildTillKey(branchId, teller.Id, currency), processedBy, new
        {
            TellerId = teller.Id,
            BranchId = branchId,
            Currency = currency,
            request.OpeningBalance,
            request.MidDayCashLimit,
            request.WitnessOfficer
        });
        return await BuildTillSummaryAsync(teller, branchId, currency);
    }

    public async Task<TellerTillSummaryDto> AllocateTillCashAsync(TillCashTransferRequest request, string processedBy)
    {
        return await MoveCashBetweenVaultAndTillAsync(request, processedBy, true);
    }

    public async Task<TellerTillSummaryDto> ReturnTillCashAsync(TillCashTransferRequest request, string processedBy)
    {
        return await MoveCashBetweenVaultAndTillAsync(request, processedBy, false);
    }

    public async Task<TellerTillSummaryDto> CloseTillAsync(CloseTillRequest request, string processedBy)
    {
        var teller = await GetStaffAsync(request.TellerId);
        var branchId = ResolveBranchId(request.BranchId, teller.BranchId);
        var currency = NormalizeCurrency(request.Currency);
        var current = await BuildTillSummaryAsync(teller, branchId, currency);

        if (!current.IsOpen)
        {
            throw new InvalidOperationException("Till is not open for this teller.");
        }

        var physicalCashCount = request.PhysicalCashCount;
        var variance = physicalCashCount - current.CurrentBalance;
        ValidateDenominationTotal(request.Denominations, physicalCashCount, "till close");

        if (physicalCashCount > 0)
        {
            var vault = await GetOrCreateVaultEntityAsync(branchId, currency);
            vault.CashOnHand += physicalCashCount;
            vault.UpdatedAt = DateTime.UtcNow;

            var ledger = CreateVaultLedger(
                VaultTransactionType.Teller_To_BranchVault_Return,
                branchId,
                physicalCashCount,
                currency,
                $"TILL-CLOSE-{DateTime.UtcNow.Ticks}",
                BuildTillNarration("CLOSE_TILL_RETURN", teller.Id, branchId, currency, request.Notes),
                processedBy);
            AttachDenominations(ledger, request.Denominations);
            _context.VaultLedgers.Add(ledger);
        }

        _context.UserActivities.Add(new UserActivity
        {
            StaffId = processedBy,
            Action = "CLOSE_TILL",
            EntityType = TillSessionEntityType,
            EntityId = BuildTillKey(branchId, teller.Id, currency),
            AfterValue = JsonSerializer.Serialize(new CloseTillPayload
            {
                TellerId = teller.Id,
                BranchId = branchId,
                Currency = currency,
                PhysicalCashCount = physicalCashCount,
                ExpectedBalance = current.CurrentBalance,
                Variance = variance,
                Notes = request.Notes,
            }),
            CreatedAt = DateTime.UtcNow,
        });

        await _context.SaveChangesAsync();
        await RaiseSuspectAndUnfitCashFlagsAsync(
            branchId,
            "TILL",
            BuildTillKey(branchId, teller.Id, currency),
            currency,
            request.Denominations,
            request.ControlReference,
            BuildCountComplianceNarration("CLOSE_TILL", request.Notes, request.WitnessOfficer, request.SealNumber));

        if (Math.Abs(variance) > 0.009m)
        {
            await _cashIncidentService.ReportSystemIncidentAsync(
                branchId,
                "TILL",
                BuildTillKey(branchId, teller.Id, currency),
                variance < 0 ? "SHORTAGE" : "EXCESS",
                currency,
                Math.Abs(variance),
                $"TILL-CLOSE-{DateTime.UtcNow:yyyyMMddHHmmss}",
                $"Till close completed with variance of {variance:N2}.",
                processedBy);
        }

        await LogCashOperationAsync("TILL_CLOSED", "TILL_SESSION", BuildTillKey(branchId, teller.Id, currency), processedBy, new
        {
            TellerId = teller.Id,
            BranchId = branchId,
            Currency = currency,
            request.PhysicalCashCount,
            current.CurrentBalance,
            Variance = variance,
            request.ControlReference,
            request.WitnessOfficer,
            request.SealNumber,
            DenominationSummary = BuildDenominationComplianceSummary(request.Denominations)
        });
        return await BuildTillSummaryAsync(teller, branchId, currency);
    }

    private async Task<TellerTillSummaryDto> MoveCashBetweenVaultAndTillAsync(TillCashTransferRequest request, string processedBy, bool isAllocation)
    {
        var teller = await GetStaffAsync(request.TellerId);
        var branchId = ResolveBranchId(request.BranchId, teller.BranchId);
        var currency = NormalizeCurrency(request.Currency);
        var current = await BuildTillSummaryAsync(teller, branchId, currency);
        ValidateDenominationTotal(request.Denominations, request.Amount, isAllocation ? "till allocation" : "till return");
        EnsureNoSuspectNotes(request.Denominations, isAllocation ? "till allocation" : "till return");

        if (!current.IsOpen)
        {
            throw new InvalidOperationException("Open the till before processing till cash movements.");
        }

        var vault = await GetOrCreateVaultEntityAsync(branchId, currency);
        if (isAllocation)
        {
            if (vault.CashOnHand < request.Amount)
            {
                throw new InvalidOperationException("Vault has insufficient cash for this allocation.");
            }

            vault.CashOnHand -= request.Amount;
        }
        else
        {
            vault.CashOnHand += request.Amount;
        }

        vault.UpdatedAt = DateTime.UtcNow;

        var ledger = CreateVaultLedger(
            isAllocation ? VaultTransactionType.BranchVault_To_Teller_Allocation : VaultTransactionType.Teller_To_BranchVault_Return,
            branchId,
            request.Amount,
            currency,
            request.Reference,
            BuildTillNarration(isAllocation ? "ALLOCATE_TILL_CASH" : "RETURN_TILL_CASH", teller.Id, branchId, currency, request.Narration),
            processedBy);
        AttachDenominations(ledger, request.Denominations);
        _context.VaultLedgers.Add(ledger);

        _context.UserActivities.Add(new UserActivity
        {
            StaffId = processedBy,
            Action = isAllocation ? "ALLOCATE_TILL_CASH" : "RETURN_TILL_CASH",
            EntityType = TillCashEntityType,
            EntityId = BuildTillKey(branchId, teller.Id, currency),
            AfterValue = JsonSerializer.Serialize(new TillCashMovementPayload
            {
                TellerId = teller.Id,
                BranchId = branchId,
                Currency = currency,
                Amount = request.Amount,
                Reference = request.Reference,
                Narration = request.Narration,
            }),
            CreatedAt = DateTime.UtcNow,
        });

        await _context.SaveChangesAsync();
        await RaiseSuspectAndUnfitCashFlagsAsync(
            branchId,
            "TILL",
            BuildTillKey(branchId, teller.Id, currency),
            currency,
            request.Denominations,
            request.ControlReference ?? request.Reference,
            BuildCountComplianceNarration(isAllocation ? "ALLOCATE_TILL_CASH" : "RETURN_TILL_CASH", request.Narration, request.WitnessOfficer, request.SealNumber));

        await LogCashOperationAsync("TILL_CASH_MOVEMENT", "TILL_CASH", BuildTillKey(branchId, teller.Id, currency), processedBy, new
        {
            TellerId = teller.Id,
            BranchId = branchId,
            Currency = currency,
            request.Amount,
            request.Reference,
            request.ControlReference,
            request.WitnessOfficer,
            request.SealNumber,
            Direction = isAllocation ? "VAULT_TO_TILL" : "TILL_TO_VAULT",
            DenominationSummary = BuildDenominationComplianceSummary(request.Denominations)
        });
        return await BuildTillSummaryAsync(teller, branchId, currency);
    }

    private async Task<TellerTillSummaryDto> BuildTillSummaryAsync(Staff teller, string branchId, string currency)
    {
        var normalizedCurrency = NormalizeCurrency(currency);
        var key = BuildTillKey(branchId, teller.Id, normalizedCurrency);
        var activities = await _context.UserActivities
            .Where(a => a.EntityId == key && (a.EntityType == TillSessionEntityType || a.EntityType == TillCashEntityType))
            .OrderBy(a => a.CreatedAt)
            .ToListAsync();

        var openActivity = activities.LastOrDefault(a => a.Action == "OPEN_TILL");
        var closeActivity = activities.LastOrDefault(a => a.Action == "CLOSE_TILL");
        var isOpen = openActivity != null && (closeActivity == null || openActivity.CreatedAt > closeActivity.CreatedAt);
        var sessionStart = isOpen ? openActivity!.CreatedAt : (DateTime?)null;
        var openingPayload = openActivity != null ? DeserializePayload<TillSessionPayload>(openActivity.AfterValue) : null;
        var closePayload = closeActivity != null ? DeserializePayload<CloseTillPayload>(closeActivity.AfterValue) : null;
        var midDayCashLimit = openingPayload?.MidDayCashLimit ?? DefaultTillLimit;
        var openingBalance = isOpen ? openingPayload?.OpeningBalance ?? 0 : 0;

        decimal allocations = 0;
        decimal returns = 0;
        decimal receipts = 0;
        decimal dispensed = 0;
        decimal currentBalance = 0;
        decimal variance = closePayload?.Variance ?? 0;

        if (sessionStart.HasValue)
        {
            allocations = SumMovementAmounts(activities, "ALLOCATE_TILL_CASH", sessionStart.Value);
            returns = SumMovementAmounts(activities, "RETURN_TILL_CASH", sessionStart.Value);

            receipts = await _context.Transactions
                .Where(t => t.TellerId == teller.Id && t.Status == "POSTED" && t.Type == "DEPOSIT" && t.Date >= sessionStart.Value)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;

            dispensed = await _context.Transactions
                .Where(t => t.TellerId == teller.Id && t.Status == "POSTED" && (t.Type == "WITHDRAWAL" || t.Type == "TRANSFER") && t.Date >= sessionStart.Value)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;

            currentBalance = openingBalance + allocations + receipts - dispensed - returns;
        }

        var branch = teller.Branch ?? await _context.Branches.FirstOrDefaultAsync(b => b.Id == branchId);
        var latestAction = activities.LastOrDefault();

        return new TellerTillSummaryDto
        {
            TellerId = teller.Id,
            TellerName = teller.Name,
            BranchId = branchId,
            BranchCode = branch?.Code ?? string.Empty,
            BranchName = branch?.Name ?? string.Empty,
            Currency = normalizedCurrency,
            IsOpen = isOpen,
            OpenedAt = sessionStart,
            ClosedAt = !isOpen ? closeActivity?.CreatedAt : null,
            OpeningBalance = openingBalance,
            MidDayCashLimit = midDayCashLimit,
            AllocatedFromVault = allocations,
            ReturnedToVault = returns,
            CashReceipts = receipts,
            CashDispensed = dispensed,
            CurrentBalance = isOpen ? currentBalance : 0,
            Variance = isOpen ? 0 : variance,
            Status = isOpen
                ? (currentBalance > midDayCashLimit ? "OVER_LIMIT" : "OPEN")
                : (Math.Abs(variance) > 0.009m ? "CLOSED_WITH_VARIANCE" : "CLOSED"),
            LastAction = latestAction?.Action,
            LastActionAt = latestAction?.CreatedAt,
            Notes = isOpen ? openingPayload?.Notes : closePayload?.Notes,
        };
    }

    private static decimal SumMovementAmounts(IEnumerable<UserActivity> activities, string action, DateTime since)
    {
        return activities
            .Where(a => a.Action == action && a.CreatedAt >= since)
            .Select(a => DeserializePayload<TillCashMovementPayload>(a.AfterValue)?.Amount ?? 0)
            .Sum();
    }

    private async Task<List<Staff>> GetEligibleTellersAsync(string? branchId)
    {
        var tellers = await _context.Staff
            .Include(s => s.Branch)
            .Where(s => s.Status == "Active" && (string.IsNullOrWhiteSpace(branchId) || s.BranchId == branchId))
            .OrderBy(s => s.Name)
            .ToListAsync();

        var tellerRoleIds = await _context.UserRoles
            .Include(ur => ur.Role)
            .Where(ur => ur.Role != null && ur.Role.Name.ToLower().Contains("teller"))
            .Select(ur => ur.UserId)
            .Distinct()
            .ToListAsync();

        var transactingTellers = await _context.Transactions
            .Where(t => t.TellerId != null)
            .Select(t => t.TellerId!)
            .Distinct()
            .ToListAsync();

        var preferredIds = tellerRoleIds.Union(transactingTellers).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (preferredIds.Count > 0)
        {
            var filtered = tellers.Where(t => preferredIds.Contains(t.Id)).ToList();
            if (filtered.Count > 0)
            {
                return filtered;
            }
        }

        return tellers;
    }

    private async Task<Staff> GetStaffAsync(string tellerId)
    {
        var teller = await _context.Staff
            .Include(s => s.Branch)
            .FirstOrDefaultAsync(s => s.Id == tellerId && s.Status == "Active");

        if (teller == null)
        {
            throw new InvalidOperationException("Teller not found or inactive.");
        }

        return teller;
    }

    private async Task<BranchVault> GetOrCreateVaultEntityAsync(string branchId, string currency)
    {
        var normalizedCurrency = NormalizeCurrency(currency);
        var vault = await _context.BranchVaults.FirstOrDefaultAsync(v => v.BranchId == branchId && v.Currency == normalizedCurrency);
        if (vault != null)
        {
            return vault;
        }

        vault = new BranchVault
        {
            Id = $"VLT-{branchId}-{normalizedCurrency}-{Guid.NewGuid().ToString()[..8]}",
            BranchId = branchId,
            Currency = normalizedCurrency,
            CashOnHand = 0,
            UpdatedAt = DateTime.UtcNow,
        };

        _context.BranchVaults.Add(vault);
        await _context.SaveChangesAsync();
        return vault;
    }


    private async Task AddVaultControlJournalAsync(
        string branchId,
        string currency,
        decimal amount,
        string? reference,
        string? narration,
        string processedBy,
        bool isDeposit)
    {
        var cashOnHand = await _context.GlAccounts.FirstOrDefaultAsync(account => account.Code == CashOnHandGlCode && account.Currency == currency);
        var suspense = await _context.GlAccounts.FirstOrDefaultAsync(account => account.Code == CashOperationsSuspenseGlCode && account.Currency == currency);

        if (cashOnHand == null || suspense == null)
        {
            throw new InvalidOperationException("Required GL control accounts for vault cash posting are missing.");
        }

        var journalId = $"JRN-VLT-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        var description = narration ?? (isDeposit ? $"Vault cash deposit for branch {branchId}" : $"Vault cash withdrawal for branch {branchId}");
        var lines = isDeposit
            ? new[]
            {
                new JournalLine { JournalId = journalId, AccountCode = CashOnHandGlCode, Debit = amount, Credit = 0m },
                new JournalLine { JournalId = journalId, AccountCode = CashOperationsSuspenseGlCode, Debit = 0m, Credit = amount }
            }
            : new[]
            {
                new JournalLine { JournalId = journalId, AccountCode = CashOperationsSuspenseGlCode, Debit = amount, Credit = 0m },
                new JournalLine { JournalId = journalId, AccountCode = CashOnHandGlCode, Debit = 0m, Credit = amount }
            };

        _context.JournalEntries.Add(new JournalEntry
        {
            Id = journalId,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Reference = reference ?? $"VLT-{branchId}-{DateTime.UtcNow.Ticks}",
            Description = description,
            PostedBy = processedBy,
            Status = "POSTED",
            CreatedAt = DateTime.UtcNow,
        });

        _context.JournalLines.AddRange(lines);

        foreach (var line in lines)
        {
            var glAccount = line.AccountCode == CashOnHandGlCode ? cashOnHand : suspense;
            var balanceChange = glAccount.Category == "ASSET" || glAccount.Category == "EXPENSE"
                ? line.Debit - line.Credit
                : line.Credit - line.Debit;
            glAccount.Balance += balanceChange;
        }
    }

    private static bool ShouldPostVaultControlJournal(string? reference)
    {
        return string.IsNullOrWhiteSpace(reference) || !reference.StartsWith("IBT-", StringComparison.OrdinalIgnoreCase);
    }
    private Task LogCashOperationAsync(string action, string entityType, string entityId, string userId, object newValues)
    {
        return _auditLoggingService.LogActionAsync(
            action: action,
            entityType: entityType,
            entityId: entityId,
            userId: userId,
            description: $"{action} recorded for {entityType} {entityId}",
            newValues: newValues);
    }

    private BranchVaultDto MapToDto(BranchVault vault)
    {
        return new BranchVaultDto
        {
            Id = vault.Id,
            BranchId = vault.BranchId,
            BranchCode = vault.Branch?.Code ?? string.Empty,
            BranchName = vault.Branch?.Name ?? string.Empty,
            Currency = vault.Currency,
            CashOnHand = vault.CashOnHand,
            VaultLimit = vault.VaultLimit,
            MinBalance = vault.MinBalance,
            LastCountDate = vault.LastCountDate,
            LastCountBy = vault.LastCountBy,
            LastCountByName = vault.LastCountByStaff?.Name,
            UpdatedAt = vault.UpdatedAt,
        };
    }

    private static string ResolveBranchId(string? requestBranchId, string? tellerBranchId)
    {
        var branchId = string.IsNullOrWhiteSpace(requestBranchId) ? tellerBranchId : requestBranchId;
        if (string.IsNullOrWhiteSpace(branchId))
        {
            throw new InvalidOperationException("Branch is required for till operations.");
        }

        return branchId;
    }

    private static string NormalizeCurrency(string? currency)
    {
        return string.IsNullOrWhiteSpace(currency) ? "GHS" : currency.Trim().ToUpperInvariant();
    }

    private static string BuildTillKey(string branchId, string tellerId, string currency)
    {
        return $"{branchId}:{tellerId}:{NormalizeCurrency(currency)}";
    }

    private static string BuildTillNarration(string action, string tellerId, string branchId, string currency, string? notes)
    {
        return $"{action}|TELLER:{tellerId}|BRANCH:{branchId}|CUR:{NormalizeCurrency(currency)}|{notes ?? string.Empty}";
    }

    private static T? DeserializePayload<T>(string? payload) where T : class
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(payload);
        }
        catch
        {
            return null;
        }
    }

    private static void ValidateDenominationTotal(IEnumerable<CashDenominationLineDto>? denominations, decimal expectedAmount, string operationName)
    {
        if (denominations == null)
        {
            return;
        }

        var lines = NormalizeDenominationLines(denominations)
            .Where(item => item.Pieces > 0 || item.SuspectPieces > 0)
            .ToList();

        if (lines.Count == 0)
        {
            return;
        }

        var total = lines.Sum(item => ParseDenominationValue(item.Denomination) * item.Pieces);
        if (Math.Abs(total - expectedAmount) > 0.009m)
        {
            throw new InvalidOperationException($"The denomination total for {operationName} ({total:N2}) does not match the entered amount ({expectedAmount:N2}).");
        }
    }

    private static void AttachDenominations(VaultLedger ledger, IEnumerable<CashDenominationLineDto>? denominations)
    {
        if (denominations == null)
        {
            return;
        }

        foreach (var line in NormalizeDenominationLines(denominations).Where(item => item.Pieces + item.SuspectPieces > 0))
        {
            ledger.Denominations.Add(new VaultTransactionDenomination
            {
                LedgerId = ledger.LedgerId,
                Denomination = ParseDenomination(line.Denomination),
                Pieces = line.Pieces + line.SuspectPieces,
            });
        }
    }

    private async Task RaiseSuspectAndUnfitCashFlagsAsync(
        string branchId,
        string storeType,
        string storeId,
        string currency,
        IEnumerable<CashDenominationLineDto>? denominations,
        string? reference,
        string? narration)
    {
        var normalized = NormalizeDenominationLines(denominations).ToList();
        if (normalized.Count == 0)
        {
            return;
        }

        var suspectValue = normalized.Sum(item => ParseDenominationValue(item.Denomination) * item.SuspectPieces);
        var unfitValue = normalized.Sum(item => ParseDenominationValue(item.Denomination) * item.UnfitPieces);

        if (suspectValue > 0)
        {
            await _cashIncidentService.ReportSystemIncidentAsync(
                branchId,
                storeType,
                storeId,
                "COUNTERFEIT_SUSPECTED",
                currency,
                suspectValue,
                reference,
                $"Suspect notes segregated for review. {narration}",
                null);
        }

        if (unfitValue > 0)
        {
            await LogCashOperationAsync(
                "UNFIT_NOTES_RECORDED",
                storeType,
                storeId,
                string.Empty,
                new
                {
                    BranchId = branchId,
                    Currency = currency,
                    UnfitValue = unfitValue,
                    Reference = reference,
                    Narration = narration,
                    DenominationSummary = BuildDenominationComplianceSummary(normalized)
                });
        }
    }

    private static void EnsureNoSuspectNotes(IEnumerable<CashDenominationLineDto>? denominations, string operationName)
    {
        var suspectValue = NormalizeDenominationLines(denominations)
            .Sum(item => ParseDenominationValue(item.Denomination) * item.SuspectPieces);

        if (suspectValue > 0)
        {
            throw new InvalidOperationException($"Suspect notes cannot be posted as available cash during {operationName}. Record them during vault count or till close and escalate for review.");
        }
    }

    private static IEnumerable<CashDenominationLineDto> NormalizeDenominationLines(IEnumerable<CashDenominationLineDto>? denominations)
    {
        if (denominations == null)
        {
            return Enumerable.Empty<CashDenominationLineDto>();
        }

        return denominations.Select(line =>
        {
            var fitPieces = line.FitPieces;
            var unfitPieces = line.UnfitPieces;
            var suspectPieces = line.SuspectPieces;

            if (fitPieces == 0 && unfitPieces == 0 && suspectPieces == 0 && line.Pieces > 0)
            {
                fitPieces = line.Pieces;
            }

            return new CashDenominationLineDto
            {
                Denomination = line.Denomination,
                FitPieces = fitPieces,
                UnfitPieces = unfitPieces,
                SuspectPieces = suspectPieces,
                Pieces = fitPieces + unfitPieces,
                TotalValue = line.TotalValue,
                SuspectValue = line.SuspectValue,
            };
        });
    }

    private static object BuildDenominationComplianceSummary(IEnumerable<CashDenominationLineDto>? denominations)
    {
        var normalized = NormalizeDenominationLines(denominations).ToList();
        return new
        {
            FitValue = normalized.Sum(item => ParseDenominationValue(item.Denomination) * item.FitPieces),
            UnfitValue = normalized.Sum(item => ParseDenominationValue(item.Denomination) * item.UnfitPieces),
            SuspectValue = normalized.Sum(item => ParseDenominationValue(item.Denomination) * item.SuspectPieces),
            Lines = normalized.Select(item => new
            {
                item.Denomination,
                item.FitPieces,
                item.UnfitPieces,
                item.SuspectPieces
            }).ToList()
        };
    }

    private static string BuildCountComplianceNarration(string action, string? baseNarration, string? witnessOfficer, string? sealNumber)
    {
        return $"{action}|WITNESS:{witnessOfficer ?? "N/A"}|SEAL:{sealNumber ?? "N/A"}|{baseNarration ?? string.Empty}";
    }

    private static DenominationUnit ParseDenomination(string denomination)
    {
        if (string.IsNullOrWhiteSpace(denomination))
        {
            throw new InvalidOperationException("Denomination is required.");
        }

        var normalized = denomination
            .Trim()
            .ToUpperInvariant()
            .Replace(" ", string.Empty)
            .Replace(".", "_")
            .Replace("-", "_");

        if (normalized.StartsWith("GHS") && Enum.TryParse<DenominationUnit>(normalized, out var parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException($"Unsupported denomination '{denomination}'.");
    }

    private static decimal ParseDenominationValue(string denomination)
    {
        var parsed = ParseDenomination(denomination).ToString().Replace("GHS_", string.Empty).Replace('_', '.');
        return decimal.Parse(parsed, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static VaultLedger CreateVaultLedger(
        VaultTransactionType transactionType,
        string branchId,
        decimal amount,
        string currency,
        string? reference,
        string? narration,
        string processedBy)
    {
        return new VaultLedger
        {
            TransactionType = transactionType,
            Amount = amount,
            Currency = NormalizeCurrency(currency),
            Narration = narration,
            ReferenceNumber = reference ?? $"VLT-{DateTime.UtcNow.Ticks}",
            MakerUserId = Guid.TryParse(processedBy, out var makerId) ? makerId : Guid.Empty,
            IsApproved = true,
            ApprovedAt = DateTime.UtcNow,
            DebitAccountId = Guid.TryParse(branchId, out var debitId) ? debitId : Guid.Empty,
            CreditAccountId = Guid.TryParse(branchId, out var creditId) ? creditId : Guid.Empty,
            TransactionDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        };
    }

    private sealed class TillSessionPayload
    {
        public string TellerId { get; set; } = string.Empty;
        public string BranchId { get; set; } = string.Empty;
        public string Currency { get; set; } = "GHS";
        public decimal OpeningBalance { get; set; }
        public decimal MidDayCashLimit { get; set; }
        public string? Notes { get; set; }
    }

    private sealed class TillCashMovementPayload
    {
        public string TellerId { get; set; } = string.Empty;
        public string BranchId { get; set; } = string.Empty;
        public string Currency { get; set; } = "GHS";
        public decimal Amount { get; set; }
        public string? Reference { get; set; }
        public string? Narration { get; set; }
    }

    private sealed class CloseTillPayload
    {
        public string TellerId { get; set; } = string.Empty;
        public string BranchId { get; set; } = string.Empty;
        public string Currency { get; set; } = "GHS";
        public decimal PhysicalCashCount { get; set; }
        public decimal ExpectedBalance { get; set; }
        public decimal Variance { get; set; }
        public string? Notes { get; set; }
    }
}











