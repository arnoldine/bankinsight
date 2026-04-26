using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Entities;
using BankInsight.API.Security;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace BankInsight.API.Services;

public class ClientChannelService
{
    private static readonly string[] AllowedComplaintAttachmentContentTypes =
        ["image/png", "image/jpeg", "image/jpg", "image/webp", "application/pdf"];
    private const int MaxComplaintAttachmentBytes = 5 * 1024 * 1024;
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserContext _currentUser;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAuditLoggingService _auditLoggingService;
    private readonly CustomerService _customerService;
    private readonly ClientAuthService _clientAuthService;
    private readonly IClientFileStorageService _clientFileStorageService;
    private readonly ILedgerEngine _ledgerEngine;
    private readonly LoanService _loanService;

    public ClientChannelService(
        ApplicationDbContext context,
        ICurrentUserContext currentUser,
        IHttpContextAccessor httpContextAccessor,
        IAuditLoggingService auditLoggingService,
        CustomerService customerService,
        ClientAuthService clientAuthService,
        IClientFileStorageService clientFileStorageService,
        ILedgerEngine ledgerEngine,
        LoanService loanService)
    {
        _context = context;
        _currentUser = currentUser;
        _httpContextAccessor = httpContextAccessor;
        _auditLoggingService = auditLoggingService;
        _customerService = customerService;
        _clientAuthService = clientAuthService;
        _clientFileStorageService = clientFileStorageService;
        _ledgerEngine = ledgerEngine;
        _loanService = loanService;
    }

    public async Task<ClientChannelBootstrapResponse> GetBootstrapAsync()
    {
        var identity = await GetIdentityAsync();
        var customer = await ResolveLinkedCustomerAsync();
        var warnings = new List<string>();

        if (customer == null)
        {
            warnings.Add("No customer record is currently linked to the signed-in client identity.");
        }

        return new ClientChannelBootstrapResponse
        {
            Identity = identity,
            LinkedCustomer = customer == null ? null : MapLinkedCustomer(customer),
            Warnings = warnings
        };
    }

    public async Task<CustomerProfileResponse?> GetLinkedCustomerProfileAsync()
    {
        var customer = await ResolveLinkedCustomerAsync();
        if (customer == null)
        {
            return null;
        }

        return await _customerService.GetCustomerProfileAsync(customer.Id);
    }

    public async Task<List<ClientAccountDto>> GetLinkedAccountsAsync()
    {
        var customer = await ResolveLinkedCustomerAsync();
        if (customer == null)
        {
            return [];
        }

        return await _context.Accounts
            .AsNoTracking()
            .Where(a => a.CustomerId == customer.Id)
            .OrderBy(a => a.Id)
            .Select(a => new ClientAccountDto
            {
                Id = a.Id,
                Type = a.Type,
                Currency = a.Currency,
                Balance = a.Balance,
                LienAmount = a.LienAmount,
                Status = a.Status,
                ProductCode = a.ProductCode,
                LastTransDate = a.LastTransDate.HasValue ? a.LastTransDate.Value.ToString("O") : null
            })
            .ToListAsync();
    }

    public async Task<ClientBankingOverviewDto> GetBankingOverviewAsync()
    {
        var customer = await ResolveLinkedCustomerAsync();
        if (customer == null)
        {
            return new ClientBankingOverviewDto();
        }

        var accounts = await _context.Accounts
            .AsNoTracking()
            .Where(a => a.CustomerId == customer.Id && a.Status == "ACTIVE")
            .ToListAsync();
        var standingOrders = await _context.Set<ClientStandingOrder>()
            .AsNoTracking()
            .Where(s => s.CustomerId == customer.Id && s.Status == "ACTIVE")
            .ToListAsync();
        var loans = await _context.Loans
            .AsNoTracking()
            .Where(l => l.CustomerId == customer.Id && l.Status != "CLOSED")
            .ToListAsync();

        return new ClientBankingOverviewDto
        {
            TotalVisibleBalance = accounts.Where(a => a.Type != "FIXED_DEPOSIT").Sum(a => a.Balance),
            ActiveAccountCount = accounts.Count(a => a.Type != "FIXED_DEPOSIT"),
            ActiveStandingOrderCount = standingOrders.Count,
            ActiveLoanCount = loans.Count,
            ActiveInvestmentCount = accounts.Count(a => a.Type == "FIXED_DEPOSIT" && a.Status == "ACTIVE"),
            TotalLoanExposure = loans.Sum(l => l.OutstandingBalance ?? l.Principal),
            TotalInvestmentBalance = accounts.Where(a => a.Type == "FIXED_DEPOSIT" && a.Status == "ACTIVE").Sum(a => a.Balance)
        };
    }

    public async Task<List<ClientMerchantDto>> GetMerchantCatalogAsync()
    {
        var catalog = new List<ClientMerchantDto>
        {
            new()
            {
                Code = "ECG",
                Name = "ECG Bills",
                Category = "Utilities",
                SettlementType = "BANKINSIGHT_INTERNAL",
                Currency = "GHS",
                DestinationAccountId = "ACC-MERCHANT-ECG",
                MerchantKind = "CATALOG",
                AcceptsQrPayments = false
            },
            new()
            {
                Code = "DSTV",
                Name = "DStv Ghana",
                Category = "Entertainment",
                SettlementType = "BANKINSIGHT_INTERNAL",
                Currency = "GHS",
                DestinationAccountId = "ACC-MERCHANT-DSTV",
                MerchantKind = "CATALOG",
                AcceptsQrPayments = false
            },
            new()
            {
                Code = "MSQ",
                Name = "Market Square Stores",
                Category = "Retail",
                SettlementType = "BANKINSIGHT_INTERNAL",
                Currency = "GHS",
                DestinationAccountId = "ACC-MERCHANT-SHOP",
                MerchantKind = "CATALOG",
                AcceptsQrPayments = false
            }
        };

        var merchantProfiles = await _context.Set<ClientMerchantProfile>()
            .AsNoTracking()
            .Where(profile => profile.Status == "ACTIVE" && profile.AcceptsAppPayments)
            .OrderBy(profile => profile.DisplayName)
            .ToListAsync();

        catalog.AddRange(merchantProfiles.Select(MapMerchantCatalogItem));
        return catalog;
    }

    public async Task<ClientTransferResultDto> CreateInternalTransferAsync(CreateClientInternalTransferRequest request)
    {
        var customer = await RequireLinkedCustomerAsync();
        var fromAccount = await RequireOwnedAccountAsync(customer.Id, request.FromAccountId);
        var toAccount = await RequireOwnedAccountAsync(customer.Id, request.ToAccountId);

        if (fromAccount.Id == toAccount.Id)
        {
            throw new InvalidOperationException("Source and destination accounts must be different.");
        }

        await RequireStepUpAsync(request.StepUpToken, "TRANSFER_INTERNAL");

        var result = await _ledgerEngine.PostTransferAsync(new TransferRequest
        {
            FromAccountId = fromAccount.Id,
            ToAccountId = toAccount.Id,
            CustomerId = customer.Id,
            Amount = request.Amount,
            Narration = request.Narration.Trim(),
            TellerId = _currentUser.UserId,
            CustomerGhanaCard = customer.GhanaCard ?? string.Empty
        });

        if (!result.Success)
        {
            throw new InvalidOperationException(result.Message);
        }

        return MapTransferResult(result);
    }

    public async Task<ClientTransferResultDto> CreateMerchantPaymentAsync(CreateClientMerchantPaymentRequest request)
    {
        var customer = await RequireLinkedCustomerAsync();
        var sourceAccount = await RequireOwnedAccountAsync(customer.Id, request.SourceAccountId);
        var merchant = (await GetMerchantCatalogAsync())
            .FirstOrDefault(item => string.Equals(item.Code, request.MerchantCode, StringComparison.OrdinalIgnoreCase));

        if (merchant?.DestinationAccountId == null)
        {
            throw new InvalidOperationException("Merchant was not found or is not currently payable.");
        }

        await RequireStepUpAsync(request.StepUpToken, "MERCHANT_PAYMENT");

        var result = await _ledgerEngine.PostTransferAsync(new TransferRequest
        {
            FromAccountId = sourceAccount.Id,
            ToAccountId = merchant.DestinationAccountId,
            CustomerId = customer.Id,
            Amount = request.Amount,
            Narration = string.IsNullOrWhiteSpace(request.Narration)
                ? $"Merchant payment to {merchant.Name}"
                : request.Narration.Trim(),
            TellerId = _currentUser.UserId,
            CustomerGhanaCard = customer.GhanaCard ?? string.Empty
        });

        if (!result.Success)
        {
            throw new InvalidOperationException(result.Message);
        }

        return MapTransferResult(result);
    }

    public async Task<ClientMerchantAcceptanceEligibilityDto> GetMerchantAcceptanceEligibilityAsync()
    {
        var customer = await RequireLinkedCustomerAsync();
        var eligibleAccounts = await _context.Accounts
            .AsNoTracking()
            .Where(a => a.CustomerId == customer.Id && a.Status == "ACTIVE" && a.Type != "FIXED_DEPOSIT")
            .OrderBy(a => a.Id)
            .Select(a => new ClientAccountDto
            {
                Id = a.Id,
                Type = a.Type,
                Currency = a.Currency,
                Balance = a.Balance,
                LienAmount = a.LienAmount,
                Status = a.Status,
                ProductCode = a.ProductCode,
                LastTransDate = a.LastTransDate.HasValue ? a.LastTransDate.Value.ToString("O") : null
            })
            .ToListAsync();

        var canEnroll = IsBusinessCustomer(customer) && eligibleAccounts.Count > 0;
        var reason = canEnroll
            ? null
            : !IsBusinessCustomer(customer)
                ? "Only business-linked customers can enroll as merchants."
                : "An active settlement account is required before merchant acceptance can be enabled.";

        return new ClientMerchantAcceptanceEligibilityDto
        {
            CanEnroll = canEnroll,
            CustomerId = customer.Id,
            CustomerType = customer.Type ?? string.Empty,
            BusinessName = customer.Name,
            Reason = reason,
            EligibleSettlementAccounts = eligibleAccounts
        };
    }

    public async Task<List<ClientMerchantProfileDto>> GetMerchantProfilesAsync()
    {
        var customer = await RequireLinkedCustomerAsync();
        var profiles = await _context.Set<ClientMerchantProfile>()
            .AsNoTracking()
            .Include(profile => profile.SettlementAccount)
            .Where(profile => profile.CustomerId == customer.Id)
            .OrderByDescending(profile => profile.UpdatedAt)
            .ToListAsync();
        return profiles.Select(MapMerchantProfile).ToList();
    }

    public async Task<ClientMerchantProfileDto> CreateMerchantProfileAsync(CreateClientMerchantProfileRequest request)
    {
        var customer = await RequireLinkedCustomerAsync();
        if (!IsBusinessCustomer(customer))
        {
            throw new InvalidOperationException("Only business customers can be enrolled as app merchants.");
        }

        await RequireStepUpAsync(request.StepUpToken, "MERCHANT_PROFILE_ENROLLMENT");

        var settlementAccount = await RequireOwnedAccountAsync(customer.Id, request.SettlementAccountId.Trim());
        if (string.Equals(settlementAccount.Type, "FIXED_DEPOSIT", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Fixed deposit accounts cannot be used as merchant settlement accounts.");
        }

        var existingProfile = await _context.Set<ClientMerchantProfile>()
            .FirstOrDefaultAsync(profile =>
                profile.CustomerId == customer.Id &&
                profile.SettlementAccountId == settlementAccount.Id &&
                profile.Status == "ACTIVE");

        if (existingProfile != null)
        {
            await _context.Entry(existingProfile).Reference(profile => profile.SettlementAccount).LoadAsync();
            return MapMerchantProfile(existingProfile);
        }

        var merchantCode = await GenerateMerchantCodeAsync(request.DisplayName);
        var now = DateTime.UtcNow;
        var profile = new ClientMerchantProfile
        {
            Id = $"CMP-{Guid.NewGuid():N}".Substring(0, 16).ToUpperInvariant(),
            CustomerId = customer.Id,
            SettlementAccountId = settlementAccount.Id,
            MerchantCode = merchantCode,
            DisplayName = request.DisplayName.Trim(),
            Category = request.Category.Trim(),
            Currency = settlementAccount.Currency,
            Status = "ACTIVE",
            QrScheme = "BANKINSIGHT_QR",
            AcceptsAppPayments = true,
            GhQrReady = false,
            CreatedAt = now,
            UpdatedAt = now
        };
        profile.QrPayload = BuildMerchantQrPayload(profile.Id, profile.MerchantCode, profile.Currency);

        _context.Add(profile);
        await _context.SaveChangesAsync();

        profile.SettlementAccount = settlementAccount;
        return MapMerchantProfile(profile);
    }

    public async Task<ClientQrPaymentPreviewDto> ResolveQrPaymentAsync(ResolveClientQrPaymentRequest request)
    {
        var profile = await RequireMerchantProfileForQrPayloadAsync(request.QrPayload);
        return new ClientQrPaymentPreviewDto
        {
            MerchantCode = profile.MerchantCode,
            MerchantName = profile.DisplayName,
            Category = profile.Category,
            Currency = profile.Currency,
            QrScheme = profile.QrScheme,
            GhQrReady = profile.GhQrReady,
            DestinationAccountId = profile.SettlementAccountId,
            MerchantProfileId = profile.Id
        };
    }

    public async Task<ClientTransferResultDto> CreateQrPaymentAsync(CreateClientQrPaymentRequest request)
    {
        var profile = await RequireMerchantProfileForQrPayloadAsync(request.QrPayload);
        if (!profile.AcceptsAppPayments || !string.Equals(profile.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("This merchant is not currently accepting app payments.");
        }

        return await CreateMerchantPaymentAsync(new CreateClientMerchantPaymentRequest
        {
            MerchantCode = profile.MerchantCode,
            SourceAccountId = request.SourceAccountId,
            Amount = request.Amount,
            Narration = string.IsNullOrWhiteSpace(request.Narration)
                ? $"QR payment to {profile.DisplayName}"
                : request.Narration,
            StepUpToken = request.StepUpToken
        });
    }

    public async Task<List<ClientStandingOrderDto>> GetStandingOrdersAsync()
    {
        var customer = await ResolveLinkedCustomerAsync();
        if (customer == null)
        {
            return [];
        }

        return await _context.Set<ClientStandingOrder>()
            .AsNoTracking()
            .Where(s => s.CustomerId == customer.Id)
            .OrderBy(s => s.NextRunAt)
            .Select(s => new ClientStandingOrderDto
            {
                Id = s.Id,
                SourceAccountId = s.SourceAccountId,
                InstructionType = s.InstructionType,
                MerchantCode = s.MerchantCode,
                MerchantName = s.MerchantName,
                DestinationAccountId = s.DestinationAccountId,
                Amount = s.Amount,
                Currency = s.Currency,
                Frequency = s.Frequency,
                Narration = s.Narration,
                StartDate = s.StartDate.ToString("O"),
                NextRunAt = s.NextRunAt.ToString("O"),
                EndDate = s.EndDate.HasValue ? s.EndDate.Value.ToString("O") : null,
                LastRunAt = s.LastRunAt.HasValue ? s.LastRunAt.Value.ToString("O") : null,
                Status = s.Status
            })
            .ToListAsync();
    }

    public async Task<ClientStandingOrderDto> CreateStandingOrderAsync(CreateClientStandingOrderRequest request)
    {
        var customer = await RequireLinkedCustomerAsync();
        var sourceAccount = await RequireOwnedAccountAsync(customer.Id, request.SourceAccountId);
        await RequireStepUpAsync(request.StepUpToken, "STANDING_ORDER");

        var instructionType = request.InstructionType.Trim().ToUpperInvariant();
        string? merchantName = null;
        string? destinationAccountId = request.DestinationAccountId?.Trim();
        string? merchantCode = request.MerchantCode?.Trim().ToUpperInvariant();

        if (instructionType == "MERCHANT_PAYMENT")
        {
            var merchant = (await GetMerchantCatalogAsync()).FirstOrDefault(item => item.Code == merchantCode);
            if (merchant?.DestinationAccountId == null)
            {
                throw new InvalidOperationException("Merchant was not found or is not currently payable.");
            }

            merchantName = merchant.Name;
            destinationAccountId = merchant.DestinationAccountId;
        }
        else if (instructionType == "INTERNAL_TRANSFER")
        {
            if (string.IsNullOrWhiteSpace(destinationAccountId))
            {
                throw new InvalidOperationException("Destination account is required for an internal transfer standing order.");
            }

            await RequireOwnedAccountAsync(customer.Id, destinationAccountId);
        }
        else
        {
            throw new InvalidOperationException("Standing orders currently support INTERNAL_TRANSFER and MERCHANT_PAYMENT only.");
        }

        var startDate = (request.StartDate ?? DateTime.UtcNow).ToUniversalTime();
        var standingOrder = new ClientStandingOrder
        {
            Id = $"CSO-{Guid.NewGuid():N}".Substring(0, 16).ToUpperInvariant(),
            CustomerId = customer.Id,
            SourceAccountId = sourceAccount.Id,
            InstructionType = instructionType,
            MerchantCode = merchantCode,
            MerchantName = merchantName,
            DestinationAccountId = destinationAccountId,
            Amount = request.Amount,
            Currency = sourceAccount.Currency,
            Frequency = request.Frequency.Trim().ToUpperInvariant(),
            Narration = request.Narration.Trim(),
            StartDate = startDate,
            NextRunAt = CalculateNextRunAt(startDate, request.Frequency),
            EndDate = request.EndDate?.ToUniversalTime(),
            Status = "ACTIVE",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Add(standingOrder);
        await _context.SaveChangesAsync();

        return new ClientStandingOrderDto
        {
            Id = standingOrder.Id,
            SourceAccountId = standingOrder.SourceAccountId,
            InstructionType = standingOrder.InstructionType,
            MerchantCode = standingOrder.MerchantCode,
            MerchantName = standingOrder.MerchantName,
            DestinationAccountId = standingOrder.DestinationAccountId,
            Amount = standingOrder.Amount,
            Currency = standingOrder.Currency,
            Frequency = standingOrder.Frequency,
            Narration = standingOrder.Narration,
            StartDate = standingOrder.StartDate.ToString("O"),
            NextRunAt = standingOrder.NextRunAt.ToString("O"),
            EndDate = standingOrder.EndDate?.ToString("O"),
            LastRunAt = standingOrder.LastRunAt?.ToString("O"),
            Status = standingOrder.Status
        };
    }

    public async Task<ClientStandingOrderDto?> UpdateStandingOrderStatusAsync(string standingOrderId, string status)
    {
        var customer = await ResolveLinkedCustomerAsync();
        if (customer == null)
        {
            return null;
        }

        var standingOrder = await _context.Set<ClientStandingOrder>()
            .FirstOrDefaultAsync(s => s.Id == standingOrderId && s.CustomerId == customer.Id);
        if (standingOrder == null)
        {
            return null;
        }

        standingOrder.Status = status.Trim().ToUpperInvariant();
        standingOrder.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return new ClientStandingOrderDto
        {
            Id = standingOrder.Id,
            SourceAccountId = standingOrder.SourceAccountId,
            InstructionType = standingOrder.InstructionType,
            MerchantCode = standingOrder.MerchantCode,
            MerchantName = standingOrder.MerchantName,
            DestinationAccountId = standingOrder.DestinationAccountId,
            Amount = standingOrder.Amount,
            Currency = standingOrder.Currency,
            Frequency = standingOrder.Frequency,
            Narration = standingOrder.Narration,
            StartDate = standingOrder.StartDate.ToString("O"),
            NextRunAt = standingOrder.NextRunAt.ToString("O"),
            EndDate = standingOrder.EndDate?.ToString("O"),
            LastRunAt = standingOrder.LastRunAt?.ToString("O"),
            Status = standingOrder.Status
        };
    }

    public async Task<List<ClientFixedDepositDto>> GetFixedDepositsAsync()
    {
        var customer = await ResolveLinkedCustomerAsync();
        if (customer == null)
        {
            return [];
        }

        return await _context.Accounts
            .AsNoTracking()
            .Where(a => a.CustomerId == customer.Id && a.Type == "FIXED_DEPOSIT")
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new ClientFixedDepositDto
            {
                Id = a.Id,
                AccountId = a.Id,
                Principal = a.Balance,
                Rate = 12m,
                TenureDays = 90,
                StartDate = a.CreatedAt.ToString("O"),
                MaturityDate = a.CreatedAt.AddDays(90).ToString("O"),
                Currency = a.Currency,
                Status = a.Status,
                MaturityValue = Math.Round(a.Balance * 1.03m, 2)
            })
            .ToListAsync();
    }

    public async Task<ClientFixedDepositDto> CreateFixedDepositAsync(CreateClientFixedDepositRequest request)
    {
        var customer = await RequireLinkedCustomerAsync();
        var sourceAccount = await RequireOwnedAccountAsync(customer.Id, request.SourceAccountId);
        await RequireStepUpAsync(request.StepUpToken, "INVESTMENT_FIXED_DEPOSIT");

        var depositAccount = new Account
        {
            Id = $"FD-{Guid.NewGuid():N}".Substring(0, 16).ToUpperInvariant(),
            CustomerId = customer.Id,
            BranchId = sourceAccount.BranchId,
            ProductCode = sourceAccount.ProductCode,
            Type = "FIXED_DEPOSIT",
            Currency = request.Currency.Trim().ToUpperInvariant(),
            Balance = 0m,
            Status = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };

        _context.Accounts.Add(depositAccount);
        await _context.SaveChangesAsync();

        var fundingResult = await _ledgerEngine.PostTransferAsync(new TransferRequest
        {
            FromAccountId = sourceAccount.Id,
            ToAccountId = depositAccount.Id,
            CustomerId = customer.Id,
            Amount = request.Principal,
            Narration = $"Fixed deposit placement for {request.TenureDays} days",
            TellerId = _currentUser.UserId,
            CustomerGhanaCard = customer.GhanaCard ?? string.Empty
        });

        if (!fundingResult.Success)
        {
            throw new InvalidOperationException(fundingResult.Message);
        }

        var maturityValue = request.Principal + (request.Principal * request.Rate / 100m * (request.TenureDays / 365m));
        return new ClientFixedDepositDto
        {
            Id = depositAccount.Id,
            AccountId = depositAccount.Id,
            Principal = request.Principal,
            Rate = request.Rate,
            TenureDays = request.TenureDays,
            StartDate = depositAccount.CreatedAt.ToString("O"),
            MaturityDate = depositAccount.CreatedAt.AddDays(request.TenureDays).ToString("O"),
            Currency = request.Currency.Trim().ToUpperInvariant(),
            Status = "ACTIVE",
            MaturityValue = Math.Round(maturityValue, 2)
        };
    }

    public async Task<List<ClientLoanProductDto>> GetClientLoanProductsAsync()
    {
        return await _context.LoanProducts
            .AsNoTracking()
            .Where(lp => lp.IsActive)
            .OrderBy(lp => lp.Name)
            .Select(lp => new ClientLoanProductDto
            {
                Id = lp.Id,
                Code = lp.Code,
                Name = lp.Name,
                ProductType = lp.ProductType.ToString(),
                RepaymentFrequency = lp.RepaymentFrequency.ToString(),
                TermInPeriods = lp.TermInPeriods,
                AnnualInterestRate = lp.AnnualInterestRate,
                MinAmount = lp.MinAmount,
                MaxAmount = lp.MaxAmount
            })
            .ToListAsync();
    }

    public async Task<List<ClientLoanSummaryDto>> GetClientLoansAsync()
    {
        var customer = await ResolveLinkedCustomerAsync();
        if (customer == null)
        {
            return [];
        }

        return await _context.Loans
            .AsNoTracking()
            .Include(l => l.Product)
            .Include(l => l.LoanProduct)
            .Where(l => l.CustomerId == customer.Id)
            .OrderByDescending(l => l.ApplicationDate)
            .Select(l => new ClientLoanSummaryDto
            {
                Id = l.Id,
                ProductCode = l.ProductCode,
                ProductName = l.Product != null ? l.Product.Name : l.LoanProduct != null ? l.LoanProduct.Name : null,
                Principal = l.Principal,
                Rate = l.Rate,
                TermMonths = l.TermMonths,
                Status = l.Status,
                OutstandingBalance = l.OutstandingBalance,
                ServicingAccountId = l.ServicingAccountId,
                RepaymentFrequency = l.RepaymentFrequency,
                DisbursementDate = l.DisbursementDate.HasValue ? l.DisbursementDate.Value.ToString("yyyy-MM-dd") : null,
                ParBucket = l.ParBucket
            })
            .ToListAsync();
    }

    public async Task<ClientLoanSummaryDto> ApplyForLoanAsync(CreateClientLoanApplicationRequest request)
    {
        var customer = await RequireLinkedCustomerAsync();
        await RequireStepUpAsync(request.StepUpToken, "LOAN_APPLICATION");

        if (!string.IsNullOrWhiteSpace(request.ServicingAccountId))
        {
            await RequireOwnedAccountAsync(customer.Id, request.ServicingAccountId);
        }

        var created = await _loanService.ApplyLoanAsync(new ApplyLoanRequest
        {
            CustomerId = customer.Id,
            LoanProductId = request.LoanProductId.Trim(),
            Principal = request.Principal,
            ServicingAccountId = request.ServicingAccountId?.Trim(),
            ClientReference = $"CLIENT-{DateTime.UtcNow:yyyyMMddHHmmss}",
            IsConfidential = false
        }, _currentUser.UserId);

        return new ClientLoanSummaryDto
        {
            Id = created.Id,
            ProductCode = created.ProductCode,
            ProductName = created.ProductName,
            Principal = created.Principal,
            Rate = created.Rate,
            TermMonths = created.TermMonths,
            Status = created.Status,
            OutstandingBalance = created.OutstandingBalance,
            ServicingAccountId = created.ServicingAccountId,
            RepaymentFrequency = created.RepaymentFrequency,
            DisbursementDate = created.DisbursementDate?.ToString("yyyy-MM-dd"),
            ParBucket = created.ParBucket
        };
    }

    public async Task<List<LoanScheduleDto>> GetClientLoanScheduleAsync(string loanId)
    {
        await RequireOwnedLoanAsync(loanId);
        return await _loanService.GetLoanScheduleAsync(loanId);
    }

    public async Task<LoanStatementDto> GetClientLoanStatementAsync(string loanId)
    {
        await RequireOwnedLoanAsync(loanId);
        return await _loanService.GetLoanStatementAsync(loanId);
    }

    public async Task<List<ClientSessionDto>> GetMySessionsAsync()
    {
        return await _context.Set<ClientChannelSession>()
            .AsNoTracking()
            .Where(s => s.CustomerCredentialId == _currentUser.UserId)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new ClientSessionDto
            {
                Id = s.Id,
                IpAddress = s.IpAddress,
                UserAgent = s.UserAgent,
                CreatedAt = s.CreatedAt.ToString("O"),
                ExpiresAt = s.ExpiresAt.ToString("O"),
                LastActivity = s.LastActivity.ToString("O"),
                IsActive = s.IsActive
            })
            .ToListAsync();
    }

    public async Task<List<ClientComplaintListItemDto>> GetComplaintsAsync()
    {
        var customer = await ResolveLinkedCustomerAsync();
        if (customer == null)
        {
            return [];
        }

        return await _context.Set<ClientComplaint>()
            .AsNoTracking()
            .Where(c => c.CustomerId == customer.Id)
            .OrderByDescending(c => c.UpdatedAt)
            .Select(c => new ClientComplaintListItemDto
            {
                Id = c.Id,
                Reference = c.Reference,
                Category = c.Category,
                Summary = c.Summary,
                Status = c.Status,
                OwnerTeam = c.OwnerTeam,
                CreatedAt = c.CreatedAt.ToString("O"),
                UpdatedAt = c.UpdatedAt.ToString("O"),
                SlaDueAt = c.SlaDueAt.ToString("O")
            })
            .ToListAsync();
    }

    public async Task<ClientComplaintDetailDto?> GetComplaintAsync(string complaintId)
    {
        var customer = await ResolveLinkedCustomerAsync();
        if (customer == null)
        {
            return null;
        }

        var complaint = await _context.Set<ClientComplaint>()
            .AsNoTracking()
            .Include(c => c.Events)
            .Include(c => c.Attachments)
            .FirstOrDefaultAsync(c => c.Id == complaintId && c.CustomerId == customer.Id);

        if (complaint == null)
        {
            return null;
        }

        return MapComplaint(complaint);
    }

    public async Task<ClientComplaintDetailDto?> GetComplaintForOperationsAsync(string complaintId)
    {
        var complaint = await _context.Set<ClientComplaint>()
            .AsNoTracking()
            .Include(c => c.Events)
            .Include(c => c.Attachments)
            .FirstOrDefaultAsync(c => c.Id == complaintId);

        return complaint == null ? null : MapComplaint(complaint);
    }

    public async Task<ClientComplaintDetailDto?> CreateComplaintAsync(CreateClientComplaintRequest request)
    {
        var customer = await ResolveLinkedCustomerAsync();
        if (customer == null)
        {
            return null;
        }

        var complaint = new ClientComplaint
        {
            Id = $"CMP-{Guid.NewGuid():N}".Substring(0, 16).ToUpperInvariant(),
            Reference = $"BI-{DateTime.UtcNow:yyyy}-{Random.Shared.Next(10000, 99999)}",
            CustomerId = customer.Id,
            SubmittedByUserId = _currentUser.UserId,
            Category = request.Category.Trim(),
            Summary = request.Summary.Trim(),
            Details = request.Details.Trim(),
            Status = "ACKNOWLEDGED",
            OwnerTeam = "Customer Operations",
            SlaDueAt = DateTime.UtcNow.AddDays(2),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var createdEvent = new ClientComplaintEvent
        {
            Id = $"CME-{Guid.NewGuid():N}".Substring(0, 16).ToUpperInvariant(),
            ComplaintId = complaint.Id,
            EventType = "CREATED",
            Title = "Complaint acknowledged",
            Description = "Your complaint has been recorded and routed to Customer Operations for review.",
            Visibility = "CUSTOMER",
            ActorId = _currentUser.UserId,
            ActorName = (await GetIdentityAsync()).Name,
            CreatedAt = DateTime.UtcNow
        };

        var reviewEvent = new ClientComplaintEvent
        {
            Id = $"CME-{Guid.NewGuid():N}".Substring(0, 16).ToUpperInvariant(),
            ComplaintId = complaint.Id,
            EventType = "STATUS_UPDATE",
            Title = "Under review",
            Description = "A case handler will provide the next update within the published complaint handling SLA.",
            Visibility = "CUSTOMER",
            ActorName = "Customer Operations",
            CreatedAt = DateTime.UtcNow
        };

        complaint.Events.Add(createdEvent);
        complaint.Events.Add(reviewEvent);

        _context.Add(complaint);
        await _context.SaveChangesAsync();

        await _auditLoggingService.LogActionAsync(
            "CLIENT_COMPLAINT_CREATED",
            "CLIENT_COMPLAINT",
            complaint.Id,
            _currentUser.UserId,
            $"Client complaint {complaint.Reference} created for customer {customer.Id}.",
            _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
            _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString(),
            "SUCCESS",
            newValues: new
            {
                complaint.Reference,
                complaint.Category,
                complaint.Status,
                complaint.OwnerTeam
            });

        return await GetComplaintAsync(complaint.Id);
    }

    public async Task<ClientComplaintAttachmentDto?> AddComplaintAttachmentAsync(string complaintId, UploadClientComplaintAttachmentRequest request)
    {
        var customer = await ResolveLinkedCustomerAsync();
        if (customer == null)
        {
            return null;
        }

        var complaint = await _context.Set<ClientComplaint>()
            .Include(c => c.Attachments)
            .Include(c => c.Events)
            .FirstOrDefaultAsync(c => c.Id == complaintId && c.CustomerId == customer.Id);
        if (complaint == null)
        {
            return null;
        }

        var normalizedDataUrl = request.DataUrl?.Trim() ?? string.Empty;
        var normalizedContentType = string.IsNullOrWhiteSpace(request.ContentType)
            ? "application/octet-stream"
            : request.ContentType.Trim().ToLowerInvariant();
        ValidateUploadedFile(
            request.FileName,
            normalizedContentType,
            normalizedDataUrl,
            AllowedComplaintAttachmentContentTypes,
            MaxComplaintAttachmentBytes,
            "Complaint evidence must be a PNG, JPEG, WEBP image, or PDF document.");

        var storedFile = await _clientFileStorageService.StoreAsync("complaint-attachments", request.FileName.Trim(), normalizedContentType, normalizedDataUrl);

        var attachment = new ClientComplaintAttachment
        {
            Id = $"CPA-{Guid.NewGuid():N}".Substring(0, 16).ToUpperInvariant(),
            ComplaintId = complaint.Id,
            FileName = request.FileName.Trim(),
            ContentType = normalizedContentType,
            DataUrl = storedFile.StorageReference,
            UploadedBy = _currentUser.Email,
            Status = "PENDING_SCAN",
            UploadedAt = DateTime.UtcNow
        };

        complaint.Attachments.Add(attachment);
        complaint.UpdatedAt = DateTime.UtcNow;
        complaint.Events.Add(new ClientComplaintEvent
        {
            Id = $"CME-{Guid.NewGuid():N}".Substring(0, 16).ToUpperInvariant(),
            ComplaintId = complaint.Id,
            EventType = "ATTACHMENT_ADDED",
            Title = "Evidence added",
            Description = $"{attachment.FileName} was attached to this complaint and queued for review.",
            Visibility = "CUSTOMER",
            ActorId = _currentUser.UserId,
            ActorName = _currentUser.Email,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        return new ClientComplaintAttachmentDto
        {
            Id = attachment.Id,
            FileName = attachment.FileName,
            ContentType = attachment.ContentType,
            ContentUrl = BuildComplaintAttachmentContentUrl(attachment),
            Status = attachment.Status,
            UploadedAt = attachment.UploadedAt.ToString("O")
        };
    }

    public async Task<List<StaffComplaintQueueItemDto>> GetComplaintQueueAsync(string? status)
    {
        var normalizedStatus = string.IsNullOrWhiteSpace(status) ? null : status.Trim().ToUpperInvariant();
        var now = DateTime.UtcNow;

        return await _context.Set<ClientComplaint>()
            .AsNoTracking()
            .Include(c => c.Attachments)
            .Include(c => c.Events)
            .Include(c => c.Customer)
            .Where(c => normalizedStatus == null || c.Status == normalizedStatus)
            .OrderByDescending(c => c.UpdatedAt)
            .Select(c => new StaffComplaintQueueItemDto
            {
                Id = c.Id,
                Reference = c.Reference,
                CustomerId = c.CustomerId,
                CustomerName = c.Customer != null ? c.Customer.Name : null,
                Category = c.Category,
                Summary = c.Summary,
                Status = c.Status,
                OwnerTeam = c.OwnerTeam,
                CreatedAt = c.CreatedAt.ToString("O"),
                UpdatedAt = c.UpdatedAt.ToString("O"),
                SlaDueAt = c.SlaDueAt.ToString("O"),
                AttachmentCount = c.Attachments.Count,
                EventCount = c.Events.Count,
                IsSlaBreached = c.SlaDueAt < now && c.ClosedAt == null && c.Status != "CLOSED",
                SlaHoursRemaining = c.ClosedAt != null || c.Status == "CLOSED"
                    ? 0
                    : (int)Math.Ceiling((c.SlaDueAt - now).TotalHours)
            })
            .ToListAsync();
    }

    public async Task<ComplaintQueueSummaryDto> GetComplaintQueueSummaryAsync()
    {
        var now = DateTime.UtcNow;
        var complaints = await _context.Set<ClientComplaint>()
            .AsNoTracking()
            .Where(c => c.ClosedAt == null && c.Status != "CLOSED")
            .ToListAsync();

        return new ComplaintQueueSummaryDto
        {
            TotalOpen = complaints.Count,
            TotalBreached = complaints.Count(c => c.SlaDueAt < now),
            DueWithin24Hours = complaints.Count(c => c.SlaDueAt >= now && c.SlaDueAt <= now.AddHours(24)),
            AwaitingCustomerInput = complaints.Count(c => c.Status == "AWAITING_CUSTOMER_INPUT"),
            UnderReview = complaints.Count(c => c.Status == "UNDER_REVIEW"),
            Escalated = complaints.Count(c => c.Status == "ESCALATED")
        };
    }

    public async Task<ClientComplaintDetailDto?> TriageComplaintAsync(string complaintId, TriageClientComplaintRequest request)
    {
        var complaint = await _context.Set<ClientComplaint>()
            .Include(c => c.Events)
            .Include(c => c.Attachments)
            .FirstOrDefaultAsync(c => c.Id == complaintId);
        if (complaint == null)
        {
            return null;
        }

        complaint.OwnerTeam = request.OwnerTeam.Trim();
        complaint.Status = request.Status.Trim().ToUpperInvariant();
        complaint.UpdatedAt = DateTime.UtcNow;
        if (complaint.Status == "AWAITING_CUSTOMER_INPUT")
        {
            complaint.SlaDueAt = DateTime.UtcNow.AddDays(1);
        }
        complaint.Events.Add(new ClientComplaintEvent
        {
            Id = $"CME-{Guid.NewGuid():N}".Substring(0, 16).ToUpperInvariant(),
            ComplaintId = complaint.Id,
            EventType = "TRIAGE",
            Title = $"Assigned to {complaint.OwnerTeam}",
            Description = request.Note.Trim(),
            Visibility = "CUSTOMER",
            ActorId = _currentUser.UserId,
            ActorName = _currentUser.Email,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return MapComplaint(complaint);
    }

    public async Task<ClientComplaintDetailDto?> EscalateComplaintAsync(string complaintId, EscalateClientComplaintRequest request)
    {
        var complaint = await _context.Set<ClientComplaint>()
            .Include(c => c.Events)
            .Include(c => c.Attachments)
            .FirstOrDefaultAsync(c => c.Id == complaintId);
        if (complaint == null)
        {
            return null;
        }

        var previousTeam = complaint.OwnerTeam;
        complaint.OwnerTeam = request.EscalationTeam.Trim();
        complaint.Status = "ESCALATED";
        complaint.UpdatedAt = DateTime.UtcNow;
        if (request.ResetSlaWindow)
        {
            complaint.SlaDueAt = DateTime.UtcNow.AddDays(1);
        }

        complaint.Events.Add(new ClientComplaintEvent
        {
            Id = $"CME-{Guid.NewGuid():N}".Substring(0, 16).ToUpperInvariant(),
            ComplaintId = complaint.Id,
            EventType = "ESCALATED",
            Title = $"Escalated to {complaint.OwnerTeam}",
            Description = request.Reason.Trim(),
            Visibility = "CUSTOMER",
            ActorId = _currentUser.UserId,
            ActorName = _currentUser.Email,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        await _auditLoggingService.LogActionAsync(
            "CLIENT_COMPLAINT_ESCALATED",
            "CLIENT_COMPLAINT",
            complaint.Id,
            _currentUser.UserId,
            $"Client complaint {complaint.Reference} escalated from {previousTeam} to {complaint.OwnerTeam}.",
            _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
            _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString(),
            "SUCCESS",
            newValues: new
            {
                complaint.Reference,
                PreviousOwnerTeam = previousTeam,
                complaint.OwnerTeam,
                complaint.Status,
                complaint.SlaDueAt
            });

        return MapComplaint(complaint);
    }

    public async Task<ComplaintSlaProcessingResultDto> ProcessComplaintSlaBreachesAsync()
    {
        var now = DateTime.UtcNow;
        var openStatuses = new[] { "ACKNOWLEDGED", "UNDER_REVIEW", "REOPENED", "AWAITING_CUSTOMER_INPUT" };
        var complaints = await _context.Set<ClientComplaint>()
            .Include(c => c.Events)
            .Where(c => c.ClosedAt == null && openStatuses.Contains(c.Status) && c.SlaDueAt < now)
            .ToListAsync();

        foreach (var complaint in complaints)
        {
            complaint.Status = "ESCALATED";
            complaint.OwnerTeam = complaint.OwnerTeam == "Customer Operations"
                ? "Complaint Escalations"
                : complaint.OwnerTeam;
            complaint.UpdatedAt = now;
            complaint.Events.Add(new ClientComplaintEvent
            {
                Id = $"CME-{Guid.NewGuid():N}".Substring(0, 16).ToUpperInvariant(),
                ComplaintId = complaint.Id,
                EventType = "SLA_BREACHED",
                Title = "Complaint SLA breached",
                Description = "This complaint exceeded its handling SLA and has been escalated for priority review.",
                Visibility = "CUSTOMER",
                ActorName = "Complaint SLA Monitor",
                CreatedAt = now
            });
        }

        if (complaints.Count > 0)
        {
            await _context.SaveChangesAsync();
        }

        return new ComplaintSlaProcessingResultDto
        {
            ProcessedCount = complaints.Count,
            BreachedCount = complaints.Count,
            EscalatedCount = complaints.Count,
            ComplaintIds = complaints.Select(c => c.Id).ToList()
        };
    }

    public async Task<ClientComplaintDetailDto?> CloseComplaintAsync(string complaintId, CloseClientComplaintRequest request)
    {
        var complaint = await _context.Set<ClientComplaint>()
            .Include(c => c.Events)
            .Include(c => c.Attachments)
            .FirstOrDefaultAsync(c => c.Id == complaintId);
        if (complaint == null)
        {
            return null;
        }

        complaint.Status = "CLOSED";
        complaint.ClosedAt = DateTime.UtcNow;
        complaint.UpdatedAt = DateTime.UtcNow;
        complaint.Events.Add(new ClientComplaintEvent
        {
            Id = $"CME-{Guid.NewGuid():N}".Substring(0, 16).ToUpperInvariant(),
            ComplaintId = complaint.Id,
            EventType = "CLOSED",
            Title = $"Complaint resolved ({request.ResolutionCode.Trim().ToUpperInvariant()})",
            Description = request.ResolutionNote.Trim(),
            Visibility = "CUSTOMER",
            ActorId = _currentUser.UserId,
            ActorName = _currentUser.Email,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return MapComplaint(complaint);
    }

    public async Task<ClientComplaintDetailDto?> ReopenComplaintAsync(string complaintId, ReopenClientComplaintRequest request)
    {
        var customer = await ResolveLinkedCustomerAsync();
        if (customer == null)
        {
            return null;
        }

        var complaint = await _context.Set<ClientComplaint>()
            .Include(c => c.Events)
            .Include(c => c.Attachments)
            .FirstOrDefaultAsync(c => c.Id == complaintId && c.CustomerId == customer.Id);
        if (complaint == null)
        {
            return null;
        }

        complaint.Status = "REOPENED";
        complaint.ClosedAt = null;
        complaint.UpdatedAt = DateTime.UtcNow;
        complaint.SlaDueAt = DateTime.UtcNow.AddDays(2);
        complaint.Events.Add(new ClientComplaintEvent
        {
            Id = $"CME-{Guid.NewGuid():N}".Substring(0, 16).ToUpperInvariant(),
            ComplaintId = complaint.Id,
            EventType = "REOPENED",
            Title = "Complaint reopened",
            Description = request.Reason.Trim(),
            Visibility = "CUSTOMER",
            ActorId = _currentUser.UserId,
            ActorName = _currentUser.Email,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return MapComplaint(complaint);
    }

    public async Task<CustomerProfileResponse?> UpdateLinkedProfileAsync(UpdateClientProfileRequest request)
    {
        var customer = await ResolveLinkedCustomerAsync();
        if (customer == null)
        {
            return null;
        }

        var stepUpValid = await _clientAuthService.ConsumeStepUpTokenAsync(_currentUser.UserId, request.StepUpToken, "PROFILE_UPDATE");
        if (!stepUpValid)
        {
            throw new InvalidOperationException("A valid step-up verification is required before updating profile details.");
        }

        var updated = await _customerService.UpdateCustomerAsync(customer.Id, new UpdateCustomerRequest
        {
            Name = string.IsNullOrWhiteSpace(request.Name) ? customer.Name : request.Name.Trim(),
            Email = request.Email ?? customer.Email,
            Phone = request.Phone ?? customer.Phone,
            DigitalAddress = request.DigitalAddress ?? customer.DigitalAddress
        });

        return updated == null ? null : await _customerService.GetCustomerProfileAsync(updated.Id);
    }

    public async Task<CustomerMediaDto?> UploadLinkedProfileMediaAsync(UploadClientProfileMediaRequest request)
    {
        var customer = await ResolveLinkedCustomerAsync();
        if (customer == null)
        {
            return null;
        }

        var stepUpValid = await _clientAuthService.ConsumeStepUpTokenAsync(_currentUser.UserId, request.StepUpToken, "PROFILE_MEDIA_UPLOAD");
        if (!stepUpValid)
        {
            throw new InvalidOperationException("A valid step-up verification is required before uploading profile media.");
        }

        return await _customerService.UploadCustomerMediaAsync(customer.Id, new UploadCustomerMediaRequest
        {
            MediaType = request.MediaType,
            MediaSide = request.MediaSide,
            FileName = request.FileName,
            ContentType = request.ContentType,
            DataUrl = request.DataUrl
        });
    }

    public async Task<ClientKycOverviewDto?> GetKycOverviewAsync()
    {
        var customer = await ResolveLinkedCustomerAsync();
        if (customer == null)
        {
            return null;
        }

        CustomerKycReadinessDto? readiness;
        List<ClientKycCase> kycCases;

        try
        {
            readiness = await _customerService.GetCustomerKycReadinessAsync(customer.Id);
            kycCases = await _context.Set<ClientKycCase>()
                .AsNoTracking()
                .Include(c => c.Events)
                .Where(c => c.CustomerId == customer.Id)
                .OrderByDescending(c => c.UpdatedAt)
                .ToListAsync();
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedColumn)
        {
            readiness = new CustomerKycReadinessDto
            {
                IsReadyForAccountOpening = false,
                IsReadyForLoanOrigination = false,
                MissingRequirements = ["KYC media and review schema still syncing in this environment."],
                Checklist = []
            };
            kycCases = [];
        }

        return new ClientKycOverviewDto
        {
            CustomerId = customer.Id,
            KycLevel = customer.KycLevel,
            Readiness = MapKycReadiness(readiness),
            Cases = kycCases.Select(MapKycCase).ToList()
        };
    }

    public async Task<ClientKycCaseDto?> SubmitKycRefreshCaseAsync(SubmitClientKycRefreshRequest request)
    {
        var customer = await ResolveLinkedCustomerAsync();
        if (customer == null)
        {
            return null;
        }

        var stepUpValid = await _clientAuthService.ConsumeStepUpTokenAsync(_currentUser.UserId, request.StepUpToken, "KYC_REFRESH");
        if (!stepUpValid)
        {
            throw new InvalidOperationException("A valid step-up verification is required before submitting a KYC refresh.");
        }

        var existingOpenCase = await _context.Set<ClientKycCase>()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CustomerId == customer.Id && c.Status != "APPROVED" && c.Status != "REJECTED");
        if (existingOpenCase != null)
        {
            throw new InvalidOperationException("A KYC refresh case is already in progress for this customer.");
        }

        var kycCase = new ClientKycCase
        {
            Id = $"KYC-{Guid.NewGuid():N}".Substring(0, 16).ToUpperInvariant(),
            Reference = $"KYC-{DateTime.UtcNow:yyyy}-{Random.Shared.Next(10000, 99999)}",
            CustomerId = customer.Id,
            Status = "SUBMITTED",
            Reason = request.Reason.Trim(),
            Summary = request.Summary.Trim(),
            SubmittedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        kycCase.Events.Add(new ClientKycCaseEvent
        {
            Id = $"KYE-{Guid.NewGuid():N}".Substring(0, 16).ToUpperInvariant(),
            KycCaseId = kycCase.Id,
            EventType = "SUBMITTED",
            Title = "KYC refresh submitted",
            Description = "Your KYC refresh request has been recorded and routed for review.",
            ActorId = _currentUser.UserId,
            ActorName = _currentUser.Email,
            CreatedAt = DateTime.UtcNow
        });

        _context.Add(kycCase);
        await _context.SaveChangesAsync();

        await _auditLoggingService.LogActionAsync(
            "CLIENT_KYC_REFRESH_SUBMITTED",
            "CLIENT_KYC_CASE",
            kycCase.Id,
            _currentUser.UserId,
            $"Client KYC refresh case {kycCase.Reference} created for customer {customer.Id}.",
            _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
            _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString(),
            "SUCCESS",
            newValues: new
            {
                kycCase.Reference,
                kycCase.Status,
                kycCase.Reason
            });

        return MapKycCase(kycCase);
    }

    public async Task<List<ClientKycCaseDto>> GetKycCaseQueueAsync(string? status)
    {
        var normalizedStatus = string.IsNullOrWhiteSpace(status) ? null : status.Trim().ToUpperInvariant();
        var kycCases = await _context.Set<ClientKycCase>()
            .AsNoTracking()
            .Include(c => c.Customer)
            .Include(c => c.Events)
            .Where(c => normalizedStatus == null || c.Status == normalizedStatus)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync();

        return kycCases.Select(MapKycCase).ToList();
    }

    public async Task<ClientKycCaseDto?> ReviewKycCaseAsync(string kycCaseId, ReviewClientKycCaseRequest request)
    {
        var kycCase = await _context.Set<ClientKycCase>()
            .Include(c => c.Customer)
            .Include(c => c.Events)
            .FirstOrDefaultAsync(c => c.Id == kycCaseId);
        if (kycCase == null)
        {
            return null;
        }

        var decision = request.Decision.Trim().ToUpperInvariant();
        if (decision is not ("UNDER_REVIEW" or "APPROVED" or "REJECTED"))
        {
            throw new InvalidOperationException("Decision must be UNDER_REVIEW, APPROVED, or REJECTED.");
        }

        kycCase.Status = decision;
        kycCase.ReviewerUserId = _currentUser.UserId;
        kycCase.ReviewerName = _currentUser.Email;
        kycCase.DecisionNote = request.Note.Trim();
        kycCase.ReviewedAt = DateTime.UtcNow;
        kycCase.UpdatedAt = DateTime.UtcNow;
        kycCase.Events.Add(new ClientKycCaseEvent
        {
            Id = $"KYE-{Guid.NewGuid():N}".Substring(0, 16).ToUpperInvariant(),
            KycCaseId = kycCase.Id,
            EventType = decision,
            Title = decision switch
            {
                "APPROVED" => "KYC refresh approved",
                "REJECTED" => "KYC refresh needs correction",
                _ => "KYC refresh under review"
            },
            Description = kycCase.DecisionNote,
            ActorId = _currentUser.UserId,
            ActorName = _currentUser.Email,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return MapKycCase(kycCase);
    }

    public async Task<List<ClientStatementSummaryDto>> GetStatementSummariesAsync()
    {
        var customer = await ResolveLinkedCustomerAsync();
        if (customer == null)
        {
            return [];
        }

        return await _context.Transactions
            .AsNoTracking()
            .Where(t => t.Account != null && t.Account.CustomerId == customer.Id && t.Status == "POSTED")
            .GroupBy(t => new { t.AccountId, Year = t.Date.Year, Month = t.Date.Month })
            .OrderByDescending(g => g.Key.Year)
            .ThenByDescending(g => g.Key.Month)
            .Select(g => new ClientStatementSummaryDto
            {
                StatementId = $"{g.Key.AccountId}:{g.Key.Year:D4}-{g.Key.Month:D2}",
                AccountId = g.Key.AccountId ?? string.Empty,
                PeriodLabel = $"{g.Key.Year:D4}-{g.Key.Month:D2}",
                Year = g.Key.Year,
                Month = g.Key.Month,
                EntryCount = g.Count(),
                TotalDebits = g.Where(t => t.Amount < 0).Sum(t => Math.Abs(t.Amount)),
                TotalCredits = g.Where(t => t.Amount >= 0).Sum(t => t.Amount),
                GeneratedAt = DateTime.UtcNow.ToString("O")
            })
            .ToListAsync();
    }

    public async Task<ClientStatementDetailDto?> GetStatementDetailAsync(string accountId, int year, int month)
    {
        var customer = await ResolveLinkedCustomerAsync();
        if (customer == null)
        {
            return null;
        }

        var account = await _context.Accounts.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == accountId && a.CustomerId == customer.Id);
        if (account == null)
        {
            return null;
        }

        var periodStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodEnd = periodStart.AddMonths(1);

        var entries = await _context.Transactions.AsNoTracking()
            .Where(t => t.AccountId == accountId && t.Status == "POSTED" && t.Date >= periodStart && t.Date < periodEnd)
            .OrderBy(t => t.Date)
            .Select(t => new ClientStatementEntryDto
            {
                Id = t.Id,
                Type = t.Type,
                Amount = t.Amount,
                Narration = t.Narration,
                Reference = t.Reference,
                Date = t.Date.ToString("O")
            })
            .ToListAsync();

        var openingBalance = await _context.Transactions.AsNoTracking()
            .Where(t => t.AccountId == accountId && t.Status == "POSTED" && t.Date < periodStart)
            .SumAsync(t => (decimal?)t.Amount) ?? 0m;

        return new ClientStatementDetailDto
        {
            StatementId = $"{accountId}:{year:D4}-{month:D2}",
            AccountId = accountId,
            PeriodLabel = $"{year:D4}-{month:D2}",
            Currency = account.Currency,
            OpeningBalance = openingBalance,
            ClosingBalance = openingBalance + entries.Sum(e => e.Amount),
            TotalCredits = entries.Where(e => e.Amount >= 0).Sum(e => e.Amount),
            TotalDebits = entries.Where(e => e.Amount < 0).Sum(e => Math.Abs(e.Amount)),
            Entries = entries
        };
    }

    public async Task<ClientStatementExportDto?> ExportStatementAsync(string accountId, int year, int month, string? format)
    {
        var statement = await GetStatementDetailAsync(accountId, year, month);
        if (statement == null)
        {
            return null;
        }

        var normalizedFormat = string.IsNullOrWhiteSpace(format)
            ? "csv"
            : format.Trim().ToLowerInvariant();

        if (normalizedFormat != "csv")
        {
            throw new InvalidOperationException("Only CSV statement export is currently supported.");
        }

        var csv = BuildStatementCsv(statement);
        var bytes = Encoding.UTF8.GetBytes(csv);
        var checksumBytes = SHA256.HashData(bytes);
        var checksum = Convert.ToHexString(checksumBytes);
        var fileName = $"bankinsight-statement-{statement.AccountId}-{statement.PeriodLabel}.csv";

        await _auditLoggingService.LogActionAsync(
            "CLIENT_STATEMENT_EXPORTED",
            "CLIENT_STATEMENT",
            statement.StatementId,
            _currentUser.UserId,
            $"Client statement export generated for account {statement.AccountId} period {statement.PeriodLabel}.",
            _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
            _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString(),
            "SUCCESS",
            newValues: new
            {
                statement.AccountId,
                statement.PeriodLabel,
                Format = normalizedFormat,
                LineCount = statement.Entries.Count
            });

        return new ClientStatementExportDto
        {
            StatementId = statement.StatementId,
            AccountId = statement.AccountId,
            PeriodLabel = statement.PeriodLabel,
            FileName = fileName,
            ContentType = "text/csv",
            ExportedAt = DateTime.UtcNow.ToString("O"),
            ChecksumSha256 = checksum,
            LineCount = statement.Entries.Count + 1,
            ContentBase64 = Convert.ToBase64String(bytes)
        };
    }

    private async Task<ClientIdentityDto> GetIdentityAsync()
    {
        var identity = await _clientAuthService.GetCurrentUserAsync(_currentUser.UserId);
        if (identity != null)
        {
            return identity;
        }

        var principal = _httpContextAccessor.HttpContext?.User;
        return new ClientIdentityDto
        {
            UserId = _currentUser.UserId,
            CustomerId = principal?.FindFirst("customer_id")?.Value,
            Name = principal?.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty,
            Email = _currentUser.Email,
            Role = principal?.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty,
            Permissions = _currentUser.Permissions.ToArray()
        };
    }

    private async Task<Customer?> ResolveLinkedCustomerAsync()
    {
        var customerId = _httpContextAccessor.HttpContext?.User.FindFirst("customer_id")?.Value;
        if (string.IsNullOrWhiteSpace(customerId))
        {
            return null;
        }

        return await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == customerId);
    }

    private static ClientLinkedCustomerDto MapLinkedCustomer(Customer customer)
    {
        return new ClientLinkedCustomerDto
        {
            Id = customer.Id,
            Name = customer.Name,
            Email = customer.Email,
            Phone = customer.Phone,
            KycLevel = customer.KycLevel,
            RiskRating = customer.RiskRating
        };
    }

    private ClientComplaintDetailDto MapComplaint(ClientComplaint complaint)
    {
        return new ClientComplaintDetailDto
        {
            Id = complaint.Id,
            Reference = complaint.Reference,
            Category = complaint.Category,
            Summary = complaint.Summary,
            Details = complaint.Details,
            Status = complaint.Status,
            OwnerTeam = complaint.OwnerTeam,
            CreatedAt = complaint.CreatedAt.ToString("O"),
            UpdatedAt = complaint.UpdatedAt.ToString("O"),
            SlaDueAt = complaint.SlaDueAt.ToString("O"),
            ClosedAt = complaint.ClosedAt?.ToString("O"),
            Events = complaint.Events
                .OrderBy(e => e.CreatedAt)
                .Select(e => new ClientComplaintEventDto
                {
                    Id = e.Id,
                    EventType = e.EventType,
                    Title = e.Title,
                    Description = e.Description,
                    Visibility = e.Visibility,
                    ActorName = e.ActorName,
                    CreatedAt = e.CreatedAt.ToString("O")
                })
                .ToList(),
            Attachments = complaint.Attachments
                .OrderByDescending(a => a.UploadedAt)
                .Select(a => new ClientComplaintAttachmentDto
                {
                    Id = a.Id,
                    FileName = a.FileName,
                    ContentType = a.ContentType,
                    ContentUrl = BuildComplaintAttachmentContentUrl(a),
                    Status = a.Status,
                    UploadedAt = a.UploadedAt.ToString("O")
                })
                .ToList()
        };
    }

    private static string BuildComplaintAttachmentContentUrl(ClientComplaintAttachment attachment)
        => $"/api/client-files/complaint-attachments/{attachment.Id}";

    private static ClientKycCaseDto MapKycCase(ClientKycCase kycCase)
    {
        return new ClientKycCaseDto
        {
            Id = kycCase.Id,
            Reference = kycCase.Reference,
            CustomerId = kycCase.CustomerId,
            CustomerName = string.IsNullOrWhiteSpace(kycCase.Customer?.Name) ? kycCase.CustomerId : kycCase.Customer!.Name,
            Status = kycCase.Status,
            Reason = kycCase.Reason,
            Summary = kycCase.Summary,
            SubmittedAt = kycCase.SubmittedAt.ToString("O"),
            ReviewedAt = kycCase.ReviewedAt?.ToString("O"),
            ReviewerName = kycCase.ReviewerName,
            DecisionNote = kycCase.DecisionNote,
            Events = kycCase.Events
                .OrderBy(e => e.CreatedAt)
                .Select(e => new ClientKycCaseEventDto
                {
                    Id = e.Id,
                    EventType = e.EventType,
                    Title = e.Title,
                    Description = e.Description,
                    ActorName = e.ActorName,
                    CreatedAt = e.CreatedAt.ToString("O")
                })
                .ToList()
        };
    }

    private static ClientKycReadinessDto MapKycReadiness(CustomerKycReadinessDto? readiness)
    {
        if (readiness == null)
        {
            return new ClientKycReadinessDto();
        }

        return new ClientKycReadinessDto
        {
            IsReadyForAccountOpening = readiness.IsReadyForAccountOpening,
            IsReadyForLoanOrigination = readiness.IsReadyForLoanOrigination,
            MissingRequirements = readiness.MissingRequirements.ToList(),
            Checklist = readiness.Checklist
                .Select(item => new ClientKycChecklistItemDto
                {
                    Key = item.Key,
                    Label = item.Label,
                    IsSatisfied = item.IsSatisfied,
                    Detail = item.Detail
                })
                .ToList()
        };
    }

    private static ClientMerchantDto MapMerchantCatalogItem(ClientMerchantProfile profile)
    {
        return new ClientMerchantDto
        {
            Code = profile.MerchantCode,
            Name = profile.DisplayName,
            Category = profile.Category,
            SettlementType = "BANKINSIGHT_CUSTOMER_MERCHANT",
            Currency = profile.Currency,
            DestinationAccountId = profile.SettlementAccountId,
            MerchantKind = "BUSINESS_CUSTOMER",
            MerchantProfileId = profile.Id,
            SettlementCustomerId = profile.CustomerId,
            AcceptsQrPayments = profile.AcceptsAppPayments,
            QrScheme = profile.QrScheme
        };
    }

    private static ClientMerchantProfileDto MapMerchantProfile(ClientMerchantProfile profile)
    {
        return new ClientMerchantProfileDto
        {
            Id = profile.Id,
            CustomerId = profile.CustomerId,
            MerchantCode = profile.MerchantCode,
            DisplayName = profile.DisplayName,
            Category = profile.Category,
            SettlementAccountId = profile.SettlementAccountId,
            SettlementAccountLabel = $"{profile.SettlementAccount?.ProductCode ?? profile.SettlementAccount?.Type ?? "ACCOUNT"} - {profile.SettlementAccountId[^Math.Min(4, profile.SettlementAccountId.Length)..]}",
            Currency = profile.Currency,
            Status = profile.Status,
            QrScheme = profile.QrScheme,
            QrPayload = profile.QrPayload,
            AcceptsAppPayments = profile.AcceptsAppPayments,
            GhQrReady = profile.GhQrReady,
            CreatedAt = profile.CreatedAt.ToString("O"),
            LastPaymentAt = profile.LastPaymentAt?.ToString("O")
        };
    }

    private static bool IsBusinessCustomer(Customer customer)
        => string.Equals(customer.Type, "BUSINESS", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(customer.Type, "CORPORATE", StringComparison.OrdinalIgnoreCase);

    private async Task<string> GenerateMerchantCodeAsync(string displayName)
    {
        var baseCode = new string(displayName
            .ToUpperInvariant()
            .Where(char.IsLetterOrDigit)
            .Take(10)
            .ToArray());

        if (string.IsNullOrWhiteSpace(baseCode))
        {
            baseCode = "MERCHANT";
        }

        baseCode = $"BIZ-{baseCode}";
        var candidate = baseCode;
        var suffix = 1;

        while (await _context.Set<ClientMerchantProfile>().AnyAsync(profile => profile.MerchantCode == candidate))
        {
            candidate = $"{baseCode[..Math.Min(baseCode.Length, 10)]}{suffix:00}";
            suffix++;
        }

        return candidate;
    }

    private static string BuildMerchantQrPayload(string profileId, string merchantCode, string currency)
        => $"bankinsight://merchant-pay?scheme=BANKINSIGHT_QR&merchantCode={Uri.EscapeDataString(merchantCode)}&profileId={Uri.EscapeDataString(profileId)}&currency={Uri.EscapeDataString(currency)}";

    private async Task<ClientMerchantProfile> RequireMerchantProfileForQrPayloadAsync(string qrPayload)
    {
        var payload = qrPayload?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new InvalidOperationException("QR payload is required.");
        }

        if (!payload.StartsWith("bankinsight://merchant-pay?", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only BankInsight QR payloads are currently supported.");
        }

        var queryIndex = payload.IndexOf('?');
        var query = queryIndex >= 0 ? payload[(queryIndex + 1)..] : string.Empty;
        var values = query
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .Where(parts => parts.Length == 2)
            .ToDictionary(
                parts => Uri.UnescapeDataString(parts[0]),
                parts => Uri.UnescapeDataString(parts[1]),
                StringComparer.OrdinalIgnoreCase);

        if (!values.TryGetValue("scheme", out var scheme) ||
            !string.Equals(scheme, "BANKINSIGHT_QR", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("This QR code is not yet supported. GH-QR support is planned for a later release.");
        }

        values.TryGetValue("profileId", out var profileId);
        values.TryGetValue("merchantCode", out var merchantCode);

        var profile = await _context.Set<ClientMerchantProfile>()
            .AsNoTracking()
            .FirstOrDefaultAsync(item =>
                (profileId != null && item.Id == profileId) ||
                (merchantCode != null && item.MerchantCode == merchantCode));

        if (profile == null)
        {
            throw new InvalidOperationException("Merchant QR could not be resolved.");
        }

        return profile;
    }

    private async Task<Customer> RequireLinkedCustomerAsync()
    {
        return await ResolveLinkedCustomerAsync()
            ?? throw new InvalidOperationException("No linked customer profile was found for the signed-in identity.");
    }

    private async Task<Account> RequireOwnedAccountAsync(string customerId, string accountId)
    {
        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == accountId && a.CustomerId == customerId);
        if (account == null)
        {
            throw new InvalidOperationException("The selected account was not found for this customer.");
        }

        if (!string.Equals(account.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The selected account is not active.");
        }

        return account;
    }

    private async Task RequireOwnedLoanAsync(string loanId)
    {
        var customer = await RequireLinkedCustomerAsync();
        var exists = await _context.Loans.AnyAsync(l => l.Id == loanId && l.CustomerId == customer.Id);
        if (!exists)
        {
            throw new InvalidOperationException("Loan not found.");
        }
    }

    private async Task RequireStepUpAsync(string stepUpToken, string purpose)
    {
        var valid = await _clientAuthService.ConsumeStepUpTokenAsync(_currentUser.UserId, stepUpToken, purpose);
        if (!valid)
        {
            throw new InvalidOperationException("A valid step-up verification is required before completing this action.");
        }
    }

    private static DateTime CalculateNextRunAt(DateTime startDateUtc, string frequency)
    {
        var normalized = frequency.Trim().ToUpperInvariant();
        return normalized switch
        {
            "DAILY" => startDateUtc.AddDays(1),
            "WEEKLY" => startDateUtc.AddDays(7),
            _ => startDateUtc.AddMonths(1)
        };
    }

    private static ClientTransferResultDto MapTransferResult(LedgerPostingResult result)
    {
        return new ClientTransferResultDto
        {
            TransactionId = result.TransactionId,
            Reference = result.Reference,
            Narration = result.Narration,
            Amount = result.Amount,
            AppliedFees = result.AppliedFees,
            NetAmount = result.NetAmount,
            NewBalance = result.NewBalance,
            Status = result.Status,
            Message = result.Message
        };
    }

    private static void ValidateUploadedFile(
        string fileName,
        string contentType,
        string dataUrl,
        IEnumerable<string> allowedContentTypes,
        int maxBytes,
        string unsupportedTypeMessage)
    {
        if (string.IsNullOrWhiteSpace(fileName) ||
            fileName.Contains('/') ||
            fileName.Contains('\\') ||
            fileName.Contains("..", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("File name is invalid.");
        }

        if (!allowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(unsupportedTypeMessage);
        }

        var commaIndex = dataUrl.IndexOf(',');
        if (commaIndex <= 0 || commaIndex == dataUrl.Length - 1)
        {
            throw new InvalidOperationException("Uploaded file payload is malformed.");
        }

        var header = dataUrl[..commaIndex];
        if (!header.Contains(";base64", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Uploaded file payload must be base64 encoded.");
        }

        var payload = dataUrl[(commaIndex + 1)..];
        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(payload);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException("Uploaded file payload is not valid base64 content.");
        }

        if (bytes.Length == 0)
        {
            throw new InvalidOperationException("Uploaded file is empty.");
        }

        if (bytes.Length > maxBytes)
        {
            throw new InvalidOperationException($"Uploaded file exceeds the maximum allowed size of {maxBytes / (1024 * 1024)} MB.");
        }

        if (!header.Contains(contentType, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Uploaded file content type does not match the payload header.");
        }
    }

    private static string BuildStatementCsv(ClientStatementDetailDto statement)
    {
        var builder = new StringBuilder();
        builder.AppendLine("StatementId,AccountId,Period,Currency,OpeningBalance,ClosingBalance,TotalCredits,TotalDebits");
        builder.AppendLine(string.Join(",",
            EscapeCsv(statement.StatementId),
            EscapeCsv(statement.AccountId),
            EscapeCsv(statement.PeriodLabel),
            EscapeCsv(statement.Currency),
            statement.OpeningBalance.ToString("0.00"),
            statement.ClosingBalance.ToString("0.00"),
            statement.TotalCredits.ToString("0.00"),
            statement.TotalDebits.ToString("0.00")));
        builder.AppendLine();
        builder.AppendLine("Date,Reference,Type,Narration,Amount");

        foreach (var entry in statement.Entries)
        {
            builder.AppendLine(string.Join(",",
                EscapeCsv(entry.Date),
                EscapeCsv(entry.Reference),
                EscapeCsv(entry.Type),
                EscapeCsv(entry.Narration),
                entry.Amount.ToString("0.00")));
        }

        return builder.ToString();
    }

    private static string EscapeCsv(string? value)
    {
        var safeValue = value ?? string.Empty;
        if (safeValue.Contains(',') || safeValue.Contains('"') || safeValue.Contains('\n') || safeValue.Contains('\r'))
        {
            return $"\"{safeValue.Replace("\"", "\"\"")}\"";
        }

        return safeValue;
    }
}
