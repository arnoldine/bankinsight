using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Entities;
using BankInsight.API.Security;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Services;

public class DigitalBankingService
{
    private readonly ApplicationDbContext _context;
    private readonly AccountService _accountService;
    private readonly LoanService _loanService;
    private readonly CustomerService _customerService;
    private readonly IAuditLoggingService _auditLoggingService;
    private readonly ILedgerEngine _ledgerEngine;
    private readonly ICurrentUserContext _currentUser;

    public DigitalBankingService(
        ApplicationDbContext context,
        AccountService accountService,
        LoanService loanService,
        CustomerService customerService,
        IAuditLoggingService auditLoggingService,
        ILedgerEngine ledgerEngine,
        ICurrentUserContext currentUser)
    {
        _context = context;
        _accountService = accountService;
        _loanService = loanService;
        _customerService = customerService;
        _auditLoggingService = auditLoggingService;
        _ledgerEngine = ledgerEngine;
        _currentUser = currentUser;
    }

    public async Task<DigitalBankingDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var savingsAccounts = _context.Accounts.AsNoTracking().Where(a => a.Type == "SAVINGS" || a.Type == "CURRENT");
        var investmentProfiles = _context.DigitalInvestmentProfiles.AsNoTracking().Where(p => p.Status == "ACTIVE");
        var activeLoans = _context.Loans.AsNoTracking().Where(l => l.Status == "ACTIVE" || l.Status == "APPROVED" || l.Status == "PENDING");

        return new DigitalBankingDashboardDto
        {
            ActiveSavingsAccounts = await savingsAccounts.CountAsync(cancellationToken),
            TotalSavingsBalance = await savingsAccounts.SumAsync(a => a.Balance, cancellationToken),
            ActiveInvestmentProfiles = await investmentProfiles.CountAsync(cancellationToken),
            TotalInvestmentBalance = await investmentProfiles
                .Join(_context.Accounts.AsNoTracking(), profile => profile.AccountId, account => account.Id, (profile, account) => account.Balance)
                .SumAsync(balance => balance, cancellationToken),
            ActiveLoans = await activeLoans.CountAsync(cancellationToken),
            TotalLoanExposure = await activeLoans.SumAsync(l => l.OutstandingBalance ?? l.Principal, cancellationToken),
            PendingApprovals = await _context.ApprovalRequests.AsNoTracking().CountAsync(a => a.Status == "PENDING", cancellationToken)
        };
    }

    public async Task<List<DigitalBankingProductDto>> GetSavingsProductsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AsNoTracking()
            .Where(p => p.Status == "ACTIVE" && (p.Type == "SAVINGS" || p.Type == "CURRENT" || p.Type == "FIXED_DEPOSIT"))
            .OrderBy(p => p.Name)
            .Select(p => new DigitalBankingProductDto
            {
                ProductCode = p.Id,
                Name = p.Name,
                Type = p.Type,
                Currency = p.Currency,
                InterestRate = p.InterestRate,
                InterestMethod = p.InterestMethod,
                MinAmount = p.MinAmount,
                MaxAmount = p.MaxAmount,
                DefaultTerm = p.DefaultTerm,
                RequiresCompulsorySavings = p.RequiresCompulsorySavings,
                MinimumSavingsToLoanRatio = p.MinimumSavingsToLoanRatio,
                Status = p.Status
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<AccountListItemDto>> GetCustomerSavingsAccountsAsync(string customerId, CancellationToken cancellationToken = default)
    {
        return await _context.Accounts
            .AsNoTracking()
            .Include(a => a.Customer)
            .Where(a => a.CustomerId == customerId && (a.Type == "SAVINGS" || a.Type == "CURRENT"))
            .OrderBy(a => a.Id)
            .Select(a => new AccountListItemDto
            {
                Id = a.Id,
                CustomerId = a.CustomerId ?? string.Empty,
                CustomerName = a.Customer != null ? a.Customer.Name : (a.CustomerId ?? string.Empty),
                BranchId = a.BranchId ?? "BR001",
                Type = a.Type,
                Currency = a.Currency,
                Balance = a.Balance,
                LienAmount = a.LienAmount,
                Status = a.Status,
                ProductCode = a.ProductCode,
                LastTransDate = a.LastTransDate.HasValue ? a.LastTransDate.Value.ToString("O") : null,
                CreatedAt = a.CreatedAt.ToString("O")
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<AccountListItemDto> OpenSavingsAccountAsync(OpenDigitalSavingsAccountRequest request, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == request.ProductCode, cancellationToken)
            ?? throw new InvalidOperationException("Savings product not found.");

        var account = await _accountService.CreateAccountAsync(new CreateAccountRequest
        {
            CustomerId = request.CustomerId.Trim(),
            BranchId = request.BranchId,
            Type = NormalizeDepositAccountType(product.Type),
            Currency = request.Currency ?? product.Currency,
            ProductCode = product.Id,
            IsConfidential = request.IsConfidential,
            OwnerStaffId = request.OwnerStaffId
        });

        if (request.InitialDepositAmount > 0)
        {
            if (string.IsNullOrWhiteSpace(request.FundingAccountId))
            {
                throw new InvalidOperationException("Funding account is required when an initial deposit amount is provided.");
            }

            await PostInternalTransferAsync(
                request.FundingAccountId.Trim(),
                account.Id,
                request.InitialDepositAmount,
                $"Initial digital savings funding for {account.Id}",
                cancellationToken);
        }

        return await MapAccountListItemAsync(account.Id, cancellationToken)
            ?? throw new InvalidOperationException("Unable to load the created account.");
    }

    public async Task<AccountListItemDto> FundSavingsAccountAsync(string accountId, DigitalSavingsTransferRequest request, CancellationToken cancellationToken = default)
    {
        await PostInternalTransferAsync(request.CounterpartyAccountId.Trim(), accountId, request.Amount, request.Narration, cancellationToken);
        return await MapAccountListItemAsync(accountId, cancellationToken)
            ?? throw new InvalidOperationException("Savings account not found after funding.");
    }

    public async Task<AccountListItemDto> WithdrawSavingsAccountAsync(string accountId, DigitalSavingsTransferRequest request, CancellationToken cancellationToken = default)
    {
        await PostInternalTransferAsync(accountId, request.CounterpartyAccountId.Trim(), request.Amount, request.Narration, cancellationToken);
        return await MapAccountListItemAsync(accountId, cancellationToken)
            ?? throw new InvalidOperationException("Savings account not found after withdrawal.");
    }

    public async Task<DigitalInvestmentPortfolioDto> GetInvestmentPortfolioAsync(string? customerId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.DigitalInvestmentProfiles
            .AsNoTracking()
            .Include(profile => profile.Account)
            .Where(profile => customerId == null || profile.CustomerId == customerId);

        var profiles = await query.OrderByDescending(profile => profile.CreatedAt).ToListAsync(cancellationToken);
        var items = profiles.Select(MapDigitalInvestmentProfile).ToList();
        return new DigitalInvestmentPortfolioDto
        {
            ActiveProfiles = items.Count(item => item.Status == "ACTIVE"),
            TotalPrincipal = items.Where(item => item.Status == "ACTIVE").Sum(item => item.Principal),
            TotalProjectedMaturityValue = items.Where(item => item.Status == "ACTIVE").Sum(item => item.ProjectedMaturityValue),
            ByCurrency = items
                .Where(item => item.Status == "ACTIVE")
                .GroupBy(item => item.Currency)
                .ToDictionary(group => group.Key, group => group.Sum(item => item.Principal)),
            Items = items
        };
    }

    public async Task<DigitalInvestmentProfileDto> CreateInvestmentAsync(CreateDigitalInvestmentRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await _context.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken)
            ?? throw new InvalidOperationException("Customer not found.");
        var fundingAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == request.FundingAccountId, cancellationToken)
            ?? throw new InvalidOperationException("Funding account not found.");
        if (!string.Equals(fundingAccount.CustomerId, customer.Id, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Funding account does not belong to the selected customer.");
        }

        var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == request.ProductCode, cancellationToken)
            ?? throw new InvalidOperationException("Investment product not found.");

        var investmentAccount = await _accountService.CreateAccountAsync(new CreateAccountRequest
        {
            CustomerId = customer.Id,
            BranchId = fundingAccount.BranchId,
            Type = "FIXED_DEPOSIT",
            Currency = fundingAccount.Currency,
            ProductCode = product.Id
        });

        await PostInternalTransferAsync(
            fundingAccount.Id,
            investmentAccount.Id,
            request.Principal,
            $"Digital investment placement for {customer.Name}",
            cancellationToken);

        var now = DateTime.UtcNow;
        var profile = new DigitalInvestmentProfile
        {
            Id = $"DIP-{Guid.NewGuid():N}".Substring(0, 16).ToUpperInvariant(),
            AccountId = investmentAccount.Id,
            CustomerId = customer.Id,
            FundingAccountId = fundingAccount.Id,
            ProductCode = product.Id,
            TenorDays = request.TenorDays,
            Rate = request.Rate,
            PayoutOption = NormalizePayoutOption(request.PayoutOption),
            AutoRollover = request.AutoRollover,
            Status = "ACTIVE",
            StartDate = now,
            MaturityDate = now.AddDays(request.TenorDays),
            Notes = request.Notes,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.DigitalInvestmentProfiles.Add(profile);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditLoggingService.LogActionAsync(
            "DIGITAL_INVESTMENT_CREATED",
            "DIGITAL_INVESTMENT",
            profile.Id,
            _currentUser.UserId,
            $"Digital investment created for customer {customer.Id}",
            "SUCCESS",
            newValues: new { request.CustomerId, request.ProductCode, request.Principal, request.Rate, request.TenorDays, request.PayoutOption });

        var persisted = await _context.DigitalInvestmentProfiles.Include(p => p.Account).FirstAsync(p => p.Id == profile.Id, cancellationToken);
        return MapDigitalInvestmentProfile(persisted);
    }

    public async Task<DigitalInvestmentProfileDto> TopUpInvestmentAsync(string profileId, DigitalInvestmentActionRequest request, CancellationToken cancellationToken = default)
    {
        var profile = await LoadInvestmentProfileAsync(profileId, cancellationToken);
        if (!string.Equals(profile.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only active digital investments can be topped up.");
        }

        if (string.IsNullOrWhiteSpace(request.FundingAccountId) || !request.Amount.HasValue || request.Amount.Value <= 0)
        {
            throw new InvalidOperationException("Funding account and positive amount are required for top-up.");
        }

        await PostInternalTransferAsync(
            request.FundingAccountId.Trim(),
            profile.AccountId,
            request.Amount.Value,
            string.IsNullOrWhiteSpace(request.Notes) ? $"Digital investment top-up for {profile.AccountId}" : request.Notes.Trim(),
            cancellationToken);

        profile.UpdatedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            profile.Notes = AppendNote(profile.Notes, $"Top-up: {request.Notes.Trim()}");
        }

        await _context.SaveChangesAsync(cancellationToken);
        return MapDigitalInvestmentProfile(profile);
    }

    public async Task<DigitalInvestmentProfileDto> RolloverInvestmentAsync(string profileId, DigitalInvestmentActionRequest request, CancellationToken cancellationToken = default)
    {
        var profile = await LoadInvestmentProfileAsync(profileId, cancellationToken);
        if (!string.Equals(profile.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase) && !string.Equals(profile.Status, "MATURED", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only active or matured digital investments can be rolled over.");
        }

        if (!request.NewMaturityDate.HasValue)
        {
            throw new InvalidOperationException("A new maturity date is required for rollover.");
        }

        profile.StartDate = DateTime.UtcNow;
        profile.MaturityDate = request.NewMaturityDate.Value.ToUniversalTime();
        profile.TenorDays = Math.Max(1, (profile.MaturityDate.Date - profile.StartDate.Date).Days);
        profile.Rate = request.NewRate ?? profile.Rate;
        profile.Status = "ACTIVE";
        profile.MaturedAt = null;
        profile.AutoRollover = true;
        profile.UpdatedAt = DateTime.UtcNow;
        profile.Notes = AppendNote(profile.Notes, string.IsNullOrWhiteSpace(request.Notes) ? "Rolled over digitally." : request.Notes.Trim());

        await _context.SaveChangesAsync(cancellationToken);
        return MapDigitalInvestmentProfile(profile);
    }

    public async Task<DigitalInvestmentProfileDto> LiquidateInvestmentAsync(string profileId, DigitalInvestmentActionRequest request, CancellationToken cancellationToken = default)
    {
        var profile = await LoadInvestmentProfileAsync(profileId, cancellationToken);
        if (!string.Equals(profile.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only active digital investments can be liquidated.");
        }

        if (string.IsNullOrWhiteSpace(request.DestinationAccountId))
        {
            throw new InvalidOperationException("Destination account is required for liquidation.");
        }

        var liquidationAmount = Math.Max(0m, (profile.Account?.Balance ?? 0m) - (request.PenaltyAmount ?? 0m));
        if (liquidationAmount <= 0)
        {
            throw new InvalidOperationException("Investment has no available balance after penalty.");
        }

        await PostInternalTransferAsync(
            profile.AccountId,
            request.DestinationAccountId.Trim(),
            liquidationAmount,
            string.IsNullOrWhiteSpace(request.Notes) ? $"Digital investment liquidation for {profile.AccountId}" : request.Notes.Trim(),
            cancellationToken);

        profile.Status = "LIQUIDATED";
        profile.LiquidatedAt = DateTime.UtcNow;
        profile.UpdatedAt = DateTime.UtcNow;
        profile.Notes = AppendNote(profile.Notes, string.IsNullOrWhiteSpace(request.Notes) ? "Liquidated digitally." : request.Notes.Trim());

        await _context.SaveChangesAsync(cancellationToken);
        return MapDigitalInvestmentProfile(profile);
    }

    public async Task<DigitalLoanEligibilityDto> CheckLoanEligibilityAsync(CheckDigitalLoanEligibilityRequest request, CancellationToken cancellationToken = default)
    {
        var readiness = await _customerService.GetCustomerKycReadinessAsync(request.CustomerId);
        var reasons = new List<string>();
        if (readiness == null)
        {
            throw new InvalidOperationException("Customer not found.");
        }

        if (!readiness.IsReadyForLoanOrigination)
        {
            reasons.AddRange(readiness.MissingRequirements);
        }

        if (!string.IsNullOrWhiteSpace(request.LoanProductId) && request.Principal.HasValue)
        {
            var product = await _context.LoanProducts.AsNoTracking().FirstOrDefaultAsync(lp => lp.Id == request.LoanProductId, cancellationToken);
            if (product != null)
            {
                if (request.Principal.Value < product.MinAmount || request.Principal.Value > product.MaxAmount)
                {
                    reasons.Add($"Requested amount must fall within {product.MinAmount:N2} and {product.MaxAmount:N2}.");
                }
            }
        }

        var compulsorySavings = request.Principal.HasValue && !string.IsNullOrWhiteSpace(request.LoanProductId)
            ? await _loanService.EvaluateCompulsorySavingsAsync(request.CustomerId, request.LoanProductId, request.Principal.Value, cancellationToken)
            : new CompulsorySavingsAssessmentDto
            {
                IsEligible = true,
                Recommendation = "Compulsory savings check will run when both principal and loan product are selected."
            };

        if (compulsorySavings.RequiresCompulsorySavings && !compulsorySavings.IsEligible)
        {
            reasons.Add(compulsorySavings.Recommendation);
        }

        var creditCheck = await _loanService.CheckCreditAsync(new CheckCreditRequest
        {
            CustomerId = request.CustomerId,
            ProviderName = request.ProviderName
        });

        if (string.Equals(creditCheck.Decision, "FAIL", StringComparison.OrdinalIgnoreCase))
        {
            reasons.Add(creditCheck.Recommendation);
        }

        return new DigitalLoanEligibilityDto
        {
            IsEligible = reasons.Count == 0 && !string.Equals(creditCheck.Decision, "FAIL", StringComparison.OrdinalIgnoreCase),
            Reasons = reasons,
            CreditCheck = creditCheck,
            CompulsorySavings = compulsorySavings
        };
    }

    public async Task<LoanDto> ApplyLoanAsync(CreateDigitalLoanApplicationRequest request, CancellationToken cancellationToken = default)
    {
        var eligibility = await CheckLoanEligibilityAsync(new CheckDigitalLoanEligibilityRequest
        {
            CustomerId = request.CustomerId,
            LoanProductId = request.LoanProductId,
            Principal = request.Principal
        }, cancellationToken);

        if (!eligibility.IsEligible)
        {
            throw new InvalidOperationException($"Digital loan application is not eligible: {string.Join("; ", eligibility.Reasons)}");
        }

        return await _loanService.ApplyLoanAsync(new ApplyLoanRequest
        {
            CustomerId = request.CustomerId.Trim(),
            LoanProductId = request.LoanProductId.Trim(),
            Principal = request.Principal,
            ServicingAccountId = string.IsNullOrWhiteSpace(request.ServicingAccountId) ? null : request.ServicingAccountId.Trim(),
            CollateralAccountId = string.IsNullOrWhiteSpace(request.CollateralAccountId) ? null : request.CollateralAccountId.Trim(),
            ClientReference = string.IsNullOrWhiteSpace(request.ClientReference) ? $"DIGITAL-{DateTime.UtcNow:yyyyMMddHHmmss}" : request.ClientReference.Trim(),
            IsConfidential = false
        }, _currentUser.UserId);
    }

    public async Task<LoanDto> RepayLoanAsync(string loanId, LoanRepayRequest request, CancellationToken cancellationToken = default)
    {
        return await _loanService.RepayLoanAsync(loanId, request);
    }

    public async Task<LoanDto> RestructureLoanAsync(LoanRestructureRequest request, CancellationToken cancellationToken = default)
    {
        return await _loanService.RestructureLoanAsync(request, _currentUser.UserId);
    }

    public Task<LoanStatementDto> GetLoanStatementAsync(string loanId, CancellationToken cancellationToken = default)
        => _loanService.GetLoanStatementAsync(loanId);

    public Task<List<LoanScheduleDto>> GetLoanScheduleAsync(string loanId, CancellationToken cancellationToken = default)
        => _loanService.GetLoanScheduleAsync(loanId);

    private async Task PostInternalTransferAsync(string fromAccountId, string toAccountId, decimal amount, string narration, CancellationToken cancellationToken)
    {
        var fromAccount = await _context.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == fromAccountId, cancellationToken)
            ?? throw new InvalidOperationException("Source account not found.");
        var toAccount = await _context.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == toAccountId, cancellationToken)
            ?? throw new InvalidOperationException("Destination account not found.");
        var customer = !string.IsNullOrWhiteSpace(fromAccount.CustomerId)
            ? await _context.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == fromAccount.CustomerId, cancellationToken)
            : null;

        var result = await _ledgerEngine.PostTransferAsync(new TransferRequest
        {
            FromAccountId = fromAccountId,
            ToAccountId = toAccountId,
            CustomerId = fromAccount.CustomerId ?? toAccount.CustomerId ?? string.Empty,
            Amount = amount,
            Narration = narration,
            TellerId = _currentUser.UserId,
            CustomerGhanaCard = customer?.GhanaCard ?? string.Empty
        });

        if (!result.Success)
        {
            throw new InvalidOperationException(result.Message);
        }
    }

    private async Task<AccountListItemDto?> MapAccountListItemAsync(string accountId, CancellationToken cancellationToken)
    {
        return await _context.Accounts
            .AsNoTracking()
            .Include(a => a.Customer)
            .Where(a => a.Id == accountId)
            .Select(a => new AccountListItemDto
            {
                Id = a.Id,
                CustomerId = a.CustomerId ?? string.Empty,
                CustomerName = a.Customer != null ? a.Customer.Name : (a.CustomerId ?? string.Empty),
                BranchId = a.BranchId ?? "BR001",
                Type = a.Type,
                Currency = a.Currency,
                Balance = a.Balance,
                LienAmount = a.LienAmount,
                Status = a.Status,
                ProductCode = a.ProductCode,
                LastTransDate = a.LastTransDate.HasValue ? a.LastTransDate.Value.ToString("O") : null,
                CreatedAt = a.CreatedAt.ToString("O")
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<DigitalInvestmentProfile> LoadInvestmentProfileAsync(string profileId, CancellationToken cancellationToken)
    {
        var profile = await _context.DigitalInvestmentProfiles
            .Include(p => p.Account)
            .FirstOrDefaultAsync(p => p.Id == profileId, cancellationToken);
        return profile ?? throw new InvalidOperationException("Digital investment profile not found.");
    }

    private static DigitalInvestmentProfileDto MapDigitalInvestmentProfile(DigitalInvestmentProfile profile)
    {
        var principal = profile.Account?.Balance ?? 0m;
        var projectedMaturityValue = principal + (principal * profile.Rate / 100m * (profile.TenorDays / 365m));
        var resolvedStatus = profile.Status == "ACTIVE" && profile.MaturityDate <= DateTime.UtcNow
            ? "MATURED"
            : profile.Status;

        return new DigitalInvestmentProfileDto
        {
            Id = profile.Id,
            AccountId = profile.AccountId,
            CustomerId = profile.CustomerId,
            FundingAccountId = profile.FundingAccountId,
            ProductCode = profile.ProductCode,
            Currency = profile.Account?.Currency ?? "GHS",
            Principal = principal,
            Rate = profile.Rate,
            TenorDays = profile.TenorDays,
            PayoutOption = profile.PayoutOption,
            AutoRollover = profile.AutoRollover,
            Status = resolvedStatus,
            StartDate = profile.StartDate,
            MaturityDate = profile.MaturityDate,
            ProjectedMaturityValue = Math.Round(projectedMaturityValue, 2),
            Notes = profile.Notes
        };
    }

    private static string NormalizeDepositAccountType(string productType)
    {
        var normalized = productType.Trim().ToUpperInvariant();
        return normalized switch
        {
            "CURRENT" => "CURRENT",
            "FIXED_DEPOSIT" => "FIXED_DEPOSIT",
            _ => "SAVINGS"
        };
    }

    private static string NormalizePayoutOption(string payoutOption)
    {
        if (string.IsNullOrWhiteSpace(payoutOption))
        {
            return "AT_MATURITY";
        }

        return payoutOption.Trim().ToUpperInvariant() switch
        {
            "MONTHLY" => "MONTHLY",
            "QUARTERLY" => "QUARTERLY",
            _ => "AT_MATURITY"
        };
    }

    private static string AppendNote(string? existing, string note)
    {
        return string.IsNullOrWhiteSpace(existing) ? note : $"{existing}\n{note}";
    }
}
