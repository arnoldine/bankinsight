using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Services;

public class LoanService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLoggingService _auditLoggingService;
    private readonly ISuspiciousActivityService _suspiciousActivityService;
    private readonly IKycService _kycService;
    private readonly ICreditBureauService _creditBureauService;
    private readonly ILoanAccountingPostingService _loanAccountingPostingService;
    private readonly ISequenceGeneratorService _sequenceService;
    private readonly Security.ICurrentUserContext _currentUser;
    private readonly IPostingEngine _postingEngine;

    public LoanService(
        ApplicationDbContext context,
        IAuditLoggingService auditLoggingService,
        ISuspiciousActivityService suspiciousActivityService,
        IKycService kycService,
        ICreditBureauService creditBureauService,
        ILoanAccountingPostingService loanAccountingPostingService,
        ISequenceGeneratorService sequenceService,
        Security.ICurrentUserContext currentUser,
        IPostingEngine postingEngine)
    {
        _context = context;
        _auditLoggingService = auditLoggingService;
        _suspiciousActivityService = suspiciousActivityService;
        _kycService = kycService;
        _creditBureauService = creditBureauService;
        _loanAccountingPostingService = loanAccountingPostingService;
        _sequenceService = sequenceService;
        _currentUser = currentUser;
        _postingEngine = postingEngine;
    }

    public async Task<List<LoanDto>> GetLoansAsync()
    {
        var query = _context.Loans.Include(l => l.Product).AsQueryable();
        if (_currentUser.ScopeType == AccessScopeType.BranchOnly && !string.IsNullOrEmpty(_currentUser.BranchId))
        {
            query = query.Where(l => l.BranchId == _currentUser.BranchId);
        }

        return await query
            .Select(l => new LoanDto
            {
                Id = l.Id,
                Cif = l.CustomerId ?? string.Empty,
                GroupId = l.GroupId,
                ProductCode = l.ProductCode,
                ProductName = l.Product != null ? l.Product.Name : (l.LoanProduct != null ? l.LoanProduct.Name : null),
                Principal = l.Principal,
                Rate = l.Rate,
                TermMonths = l.TermMonths,
                DisbursementDate = l.DisbursementDate,
                ParBucket = l.ParBucket,
                OutstandingBalance = l.OutstandingBalance,
                CollateralType = l.CollateralType,
                CollateralValue = l.CollateralValue,
                Status = l.Status,
                InterestMethod = l.InterestMethod,
                RepaymentFrequency = l.RepaymentFrequency,
                ScheduleType = l.ScheduleType,
                LoanProductId = l.LoanProductId
            })
            .ToListAsync();
    }

    public async Task<LoanDto> DisburseLoanAsync(DisburseLoanRequest request)
    {
        using var dbTransaction = await _context.Database.BeginTransactionAsync();

        try
        {
            if (string.IsNullOrWhiteSpace(request.Cif))
            {
                throw new InvalidOperationException("Cif is required for direct disbursement");
            }

            if (string.IsNullOrWhiteSpace(request.ProductCode))
            {
                throw new InvalidOperationException("ProductCode is required for direct disbursement");
            }

            if (!request.Principal.HasValue)
            {
                throw new InvalidOperationException("Principal is required for direct disbursement");
            }

            if (!request.Rate.HasValue)
            {
                throw new InvalidOperationException("Rate is required for direct disbursement");
            }

            if (!request.TermMonths.HasValue)
            {
                throw new InvalidOperationException("TermMonths is required for direct disbursement");
            }

            var principalAmount = request.Principal.Value;
            var rate = request.Rate.Value;
            var termMonths = request.TermMonths.Value;

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == request.Cif);
            if (customer == null)
            {
                throw new InvalidOperationException("Customer not found");
            }

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == request.ProductCode);
            if (product == null)
            {
                throw new InvalidOperationException("Loan product not found");
            }

            if (!string.Equals(product.Type, "LOAN", StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(product.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Product is not an active loan product");
            }

            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.CustomerId == request.Cif && (a.Status == "ACTIVE" || a.Status == "Active"));

            if (account == null)
            {
                throw new InvalidOperationException("Customer has no active account for disbursement");
            }

            var disbursementReference = string.IsNullOrWhiteSpace(request.ClientReference)
                ? $"DSB-{DateTime.UtcNow:yyyyMMddHHmmss}-{RandomNumberGenerator.GetInt32(1000, 9999)}"
                : $"DSB-{request.ClientReference.Trim()}";

            if (!string.IsNullOrWhiteSpace(request.ClientReference))
            {
                var existingEvent = await _context.FinancialEvents.FirstOrDefaultAsync(e =>
                    e.Reference == disbursementReference &&
                    e.EventType == EventTypes.LoanDisbursed &&
                    e.EntityType == "LOAN");

                if (existingEvent != null)
                {
                    var existingLoan = await _context.Loans.Include(l => l.Product).FirstOrDefaultAsync(l => l.Id == existingEvent.EntityId);
                    if (existingLoan != null)
                    {
                        return MapLoanDto(existingLoan);
                    }
                }
            }

            var branchCode = "001"; // Fallback branch code
            var loanProductCode = request.ProductCode.PadLeft(2, '0');
            if (loanProductCode.Length > 2) loanProductCode = loanProductCode.Substring(0, 2);
            var yy = DateTime.UtcNow.ToString("yy");
            
            var prefix = $"LNO-{branchCode}-{loanProductCode}-{yy}";
            var seq = await _sequenceService.GetNextSequenceAsync(prefix);
            var loanId = $"{prefix}-{seq:D4}";
            var disbursementDate = DateOnly.FromDateTime(DateTime.UtcNow);

            var loan = new Loan
            {
                Id = loanId,
                CustomerId = request.Cif,
                GroupId = request.GroupId,
                ProductCode = request.ProductCode,
                Principal = principalAmount,
                Rate = rate,
                TermMonths = termMonths,
                DisbursementDate = disbursementDate,
                Status = "ACTIVE",
                OutstandingBalance = principalAmount,
                CollateralType = request.CollateralType,
                CollateralValue = request.CollateralValue,
                ParBucket = "0"
            };

            _context.Loans.Add(loan);

            var disbursementEvent = new FinancialEvent
            {
                EventType = EventTypes.LoanDisbursed,
                EntityType = "LOAN",
                EntityId = loanId,
                Amount = principalAmount,
                Currency = ResolveCurrency(product.Currency, account.Currency),
                BranchId = branchCode,
                Reference = disbursementReference,
                PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { AccountId = account.Id, Rate = rate }),
                CreatedBy = null
            };

            var postResult = await _postingEngine.ProcessEventAsync(disbursementEvent);
            if (!postResult.Success)
            {
                throw new InvalidOperationException($"Posting Engine Failed: {postResult.ErrorMessage}");
            }

            var remainingPrincipal = principalAmount;
            var monthlyPrincipal = Math.Round(principalAmount / termMonths, 2, MidpointRounding.AwayFromZero);

            for (int period = 1; period <= termMonths; period++)
            {
                var openingBalance = remainingPrincipal;
                var interest = Math.Round(openingBalance * (rate / 100m) / 12m, 2, MidpointRounding.AwayFromZero);
                var principal = period == termMonths
                    ? remainingPrincipal
                    : Math.Min(monthlyPrincipal, remainingPrincipal);

                principal = Math.Round(principal, 2, MidpointRounding.AwayFromZero);
                remainingPrincipal = Math.Max(0m, Math.Round(remainingPrincipal - principal, 2, MidpointRounding.AwayFromZero));

                var schedule = new LoanSchedule
                {
                    LoanId = loanId,
                    Period = period,
                    DueDate = disbursementDate.AddMonths(period),
                    Principal = principal,
                    Interest = interest,
                    Total = principal + interest,
                    Balance = remainingPrincipal,
                    Status = "DUE"
                };

                _context.LoanSchedules.Add(schedule);
            }

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            await dbTransaction.DisposeAsync();

            await _loanAccountingPostingService.PostEventAsync(
                loan,
                LoanAccountingEventType.Disbursement,
                principalAmount,
                null,
                $"Loan disbursement posting for {loan.Id}");

            await _auditLoggingService.LogActionAsync(
                action: "LOAN_DISBURSED",
                entityType: "LOAN",
                entityId: loan.Id,
                userId: null,
                description: $"Loan disbursed for customer {request.Cif} with principal {principalAmount:C}",
                status: "SUCCESS",
                newValues: new { loan.Id, request.Cif, Principal = principalAmount, Rate = rate, TermMonths = termMonths, disbursementReference });

            await _suspiciousActivityService.HandleLargeTransactionAsync(account.Id, principalAmount, "LOAN_DISBURSEMENT", null);

            loan.Product = product;
            return MapLoanDto(loan);
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();

            await _auditLoggingService.LogActionAsync(
                action: "LOAN_DISBURSE_FAILED",
                entityType: "LOAN",
                entityId: null,
                userId: null,
                description: $"Failed loan disbursement for customer {request.Cif}",
                status: "FAILED",
                errorMessage: ex.Message,
                newValues: new { request.Cif, request.ProductCode, request.Principal, request.ClientReference });

            throw;
        }
    }

    public async Task<List<LoanScheduleDto>> GetLoanScheduleAsync(string loanId)
    {
        return await _context.LoanSchedules
            .Where(s => s.LoanId == loanId)
            .OrderBy(s => s.Period)
            .Select(s => new LoanScheduleDto
            {
                Period = s.Period ?? 0,
                DueDate = s.DueDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
                Principal = s.Principal ?? 0,
                Interest = s.Interest ?? 0,
                Total = s.Total ?? 0,
                Balance = s.Balance ?? 0,
                Status = s.Status ?? "DUE"
            })
            .ToListAsync();
    }

    public async Task<LoanAccrualSnapshotDto> GetLoanAccrualSnapshotAsync(string loanId)
    {
        var loan = await _context.Loans
            .Include(l => l.Schedules)
            .FirstOrDefaultAsync(l => l.Id == loanId);

        if (loan == null)
        {
            throw new InvalidOperationException("Loan not found");
        }

        var asOf = DateOnly.FromDateTime(DateTime.UtcNow);
        var disbursementDate = loan.DisbursementDate ?? asOf;
        var daysOnBook = Math.Max(0, asOf.DayNumber - disbursementDate.DayNumber);
        var outstandingPrincipal = Math.Max(0m, loan.OutstandingBalance ?? 0m);
        var dailyAccruedInterest = Math.Round(outstandingPrincipal * (loan.Rate / 100m) / 365m, 2, MidpointRounding.AwayFromZero);
        var accruedInterestToDate = Math.Round(dailyAccruedInterest * daysOnBook, 2, MidpointRounding.AwayFromZero);

        var daysPastDue = CalculateDaysPastDue(loan.Schedules, asOf);
        var parBucket = DetermineParBucket(daysPastDue);

        return new LoanAccrualSnapshotDto
        {
            LoanId = loan.Id,
            AsOfDate = asOf,
            OutstandingPrincipal = outstandingPrincipal,
            AnnualRate = loan.Rate,
            DailyAccruedInterest = dailyAccruedInterest,
            AccruedInterestToDate = accruedInterestToDate,
            DaysPastDue = daysPastDue,
            ParBucket = parBucket
        };
    }

    public async Task<LoanDto> RepayLoanAsync(string loanId, LoanRepayRequest request)
    {
        using var dbTransaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var loan = await _context.Loans
                .Include(l => l.Schedules)
                .Include(l => l.Product)
                .Include(l => l.LoanProduct)
                .FirstOrDefaultAsync(l => l.Id == loanId);

            if (loan == null)
            {
                throw new InvalidOperationException("Loan not found");
            }

            if (!string.Equals(loan.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Loan is not active");
            }

            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == request.AccountId);
            if (account == null)
            {
                throw new InvalidOperationException("Repayment account not found");
            }

            if (!string.Equals(account.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Repayment account is not active");
            }

            if (!string.Equals(account.CustomerId, loan.CustomerId, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Repayment account does not belong to the loan customer");
            }

            var availableBalance = Math.Max(0m, account.Balance - account.LienAmount);
            if (availableBalance < request.Amount)
            {
                throw new InvalidOperationException("Insufficient available funds for repayment");
            }

            var repaymentReference = string.IsNullOrWhiteSpace(request.ClientReference)
                ? $"RPY-{loanId}-{DateTime.UtcNow:yyyyMMddHHmmss}-{RandomNumberGenerator.GetInt32(1000, 9999)}"
                : $"RPY-{request.ClientReference.Trim()}";

            if (!string.IsNullOrWhiteSpace(request.ClientReference))
            {
                var existingRepayment = await _context.Transactions.FirstOrDefaultAsync(t =>
                    t.Reference == repaymentReference &&
                    t.Type == "LOAN_REPAYMENT" &&
                    t.Narration == $"Loan Repayment - {loanId}");

                if (existingRepayment != null)
                {
                    return MapLoanDto(loan);
                }
            }

            var repaymentEvent = new FinancialEvent
            {
                EventType = EventTypes.LoanRepaymentReceived,
                EntityType = "LOAN",
                EntityId = loanId,
                Amount = request.Amount,
                Currency = ResolveCurrency(loan.LoanProduct?.Currency, account.Currency),
                BranchId = loan.BranchId ?? "001",
                Reference = repaymentReference,
                PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { AccountId = account.Id }),
                CreatedBy = null
            };

            var postResult = await _postingEngine.ProcessEventAsync(repaymentEvent);
            if (!postResult.Success)
            {
                throw new InvalidOperationException($"Posting Engine Failed: {postResult.ErrorMessage}");
            }
            
            var txnId = postResult.JournalEntryId ?? $"JRN-{DateTime.UtcNow:yyyyMMdd}";

            decimal remainingAmount = request.Amount;
            decimal principalAllocated = 0m;
            decimal interestAllocated = 0m;

            var dueSchedules = loan.Schedules
                .Where(s => string.Equals(s.Status, "DUE", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(s.Status, "PARTIAL", StringComparison.OrdinalIgnoreCase))
                .OrderBy(s => s.Period)
                .ToList();

            foreach (var schedule in dueSchedules)
            {
                if (remainingAmount <= 0) break;

                var interestDue = Math.Max(0m, schedule.Interest ?? 0m);
                var principalDue = Math.Max(0m, schedule.Principal ?? 0m);

                var interestPayment = Math.Min(remainingAmount, interestDue);
                interestDue -= interestPayment;
                remainingAmount -= interestPayment;
                interestAllocated += interestPayment;

                var principalPayment = Math.Min(remainingAmount, principalDue);
                principalDue -= principalPayment;
                remainingAmount -= principalPayment;
                principalAllocated += principalPayment;

                schedule.Interest = Math.Round(interestDue, 2, MidpointRounding.AwayFromZero);
                schedule.Principal = Math.Round(principalDue, 2, MidpointRounding.AwayFromZero);
                schedule.Total = Math.Round((schedule.Principal ?? 0m) + (schedule.Interest ?? 0m), 2, MidpointRounding.AwayFromZero);

                schedule.Status = (schedule.Total ?? 0m) <= 0m
                    ? "PAID"
                    : "PARTIAL";
            }

            if (remainingAmount > 0m)
            {
                var unpaidSchedules = dueSchedules
                    .Where(s => (s.Principal ?? 0m) > 0m)
                    .OrderBy(s => s.Period)
                    .ToList();

                foreach (var schedule in unpaidSchedules)
                {
                    if (remainingAmount <= 0) break;

                    var principalDue = Math.Max(0m, schedule.Principal ?? 0m);
                    var principalPayment = Math.Min(remainingAmount, principalDue);

                    principalDue -= principalPayment;
                    remainingAmount -= principalPayment;
                    principalAllocated += principalPayment;

                    schedule.Principal = Math.Round(principalDue, 2, MidpointRounding.AwayFromZero);
                    schedule.Total = Math.Round((schedule.Principal ?? 0m) + (schedule.Interest ?? 0m), 2, MidpointRounding.AwayFromZero);
                    schedule.Status = (schedule.Total ?? 0m) <= 0m ? "PAID" : "PARTIAL";
                }
            }

            var currentOutstanding = Math.Max(0m, loan.OutstandingBalance ?? 0m);
            loan.OutstandingBalance = Math.Max(0m, Math.Round(currentOutstanding - principalAllocated, 2, MidpointRounding.AwayFromZero));
            loan.Status = loan.OutstandingBalance <= 0m ? "CLOSED" : "ACTIVE";

            var asOf = DateOnly.FromDateTime(DateTime.UtcNow);
            var daysPastDue = CalculateDaysPastDue(loan.Schedules, asOf);
            loan.ParBucket = DetermineParBucket(daysPastDue);

            var behavior = new LoanRepaymentBehavior
            {
                LoanId = ToDeterministicGuid(loan.Id),
                TransactionId = ToDeterministicGuid(txnId),
                PaymentDate = DateTime.UtcNow,
                PaymentSource = PaymentSourceType.Internal,
                PaymentReference = repaymentReference,
                TotalPaid = request.Amount,
                PrincipalAllocated = Math.Round(principalAllocated, 2, MidpointRounding.AwayFromZero),
                InterestAllocated = Math.Round(interestAllocated, 2, MidpointRounding.AwayFromZero),
                PenaltyAllocated = 0m,
                FeesAllocated = 0m,
                IsFirstPaymentDefault = loan.Schedules.Any(s => s.Period == 1 && s.DueDate < asOf && !string.Equals(s.Status, "PAID", StringComparison.OrdinalIgnoreCase)),
                DaysPastDueUponPayment = daysPastDue,
                LatePayTrendScore = Math.Min(100, daysPastDue * 2)
            };

            var repayment = new LoanRepayment
            {
                LoanId = loan.Id,
                RepaymentDate = DateTime.UtcNow,
                Amount = request.Amount,
                PrincipalComponent = Math.Round(principalAllocated, 2, MidpointRounding.AwayFromZero),
                InterestComponent = Math.Round(interestAllocated, 2, MidpointRounding.AwayFromZero),
                PenaltyComponent = 0m,
                Reference = repaymentReference,
                ProcessedBy = null
            };

            _context.LoanRepaymentBehaviors.Add(behavior);
            _context.LoanRepayments.Add(repayment);

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            await dbTransaction.DisposeAsync();

            await _loanAccountingPostingService.PostEventAsync(
                loan,
                LoanAccountingEventType.Repayment,
                request.Amount,
                null,
                $"Loan repayment posting for {loan.Id}");

            await _auditLoggingService.LogActionAsync(
                action: "LOAN_REPAID",
                entityType: "LOAN",
                entityId: loan.Id,
                userId: null,
                description: $"Loan repayment posted for {loan.Id}",
                status: "SUCCESS",
                oldValues: new { OutstandingBalance = currentOutstanding },
                newValues: new { loan.OutstandingBalance, principalAllocated, interestAllocated, request.Amount, repaymentReference });

            await _suspiciousActivityService.HandleLargeTransactionAsync(account.Id, request.Amount, "LOAN_REPAYMENT", null);

            return MapLoanDto(loan);
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();

            await _auditLoggingService.LogActionAsync(
                action: "LOAN_REPAY_FAILED",
                entityType: "LOAN",
                entityId: loanId,
                userId: null,
                description: $"Failed loan repayment for {loanId}",
                status: "FAILED",
                errorMessage: ex.Message,
                newValues: new { loanId, request.AccountId, request.Amount, request.ClientReference });

            throw;
        }
    }

    private static int CalculateDaysPastDue(ICollection<LoanSchedule> schedules, DateOnly asOf)
    {
        var overdue = schedules
            .Where(s => (string.Equals(s.Status, "DUE", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(s.Status, "PARTIAL", StringComparison.OrdinalIgnoreCase)) &&
                        s.DueDate.HasValue && s.DueDate.Value < asOf)
            .Select(s => asOf.DayNumber - s.DueDate!.Value.DayNumber)
            .DefaultIfEmpty(0)
            .Max();

        return Math.Max(0, overdue);
    }

    private static string DetermineParBucket(int daysPastDue)
    {
        if (daysPastDue <= 0) return "0";
        if (daysPastDue <= 30) return "1-30";
        if (daysPastDue <= 90) return "31-90";
        if (daysPastDue <= 180) return "91-180";
        return "180+";
    }

    private static Guid ToDeterministicGuid(string seed)
    {
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(seed));
        return new Guid(hash);
    }

    private static LoanDto MapLoanDto(Loan loan)
    {
        return new LoanDto
        {
            Id = loan.Id,
            Cif = loan.CustomerId ?? string.Empty,
            GroupId = loan.GroupId,
            ProductCode = loan.ProductCode,
            ProductName = loan.Product?.Name ?? loan.LoanProduct?.Name,
            Principal = loan.Principal,
            Rate = loan.Rate,
            TermMonths = loan.TermMonths,
            DisbursementDate = loan.DisbursementDate,
            ParBucket = loan.ParBucket,
            OutstandingBalance = loan.OutstandingBalance,
            CollateralType = loan.CollateralType,
            CollateralValue = loan.CollateralValue,
            Status = loan.Status,
            InterestMethod = loan.InterestMethod,
            RepaymentFrequency = loan.RepaymentFrequency,
            ScheduleType = loan.ScheduleType,
            LoanProductId = loan.LoanProductId
        };
    }

    public async Task<LoanPenaltyDto> AssessPenaltyAsync(string loanId, AssessLoanPenaltyRequest request)
    {
        using var dbTransaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var loan = await _context.Loans
                .Include(l => l.Schedules)
                .FirstOrDefaultAsync(l => l.Id == loanId);

            if (loan == null)
            {
                throw new InvalidOperationException("Loan not found");
            }

            if (!string.Equals(loan.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Penalty can only be assessed on active loans");
            }

            var clientRef = string.IsNullOrWhiteSpace(request.ClientReference)
                ? null
                : request.ClientReference.Trim();

            if (!string.IsNullOrWhiteSpace(clientRef))
            {
                var existingBehavior = await _context.LoanRepaymentBehaviors
                    .Where(b => b.LoanId == ToDeterministicGuid(loanId) && 
                                b.PaymentReference == clientRef &&
                                b.PenaltyAllocated > 0)
                    .FirstOrDefaultAsync();

                if (existingBehavior != null)
                {
                    return new LoanPenaltyDto
                    {
                        LoanId = loanId,
                        PenaltyAmount = existingBehavior.PenaltyAllocated,
                        PenaltyRate = request.PenaltyRate,
                        DaysPastDue = CalculateDaysPastDue(loan.Schedules, DateOnly.FromDateTime(DateTime.UtcNow)),
                        OutstandingBalance = loan.OutstandingBalance ?? 0m,
                        Reason = request.Reason ?? "Late payment penalty",
                        AssessedAt = existingBehavior.PaymentDate
                    };
                }
            }

            var outstandingBalance = loan.OutstandingBalance ?? 0m;
            if (outstandingBalance <= 0m)
            {
                throw new InvalidOperationException("No outstanding balance to assess penalty on");
            }

            var penaltyAmount = Math.Round(outstandingBalance * request.PenaltyRate / 100m, 2, MidpointRounding.AwayFromZero);
            if (penaltyAmount <= 0m)
            {
                throw new InvalidOperationException("Calculated penalty amount must be greater than zero");
            }

            loan.OutstandingBalance = Math.Round(outstandingBalance + penaltyAmount, 2, MidpointRounding.AwayFromZero);

            var penaltySchedule = new LoanSchedule
            {
                LoanId = loanId,
                Period = null,
                DueDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Principal = 0m,
                Interest = penaltyAmount,
                Total = penaltyAmount,
                Balance = loan.OutstandingBalance,
                Status = "DUE"
            };

            var penaltyBehavior = new LoanRepaymentBehavior
            {
                LoanId = ToDeterministicGuid(loanId),
                TransactionId = Guid.NewGuid(),
                PaymentDate = DateTime.UtcNow,
                PaymentSource = PaymentSourceType.Internal,
                PaymentReference = clientRef ?? $"AUTO-PEN-{DateTime.UtcNow:yyyyMMddHHmmss}",
                TotalPaid = 0m,
                PrincipalAllocated = 0m,
                InterestAllocated = 0m,
                PenaltyAllocated = penaltyAmount,
                FeesAllocated = 0m,
                IsFirstPaymentDefault = false,
                DaysPastDueUponPayment = CalculateDaysPastDue(loan.Schedules, DateOnly.FromDateTime(DateTime.UtcNow)),
                LatePayTrendScore = 0
            };

            _context.LoanSchedules.Add(penaltySchedule);
            _context.LoanRepaymentBehaviors.Add(penaltyBehavior);

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            await dbTransaction.DisposeAsync();

            await _loanAccountingPostingService.PostEventAsync(
                loan,
                LoanAccountingEventType.PenaltyAccrual,
                penaltyAmount,
                null,
                $"Penalty accrual posting for {loan.Id}");

            await _auditLoggingService.LogActionAsync(
                action: "PENALTY_ASSESSED",
                entityType: "LOAN",
                entityId: loanId,
                userId: null,
                description: $"Penalty assessed on loan {loanId}",
                status: "SUCCESS",
                oldValues: new { OutstandingBalance = outstandingBalance },
                newValues: new { loan.OutstandingBalance, penaltyAmount, request.PenaltyRate, request.Reason, clientRef });

            var daysPastDue = CalculateDaysPastDue(loan.Schedules, DateOnly.FromDateTime(DateTime.UtcNow));

            return new LoanPenaltyDto
            {
                LoanId = loanId,
                PenaltyAmount = penaltyAmount,
                PenaltyRate = request.PenaltyRate,
                DaysPastDue = daysPastDue,
                OutstandingBalance = loan.OutstandingBalance.Value,
                Reason = request.Reason ?? "Late payment penalty",
                AssessedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();

            await _auditLoggingService.LogActionAsync(
                action: "PENALTY_ASSESSMENT_FAILED",
                entityType: "LOAN",
                entityId: loanId,
                userId: null,
                description: $"Failed to assess penalty on loan {loanId}",
                status: "FAILED",
                errorMessage: ex.Message,
                newValues: new { loanId, request.PenaltyRate, request.Reason, request.ClientReference });

            throw;
        }
    }

    public async Task<LoanClassificationDto> ClassifyAndProvisionLoanAsync(string loanId)
    {
        using var dbTransaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var loan = await _context.Loans
                .Include(l => l.Schedules)
                .FirstOrDefaultAsync(l => l.Id == loanId);

            if (loan == null)
            {
                throw new InvalidOperationException("Loan not found");
            }

            var asOf = DateOnly.FromDateTime(DateTime.UtcNow);
            var daysPastDue = CalculateDaysPastDue(loan.Schedules, asOf);

            var bogTier = daysPastDue switch
            {
                0 => BogClassificationTier.Current,
                > 0 and <= 30 => BogClassificationTier.Oversight,
                > 30 and <= 90 => BogClassificationTier.Substandard,
                > 90 and <= 180 => BogClassificationTier.Doubtful,
                _ => BogClassificationTier.Loss
            };

            var provisioningRate = bogTier switch
            {
                BogClassificationTier.Current => 0.01m,
                BogClassificationTier.Oversight => 0.05m,
                BogClassificationTier.Substandard => 0.25m,
                BogClassificationTier.Doubtful => 0.50m,
                BogClassificationTier.Loss => 1.00m,
                _ => 0m
            };

            var outstandingPrincipal = loan.OutstandingBalance ?? 0m;
            var outstandingInterest = loan.Schedules
                .Where(s => s.Status == "DUE" || s.Status == "PARTIAL")
                .Sum(s => s.Interest ?? 0m);

            var totalOutstanding = outstandingPrincipal + outstandingInterest;
            var provisioningAmount = Math.Round(totalOutstanding * provisioningRate, 2, MidpointRounding.AwayFromZero);

            var classification = new LoanBogClassification
            {
                LoanId = ToDeterministicGuid(loanId),
                EvaluationDate = DateTime.UtcNow,
                DaysPastDue = daysPastDue,
                Classification = bogTier,
                OutstandingPrincipal = outstandingPrincipal,
                OutstandingInterest = outstandingInterest,
                ProvisioningAmount = provisioningAmount
            };

            _context.LoanBogClassifications.Add(classification);

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            await dbTransaction.DisposeAsync();

            if (provisioningAmount > 0)
            {
                await _loanAccountingPostingService.PostEventAsync(
                    loan,
                    LoanAccountingEventType.Impairment,
                    provisioningAmount,
                    null,
                    $"Impairment posting for {loan.Id}");
            }

            await _auditLoggingService.LogActionAsync(
                action: "NPL_CLASSIFIED",
                entityType: "LOAN",
                entityId: loanId,
                userId: null,
                description: $"Loan {loanId} classified as {bogTier}",
                status: "SUCCESS",
                newValues: new { bogTier, daysPastDue, provisioningAmount, provisioningRate });

            return new LoanClassificationDto
            {
                LoanId = loanId,
                BogTier = bogTier.ToString(),
                DaysPastDue = daysPastDue,
                OutstandingPrincipal = outstandingPrincipal,
                OutstandingInterest = outstandingInterest,
                ProvisioningAmount = provisioningAmount,
                ProvisioningRate = provisioningRate,
                EvaluationDate = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();

            await _auditLoggingService.LogActionAsync(
                action: "NPL_CLASSIFICATION_FAILED",
                entityType: "LOAN",
                entityId: loanId,
                userId: null,
                description: $"Failed to classify loan {loanId}",
                status: "FAILED",
                errorMessage: ex.Message,
                newValues: new { loanId });

            throw;
        }
    }

    public async Task<LoanDto> ApplyLoanAsync(ApplyLoanRequest request, string? makerId)
    {
        var resolvedMakerId = await ResolveStaffIdAsync(makerId);

        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == request.CustomerId);
        if (customer == null)
        {
            throw new InvalidOperationException("Customer not found");
        }

        var loanProduct = await _context.LoanProducts.FirstOrDefaultAsync(lp => lp.Id == request.LoanProductId && lp.IsActive);
        if (loanProduct == null)
        {
            throw new InvalidOperationException("Loan product not found or inactive");
        }

        if (request.Principal < loanProduct.MinAmount || request.Principal > loanProduct.MaxAmount)
        {
            throw new InvalidOperationException("Requested amount is outside product limits");
        }

        var rate = request.AnnualInterestRate <= 0 ? loanProduct.AnnualInterestRate : request.AnnualInterestRate;
        var termInPeriods = request.TermInPeriods <= 0 ? loanProduct.TermInPeriods : request.TermInPeriods;

        var defaultRetailLoanProductId = await _context.Products
            .Where(p => p.Type == "LOAN" && p.Status == "ACTIVE")
            .OrderBy(p => p.Id)
            .Select(p => p.Id)
            .FirstOrDefaultAsync();

        ValidateProductBusinessRules(loanProduct, request);

        var loanId = $"LN{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        var loan = new Loan
        {
            Id = loanId,
            CustomerId = request.CustomerId,
            GroupId = request.GroupId,
            ProductCode = defaultRetailLoanProductId,
            LoanProductId = loanProduct.Id,
            Principal = request.Principal,
            Rate = rate,
            TermMonths = NormalizeTermMonths(request.RepaymentFrequency, termInPeriods),
            InterestMethod = request.InterestMethod,
            RepaymentFrequency = request.RepaymentFrequency,
            ScheduleType = request.ScheduleType,
            Status = "PENDING_APPROVAL",
            OutstandingBalance = request.Principal,
            ParBucket = "0",
            ApplicationDate = DateTime.UtcNow,
            MakerId = resolvedMakerId
        };

        var workflowId = await _context.Workflows
            .Where(w => w.Status == "ACTIVE" && (w.TriggerType == "LOAN" || w.TriggerType == "TRANSACTION"))
            .OrderBy(w => w.Id)
            .Select(w => w.Id)
            .FirstOrDefaultAsync();

        var approvalRequest = new ApprovalRequest
        {
            Id = $"APR-{loanId}",
            WorkflowId = workflowId,
            EntityType = "LOAN",
            EntityId = loanId,
            RequesterId = resolvedMakerId,
            Status = "PENDING",
            CurrentStep = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var loanAccount = new LoanAccount
        {
            LoanId = loanId,
            BranchId = "BR001",
            AppraisalStatus = LoanAppraisalStatus.Pending,
            ExposureAmount = request.Principal,
            DelinquencyDays = 0,
            ArrearsBucket = "0",
            CreatedAt = DateTime.UtcNow
        };

        var disclosure = new LoanDisclosure
        {
            LoanId = loanId,
            DisclosureText = "Digital lending terms, pricing, total cost of credit, and consumer rights have been provided.",
            Accepted = true,
            AcceptedAt = DateTime.UtcNow,
            Channel = "DIGITAL"
        };

        _context.Loans.Add(loan);
        _context.LoanAccounts.Add(loanAccount);
        _context.LoanDisclosures.Add(disclosure);
        _context.ApprovalRequests.Add(approvalRequest);
        await _context.SaveChangesAsync();

        await _auditLoggingService.LogActionAsync(
            action: "LOAN_APPLIED",
            entityType: "LOAN",
            entityId: loan.Id,
            userId: resolvedMakerId,
            description: $"Loan application submitted for customer {request.CustomerId}",
            status: "SUCCESS",
            newValues: new { request.CustomerId, request.Principal, request.LoanProductId, request.InterestMethod, request.RepaymentFrequency, request.ScheduleType });

        return MapLoanDto(loan);
    }

    public async Task<LoanDto> ApproveLoanAsync(ApproveLoanRequest request, string? checkerId)
    {
        var resolvedCheckerId = await ResolveStaffIdAsync(checkerId);

        var loan = await _context.Loans.FirstOrDefaultAsync(l => l.Id == request.LoanId);
        if (loan == null)
        {
            throw new InvalidOperationException("Loan not found");
        }

        if (!string.Equals(loan.Status, "PENDING_APPROVAL", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only pending loans can be approved");
        }

        if (!string.IsNullOrWhiteSpace(resolvedCheckerId) && string.Equals(resolvedCheckerId, loan.MakerId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Maker-checker rule violated: approver cannot be the applicant");
        }

        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == loan.CustomerId);
        if (customer == null)
        {
            throw new InvalidOperationException("Customer not found");
        }

        if (string.IsNullOrWhiteSpace(customer.GhanaCard))
        {
            throw new InvalidOperationException("KYC validation failed: Ghana Card is required before approval");
        }

        var kycValid = await _kycService.ValidateTransactionAmountAsync(customer.Id, loan.Principal);
        if (!kycValid)
        {
            throw new InvalidOperationException("KYC validation failed: loan amount exceeds customer KYC limit");
        }

        var maxExposurePerCustomer = await GetDecimalConfigAsync("loan.max_exposure_per_customer", 100000m);
        var currentCustomerExposure = await _context.Loans
            .Where(l => l.CustomerId == customer.Id && (l.Status == "ACTIVE" || l.Status == "APPROVED" || l.Status == "PENDING_APPROVAL"))
            .SumAsync(l => l.OutstandingBalance ?? l.Principal);

        if (currentCustomerExposure > maxExposurePerCustomer)
        {
            throw new InvalidOperationException("Exposure limit exceeded for customer");
        }

        var portfolioConcentrationLimit = await GetDecimalConfigAsync("loan.portfolio_concentration_limit_pct", 40m);
        var totalPortfolio = await _context.Loans
            .Where(l => l.Status == "ACTIVE" || l.Status == "APPROVED")
            .SumAsync(l => l.OutstandingBalance ?? l.Principal);

        var concentrationRatio = totalPortfolio <= 0m ? 0m : Math.Round((currentCustomerExposure / totalPortfolio) * 100m, 2);
        if (concentrationRatio > portfolioConcentrationLimit)
        {
            throw new InvalidOperationException("Concentration threshold breached for this approval");
        }

        var creditResult = await CheckCreditAndPersistAsync(customer.Id, loan.Id);
        if (string.Equals(creditResult.Decision, "FAIL", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Credit bureau check failed");
        }

        loan.Status = "APPROVED";
        loan.ApprovedAt = DateTime.UtcNow;
        loan.ApprovedBy = resolvedCheckerId;
        loan.CheckerId = resolvedCheckerId;

        var approvalRequest = await _context.ApprovalRequests
            .FirstOrDefaultAsync(a => a.EntityType == "LOAN" && a.EntityId == loan.Id && a.Status == "PENDING");

        if (approvalRequest != null)
        {
            approvalRequest.Status = "APPROVED";
            approvalRequest.UpdatedAt = DateTime.UtcNow;
            approvalRequest.CurrentStep = 2;
        }

        await _context.SaveChangesAsync();

        await _auditLoggingService.LogActionAsync(
            action: "LOAN_APPROVED",
            entityType: "LOAN",
            entityId: loan.Id,
            userId: resolvedCheckerId,
            description: $"Loan approved with checker {resolvedCheckerId}",
            status: "SUCCESS",
            newValues: new { loan.Id, loan.Status, loan.ApprovedAt, loan.ApprovedBy, request.DecisionNotes });

        return MapLoanDto(loan);
    }

    public async Task<LoanDto> DisburseApprovedLoanAsync(DisburseApprovedLoanRequest request)
    {
        using var dbTransaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var loan = await _context.Loans
                .Include(l => l.Schedules)
                .Include(l => l.LoanProduct)
                .FirstOrDefaultAsync(l => l.Id == request.LoanId);

            if (loan == null)
            {
                throw new InvalidOperationException("Loan not found");
            }

            if (string.Equals(loan.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase) && loan.DisbursementDate.HasValue)
            {
                return MapLoanDto(loan);
            }

            if (!string.Equals(loan.Status, "APPROVED", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Loan must be approved before disbursement");
            }

            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.CustomerId == loan.CustomerId && (a.Status == "ACTIVE" || a.Status == "Active"));

            if (account == null)
            {
                throw new InvalidOperationException("Customer has no active account for disbursement");
            }

            var disbursementReference = string.IsNullOrWhiteSpace(request.ClientReference)
                ? $"DSB-{loan.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}-{RandomNumberGenerator.GetInt32(1000, 9999)}"
                : $"DSB-{request.ClientReference.Trim()}";

            var existingEvent = await _context.FinancialEvents.FirstOrDefaultAsync(e =>
                e.Reference == disbursementReference &&
                e.EventType == EventTypes.LoanDisbursed &&
                e.EntityType == "LOAN" &&
                e.EntityId == loan.Id);

            if (existingEvent != null)
            {
                return MapLoanDto(loan);
            }

            loan.DisbursementDate = DateOnly.FromDateTime(DateTime.UtcNow);
            loan.DisbursedAt = DateTime.UtcNow;
            loan.Status = "ACTIVE";
            loan.OutstandingBalance = loan.Principal;

            _context.LoanSchedules.RemoveRange(loan.Schedules);

            var generatedSchedule = GenerateScheduleLines(
                principal: loan.Principal,
                annualRate: loan.Rate,
                termInPeriods: ResolveTermInPeriods(loan),
                interestMethod: ParseInterestMethod(loan.InterestMethod),
                repaymentFrequency: ParseRepaymentFrequency(loan.RepaymentFrequency),
                scheduleType: ParseRepaymentFrequency(loan.ScheduleType),
                startDate: loan.DisbursementDate.Value);

            foreach (var line in generatedSchedule)
            {
                _context.LoanSchedules.Add(new LoanSchedule
                {
                    LoanId = loan.Id,
                    Period = line.Period,
                    DueDate = line.DueDate,
                    Principal = line.Principal,
                    Interest = line.Interest,
                    Total = line.Installment,
                    Balance = line.ClosingBalance,
                    Status = "DUE"
                });
            }

            var disbursementEvent = new FinancialEvent
            {
                EventType = EventTypes.LoanDisbursed,
                EntityType = "LOAN",
                EntityId = loan.Id,
                Amount = loan.Principal,
                Currency = ResolveCurrency(loan.LoanProduct?.Currency, account.Currency),
                BranchId = loan.BranchId ?? "001",
                Reference = disbursementReference,
                PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { AccountId = account.Id, Rate = loan.Rate }),
                CreatedBy = null // Processed through automated approval worker
            };

            var postResult = await _postingEngine.ProcessEventAsync(disbursementEvent);
            if (!postResult.Success)
            {
                throw new InvalidOperationException($"Posting Engine Failed: {postResult.ErrorMessage}");
            }

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            await dbTransaction.DisposeAsync();

            await _loanAccountingPostingService.PostEventAsync(
                loan,
                LoanAccountingEventType.Disbursement,
                loan.Principal,
                loan.CheckerId,
                $"Approved loan disbursement posting for {loan.Id}");

            await _auditLoggingService.LogActionAsync(
                action: "LOAN_DISBURSED",
                entityType: "LOAN",
                entityId: loan.Id,
                userId: loan.CheckerId,
                description: $"Approved loan disbursed for customer {loan.CustomerId}",
                status: "SUCCESS",
                newValues: new { loan.Id, loan.CustomerId, loan.Principal, disbursementReference });

            await _suspiciousActivityService.HandleLargeTransactionAsync(account.Id, loan.Principal, "LOAN_DISBURSEMENT", null);

            return MapLoanDto(loan);
        }
        catch
        {
            await dbTransaction.RollbackAsync();
            throw;
        }
    }

    public Task<LoanDto> RepayLoanByBodyAsync(RepayLoanRequest request)
    {
        var repayRequest = new LoanRepayRequest
        {
            AccountId = request.AccountId,
            Amount = request.Amount,
            ClientReference = request.ClientReference
        };

        return RepayLoanAsync(request.LoanId, repayRequest);
    }

    public async Task<CreditCheckDto> CheckCreditAsync(CheckCreditRequest request)
    {
        var check = await CheckCreditAndPersistAsync(request.CustomerId, request.LoanId, request.ProviderName);

        return new CreditCheckDto
        {
            CustomerId = request.CustomerId,
            LoanId = request.LoanId,
            Score = check.Score,
            RiskBand = check.RiskBand,
            RiskGrade = check.RiskGrade,
            Decision = check.Decision,
            Recommendation = check.Recommendation,
            ProviderName = check.ProviderName,
            InquiryReference = check.InquiryReference,
            CheckedAt = DateTime.UtcNow
        };
    }

    public Task<LoanScheduleGenerationResultDto> GenerateScheduleAsync(GenerateLoanScheduleRequest request)
    {
        var startDate = request.StartDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var lines = GenerateScheduleLines(
            principal: request.Principal,
            annualRate: request.AnnualInterestRate,
            termInPeriods: request.TermInPeriods,
            interestMethod: ParseInterestMethod(request.InterestMethod),
            repaymentFrequency: ParseRepaymentFrequency(request.RepaymentFrequency),
            scheduleType: ParseRepaymentFrequency(request.ScheduleType),
            startDate: startDate);

        return Task.FromResult(new LoanScheduleGenerationResultDto
        {
            Principal = request.Principal,
            AnnualInterestRate = request.AnnualInterestRate,
            InterestMethod = request.InterestMethod,
            RepaymentFrequency = request.RepaymentFrequency,
            ScheduleType = request.ScheduleType,
            Lines = lines
        });
    }

    private static string ResolveCurrency(string? primaryCurrency, string? secondaryCurrency)
    {
        if (!string.IsNullOrWhiteSpace(primaryCurrency))
        {
            return primaryCurrency.Trim().ToUpperInvariant();
        }

        if (!string.IsNullOrWhiteSpace(secondaryCurrency))
        {
            return secondaryCurrency.Trim().ToUpperInvariant();
        }

        return "GHS";
    }

    private async Task<CreditBureauCheck> CheckCreditAndPersistAsync(string customerId, string? loanId, string? providerName = null)
    {
        var bureauResult = await _creditBureauService.CheckCreditAsync(customerId, providerName);

        var check = new CreditBureauCheck
        {
            LoanId = loanId,
            CustomerId = customerId,
            BureauName = bureauResult.BureauName,
            ProviderName = bureauResult.ProviderName,
            InquiryReference = bureauResult.InquiryReference,
            Score = bureauResult.Score,
            RiskBand = bureauResult.RiskBand,
            RiskGrade = bureauResult.RiskGrade,
            Decision = bureauResult.Decision,
            Recommendation = bureauResult.Recommendation,
            RequestPayload = bureauResult.RequestPayload,
            RawResponse = bureauResult.RawResponse,
            IsTimeout = bureauResult.IsTimeout,
            RetryCount = bureauResult.RetryCount,
            Status = bureauResult.Status,
            CheckedAt = DateTime.UtcNow
        };

        _context.CreditBureauChecks.Add(check);
        await _context.SaveChangesAsync();

        return check;
    }

    private static List<LoanScheduleLineDto> GenerateScheduleLines(
        decimal principal,
        decimal annualRate,
        int termInPeriods,
        LoanInterestMethod interestMethod,
        LoanRepaymentFrequency repaymentFrequency,
        LoanRepaymentFrequency scheduleType,
        DateOnly startDate)
    {
        var lines = new List<LoanScheduleLineDto>();
        if (termInPeriods <= 0)
        {
            return lines;
        }

        var periodsPerYear = repaymentFrequency == LoanRepaymentFrequency.Weekly ? 52m : 12m;
        var periodRate = annualRate <= 0 ? 0m : (annualRate / 100m) / periodsPerYear;
        var opening = principal;

        if (interestMethod == LoanInterestMethod.Flat)
        {
            var totalInterest = Math.Round(principal * (annualRate / 100m) * (termInPeriods / periodsPerYear), 2, MidpointRounding.AwayFromZero);
            var perPeriodInterest = Math.Round(totalInterest / termInPeriods, 2, MidpointRounding.AwayFromZero);

            for (var period = 1; period <= termInPeriods; period++)
            {
                var principalComponent = scheduleType == LoanRepaymentFrequency.Bullet
                    ? (period == termInPeriods ? opening : 0m)
                    : Math.Round(principal / termInPeriods, 2, MidpointRounding.AwayFromZero);

                if (period == termInPeriods)
                {
                    principalComponent = opening;
                }

                var closing = Math.Max(0m, Math.Round(opening - principalComponent, 2, MidpointRounding.AwayFromZero));

                lines.Add(new LoanScheduleLineDto
                {
                    Period = period,
                    DueDate = AddPeriod(startDate, repaymentFrequency, period),
                    OpeningBalance = opening,
                    Principal = principalComponent,
                    Interest = perPeriodInterest,
                    Installment = Math.Round(principalComponent + perPeriodInterest, 2, MidpointRounding.AwayFromZero),
                    ClosingBalance = closing
                });

                opening = closing;
            }

            return lines;
        }

        decimal installment;
        if (scheduleType == LoanRepaymentFrequency.Bullet)
        {
            installment = 0m;
        }
        else if (periodRate == 0m)
        {
            installment = Math.Round(principal / termInPeriods, 2, MidpointRounding.AwayFromZero);
        }
        else
        {
            var r = (double)periodRate;
            var n = termInPeriods;
            var payment = (double)principal * r / (1 - Math.Pow(1 + r, -n));
            installment = Math.Round((decimal)payment, 2, MidpointRounding.AwayFromZero);
        }

        for (var period = 1; period <= termInPeriods; period++)
        {
            var interest = Math.Round(opening * periodRate, 2, MidpointRounding.AwayFromZero);
            decimal principalComponent;

            if (scheduleType == LoanRepaymentFrequency.Bullet)
            {
                principalComponent = period == termInPeriods ? opening : 0m;
                installment = Math.Round(principalComponent + interest, 2, MidpointRounding.AwayFromZero);
            }
            else
            {
                principalComponent = Math.Round(installment - interest, 2, MidpointRounding.AwayFromZero);
                if (period == termInPeriods)
                {
                    principalComponent = opening;
                    installment = Math.Round(principalComponent + interest, 2, MidpointRounding.AwayFromZero);
                }
            }

            var closing = Math.Max(0m, Math.Round(opening - principalComponent, 2, MidpointRounding.AwayFromZero));

            lines.Add(new LoanScheduleLineDto
            {
                Period = period,
                DueDate = AddPeriod(startDate, repaymentFrequency, period),
                OpeningBalance = opening,
                Principal = principalComponent,
                Interest = interest,
                Installment = installment,
                ClosingBalance = closing
            });

            opening = closing;
        }

        return lines;
    }

    public async Task<LoanProduct> ConfigureLoanProductAsync(ConfigureLoanProductRequest request)
    {
        if (request.ProductType.Equals(nameof(LoanProductType.DigitalLoan30Days), StringComparison.OrdinalIgnoreCase))
        {
            request.TermInPeriods = 1;
            request.RepaymentFrequency = nameof(LoanRepaymentFrequency.Bullet);
        }

        if (request.ProductType.Equals(nameof(LoanProductType.WeeklyGroupLoan), StringComparison.OrdinalIgnoreCase))
        {
            request.RepaymentFrequency = nameof(LoanRepaymentFrequency.Weekly);
        }

        if (request.ProductType.Equals(nameof(LoanProductType.MonthlyBusinessLoan), StringComparison.OrdinalIgnoreCase) ||
            request.ProductType.Equals(nameof(LoanProductType.MonthlyConsumerLoan), StringComparison.OrdinalIgnoreCase))
        {
            request.RepaymentFrequency = nameof(LoanRepaymentFrequency.Monthly);
        }

        var existing = await _context.LoanProducts.FirstOrDefaultAsync(p => p.Id == request.Id);
        if (existing == null)
        {
            existing = new LoanProduct
            {
                Id = request.Id,
                Code = request.Code,
                Name = request.Name,
                ProductType = Enum.TryParse<LoanProductType>(request.ProductType, true, out var productType)
                    ? productType
                    : LoanProductType.MonthlyConsumerLoan,
                InterestMethod = Enum.TryParse<LoanInterestMethod>(request.InterestMethod, true, out var method)
                    ? method
                    : LoanInterestMethod.Flat,
                RepaymentFrequency = Enum.TryParse<LoanRepaymentFrequency>(request.RepaymentFrequency, true, out var frequency)
                    ? frequency
                    : LoanRepaymentFrequency.Monthly,
                TermInPeriods = request.TermInPeriods,
                AnnualInterestRate = request.AnnualInterestRate,
                MinAmount = request.MinAmount,
                MaxAmount = request.MaxAmount,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.LoanProducts.Add(existing);
        }
        else
        {
            existing.Code = request.Code;
            existing.Name = request.Name;
            existing.ProductType = Enum.TryParse<LoanProductType>(request.ProductType, true, out var productType)
                ? productType
                : existing.ProductType;
            existing.InterestMethod = Enum.TryParse<LoanInterestMethod>(request.InterestMethod, true, out var method)
                ? method
                : existing.InterestMethod;
            existing.RepaymentFrequency = Enum.TryParse<LoanRepaymentFrequency>(request.RepaymentFrequency, true, out var frequency)
                ? frequency
                : existing.RepaymentFrequency;
            existing.TermInPeriods = request.TermInPeriods;
            existing.AnnualInterestRate = request.AnnualInterestRate;
            existing.MinAmount = request.MinAmount;
            existing.MaxAmount = request.MaxAmount;
            existing.IsActive = true;
        }

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<LoanAccountingProfile> ConfigureLoanAccountingProfileAsync(ConfigureLoanAccountingProfileRequest request)
    {
        var existing = await _context.LoanAccountingProfiles.FirstOrDefaultAsync(p => p.Id == request.Id);

        if (existing == null)
        {
            existing = new LoanAccountingProfile
            {
                Id = request.Id,
                LoanProductId = request.LoanProductId,
                LoanPortfolioGl = request.LoanPortfolioGl,
                InterestIncomeGl = request.InterestIncomeGl,
                ProcessingFeeIncomeGl = request.ProcessingFeeIncomeGl,
                PenaltyIncomeGl = request.PenaltyIncomeGl,
                InterestReceivableGl = request.InterestReceivableGl,
                PenaltyReceivableGl = request.PenaltyReceivableGl,
                ImpairmentExpenseGl = request.ImpairmentExpenseGl,
                ImpairmentAllowanceGl = request.ImpairmentAllowanceGl,
                RecoveryIncomeGl = request.RecoveryIncomeGl,
                DisbursementFundingGl = request.DisbursementFundingGl,
                RepaymentAllocationOrder = request.RepaymentAllocationOrder,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.LoanAccountingProfiles.Add(existing);
        }
        else
        {
            existing.LoanProductId = request.LoanProductId;
            existing.LoanPortfolioGl = request.LoanPortfolioGl;
            existing.InterestIncomeGl = request.InterestIncomeGl;
            existing.ProcessingFeeIncomeGl = request.ProcessingFeeIncomeGl;
            existing.PenaltyIncomeGl = request.PenaltyIncomeGl;
            existing.InterestReceivableGl = request.InterestReceivableGl;
            existing.PenaltyReceivableGl = request.PenaltyReceivableGl;
            existing.ImpairmentExpenseGl = request.ImpairmentExpenseGl;
            existing.ImpairmentAllowanceGl = request.ImpairmentAllowanceGl;
            existing.RecoveryIncomeGl = request.RecoveryIncomeGl;
            existing.DisbursementFundingGl = request.DisbursementFundingGl;
            existing.RepaymentAllocationOrder = request.RepaymentAllocationOrder;
            existing.IsActive = true;
        }

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<LoanAccount> AppraiseLoanAsync(AppraiseLoanRequest request, string? appraiserId)
    {
        var loan = await _context.Loans.FirstOrDefaultAsync(l => l.Id == request.LoanId)
            ?? throw new InvalidOperationException("Loan not found");

        var account = await _context.LoanAccounts.FirstOrDefaultAsync(a => a.LoanId == request.LoanId);
        if (account == null)
        {
            account = new LoanAccount
            {
                LoanId = loan.Id,
                BranchId = "BR001",
                CreatedAt = DateTime.UtcNow
            };
            _context.LoanAccounts.Add(account);
        }

        account.AppraisalStatus = Enum.TryParse<LoanAppraisalStatus>(request.Decision, true, out var status)
            ? status
            : LoanAppraisalStatus.Reviewed;
        account.AppraisalNotes = request.Notes;
        account.AppraisedAt = DateTime.UtcNow;
        account.AppraisedBy = await ResolveStaffIdAsync(appraiserId);
        account.LastReviewedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditLoggingService.LogActionAsync(
            "LOAN_APPRAISED",
            "LOAN",
            loan.Id,
            account.AppraisedBy,
            $"Loan appraised with decision {account.AppraisalStatus}",
            status: "SUCCESS",
            newValues: new { request.Decision, request.Notes });

        return account;
    }

    public async Task<LoanDto> RestructureLoanAsync(LoanRestructureRequest request, string? userId)
    {
        var loan = await _context.Loans.Include(l => l.Schedules).FirstOrDefaultAsync(l => l.Id == request.LoanId)
            ?? throw new InvalidOperationException("Loan not found");

        if (!string.Equals(loan.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only active loans can be restructured");
        }

        var updatedRate = request.NewAnnualRate ?? loan.Rate;
        loan.Rate = updatedRate;
        loan.RepaymentFrequency = string.IsNullOrWhiteSpace(request.NewRepaymentFrequency)
            ? loan.RepaymentFrequency
            : request.NewRepaymentFrequency;

        loan.TermMonths = NormalizeTermMonths(loan.RepaymentFrequency, request.NewTermInPeriods);
        var outstanding = loan.OutstandingBalance ?? loan.Principal;

        _context.LoanSchedules.RemoveRange(loan.Schedules);

        var newLines = GenerateScheduleLines(
            outstanding,
            loan.Rate,
            request.NewTermInPeriods,
            ParseInterestMethod(loan.InterestMethod),
            ParseRepaymentFrequency(loan.RepaymentFrequency),
            ParseRepaymentFrequency(loan.ScheduleType),
            DateOnly.FromDateTime(DateTime.UtcNow));

        foreach (var line in newLines)
        {
            _context.LoanSchedules.Add(new LoanSchedule
            {
                LoanId = loan.Id,
                Period = line.Period,
                DueDate = line.DueDate,
                Principal = line.Principal,
                Interest = line.Interest,
                Total = line.Installment,
                Balance = line.ClosingBalance,
                Status = "DUE"
            });
        }

        await _context.SaveChangesAsync();

        await _auditLoggingService.LogActionAsync(
            "LOAN_RESTRUCTURED",
            "LOAN",
            loan.Id,
            await ResolveStaffIdAsync(userId),
            $"Loan restructured: {request.Reason}",
            status: "SUCCESS",
            newValues: new { request.NewTermInPeriods, updatedRate, loan.RepaymentFrequency, request.Reason });

        return MapLoanDto(loan);
    }

    public async Task<LoanDto> ReverseRepaymentAsync(LoanRepaymentReversalRequest request, string? userId)
    {
        var loan = await _context.Loans.FirstOrDefaultAsync(l => l.Id == request.LoanId)
            ?? throw new InvalidOperationException("Loan not found");

        var repayment = await _context.LoanRepayments.FirstOrDefaultAsync(r => r.Id == request.RepaymentId && r.LoanId == request.LoanId)
            ?? throw new InvalidOperationException("Repayment not found");

        if (repayment.IsReversal)
        {
            throw new InvalidOperationException("Repayment is already reversed");
        }

        loan.OutstandingBalance = Math.Round((loan.OutstandingBalance ?? 0m) + repayment.PrincipalComponent, 2, MidpointRounding.AwayFromZero);

        repayment.IsReversal = true;
        repayment.ReversalReference = $"REV-{DateTime.UtcNow:yyyyMMddHHmmss}";

        await _context.SaveChangesAsync();

        await _auditLoggingService.LogActionAsync(
            "LOAN_REPAYMENT_REVERSED",
            "LOAN",
            loan.Id,
            await ResolveStaffIdAsync(userId),
            request.Reason,
            status: "SUCCESS",
            newValues: new { repayment.Id, repayment.ReversalReference, request.Reason });

        return MapLoanDto(loan);
    }

    public async Task<LoanAccrualBatchResultDto> ProcessAccrualBatchAsync(LoanAccrualBatchRequest request, string? userId)
    {
        var asOfDate = request.AsOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var query = _context.Loans
            .Where(l => l.Status != null && l.Status.ToUpper() == "ACTIVE");

        if (!string.IsNullOrWhiteSpace(request.LoanId))
        {
            query = query.Where(l => l.Id == request.LoanId);
        }

        var loans = await query.ToListAsync();

        var journalIds = new List<string>();
        decimal totalInterest = 0m;
        decimal totalPenalty = 0m;

        foreach (var loan in loans)
        {
            var outstanding = loan.OutstandingBalance ?? 0m;
            if (outstanding <= 0m) continue;

            var account = await _context.LoanAccounts.FirstOrDefaultAsync(a => a.LoanId == loan.Id);
            if (account != null && account.IsNonAccrual)
            {
                continue;
            }

            var alreadyExists = await _context.LoanAccruals.AnyAsync(a => a.LoanId == loan.Id && a.AccrualDate == asOfDate);
            if (alreadyExists)
            {
                continue;
            }

            var dailyInterest = Math.Round(outstanding * (loan.Rate / 100m) / 365m, 2, MidpointRounding.AwayFromZero);
            var penalty = 0m;

            var posting = await _loanAccountingPostingService.PostEventAsync(
                loan,
                LoanAccountingEventType.InterestAccrual,
                dailyInterest,
                await ResolveStaffIdAsync(userId),
                $"Daily interest accrual for {loan.Id} on {asOfDate:yyyy-MM-dd}");

            journalIds.Add(posting.JournalId);

            _context.LoanAccruals.Add(new LoanAccrual
            {
                LoanId = loan.Id,
                AccrualDate = asOfDate,
                InterestAccrued = dailyInterest,
                PenaltyAccrued = penalty,
                IsPosted = true,
                JournalId = posting.JournalId,
                CreatedAt = DateTime.UtcNow
            });

            totalInterest += dailyInterest;
            totalPenalty += penalty;
        }

        await _context.SaveChangesAsync();

        return new LoanAccrualBatchResultDto
        {
            AsOfDate = asOfDate,
            LoansProcessed = loans.Count,
            TotalInterestAccrued = totalInterest,
            TotalPenaltyAccrued = totalPenalty,
            JournalIds = journalIds
        };
    }

    public async Task<LoanDto> WriteOffLoanAsync(LoanWriteOffRequest request, string? userId)
    {
        var loan = await _context.Loans.FirstOrDefaultAsync(l => l.Id == request.LoanId)
            ?? throw new InvalidOperationException("Loan not found");

        var writeOffAmount = Math.Min(request.Amount, loan.OutstandingBalance ?? 0m);
        if (writeOffAmount <= 0m)
        {
            throw new InvalidOperationException("No outstanding amount to write off");
        }

        loan.OutstandingBalance = Math.Round((loan.OutstandingBalance ?? 0m) - writeOffAmount, 2, MidpointRounding.AwayFromZero);
        loan.Status = loan.OutstandingBalance <= 0m ? "WRITTEN_OFF" : loan.Status;

        _context.LoanImpairments.Add(new LoanImpairment
        {
            LoanId = loan.Id,
            Stage = LoanImpairmentStatus.WrittenOff,
            AllowanceAmount = writeOffAmount,
            ImpairmentExpense = writeOffAmount,
            IsWrittenOff = true,
            WrittenOffAt = DateTime.UtcNow,
            RecoveryAmount = 0m,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        await _loanAccountingPostingService.PostEventAsync(
            loan,
            LoanAccountingEventType.WriteOff,
            writeOffAmount,
            await ResolveStaffIdAsync(userId),
            $"Loan write-off for {loan.Id}");

        await _auditLoggingService.LogActionAsync(
            "LOAN_WRITEOFF",
            "LOAN",
            loan.Id,
            await ResolveStaffIdAsync(userId),
            request.Reason,
            status: "SUCCESS",
            newValues: new { writeOffAmount });

        return MapLoanDto(loan);
    }

    public async Task<LoanDto> RecoverWrittenOffLoanAsync(LoanRecoveryRequest request, string? userId)
    {
        var loan = await _context.Loans.FirstOrDefaultAsync(l => l.Id == request.LoanId)
            ?? throw new InvalidOperationException("Loan not found");

        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == request.AccountId)
            ?? throw new InvalidOperationException("Recovery account not found");

        if ((account.Balance - account.LienAmount) < request.Amount)
        {
            throw new InvalidOperationException("Insufficient funds for recovery posting");
        }

        account.Balance -= request.Amount;
        loan.OutstandingBalance = Math.Max(0m, Math.Round((loan.OutstandingBalance ?? 0m) - request.Amount, 2, MidpointRounding.AwayFromZero));
        if (loan.OutstandingBalance <= 0m && string.Equals(loan.Status, "WRITTEN_OFF", StringComparison.OrdinalIgnoreCase))
        {
            loan.Status = "RECOVERED";
        }

        var impairment = await _context.LoanImpairments
            .Where(i => i.LoanId == loan.Id)
            .OrderByDescending(i => i.CreatedAt)
            .FirstOrDefaultAsync();

        if (impairment != null)
        {
            impairment.RecoveryAmount += request.Amount;
        }

        await _context.SaveChangesAsync();

        await _loanAccountingPostingService.PostEventAsync(
            loan,
            LoanAccountingEventType.Recovery,
            request.Amount,
            await ResolveStaffIdAsync(userId),
            $"Loan recovery for {loan.Id}");

        return MapLoanDto(loan);
    }

    public async Task<LoanStatementDto> GetLoanStatementAsync(string loanId)
    {
        var loan = await _context.Loans.Include(l => l.Schedules).FirstOrDefaultAsync(l => l.Id == loanId)
            ?? throw new InvalidOperationException("Loan not found");

        var repayments = await _context.LoanRepayments.Where(r => r.LoanId == loanId && !r.IsReversal).ToListAsync();

        var schedule = await GetLoanScheduleAsync(loanId);

        return new LoanStatementDto
        {
            LoanId = loan.Id,
            CustomerId = loan.CustomerId ?? string.Empty,
            Principal = loan.Principal,
            OutstandingBalance = loan.OutstandingBalance ?? 0m,
            TotalInterestPaid = repayments.Sum(r => r.InterestComponent),
            TotalPenaltyPaid = repayments.Sum(r => r.PenaltyComponent),
            Status = loan.Status,
            Schedule = schedule
        };
    }

    public async Task<LoanDelinquencyDashboardDto> GetDelinquencyDashboardAsync()
    {
        var activeLoans = await _context.Loans
            .Where(l => l.Status == "ACTIVE" || l.Status == "APPROVED" || l.Status == "WRITTEN_OFF")
            .Include(l => l.Schedules)
            .ToListAsync();

        var aging = new Dictionary<string, int>
        {
            ["0"] = 0,
            ["1-30"] = 0,
            ["31-90"] = 0,
            ["91-180"] = 0,
            ["180+"] = 0
        };

        decimal par30Balance = 0m;
        decimal par90Balance = 0m;

        foreach (var loan in activeLoans)
        {
            var dpd = CalculateDaysPastDue(loan.Schedules, DateOnly.FromDateTime(DateTime.UtcNow));
            var bucket = DetermineParBucket(dpd);
            aging[bucket] = aging.GetValueOrDefault(bucket) + 1;

            if (dpd > 30) par30Balance += loan.OutstandingBalance ?? 0m;
            if (dpd > 90) par90Balance += loan.OutstandingBalance ?? 0m;

            var loanAccount = await _context.LoanAccounts.FirstOrDefaultAsync(a => a.LoanId == loan.Id);
            if (loanAccount != null)
            {
                loanAccount.DelinquencyDays = dpd;
                loanAccount.ArrearsBucket = bucket;
                loanAccount.IsNonAccrual = dpd > 90;
                loanAccount.IsSuspendedInterest = dpd > 180;
                loanAccount.LastReviewedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();

        var totalOutstanding = activeLoans.Sum(l => l.OutstandingBalance ?? 0m);
        var nonAccrual = await _context.LoanAccounts.CountAsync(a => a.IsNonAccrual);

        return new LoanDelinquencyDashboardDto
        {
            TotalActiveLoans = activeLoans.Count,
            NonAccrualLoans = nonAccrual,
            PortfolioAtRisk30 = totalOutstanding <= 0 ? 0 : Math.Round(par30Balance / totalOutstanding * 100m, 2),
            PortfolioAtRisk90 = totalOutstanding <= 0 ? 0 : Math.Round(par90Balance / totalOutstanding * 100m, 2),
            AgingBuckets = aging
        };
    }

    public async Task<LoanProfitabilityReportDto> GetLoanProfitabilityReportAsync(DateOnly fromDate, DateOnly toDate)
    {
        var from = fromDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var to = toDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var loanSet = await _context.Loans
            .Where(l => l.DisbursementDate.HasValue && l.DisbursementDate >= fromDate && l.DisbursementDate <= toDate)
            .ToListAsync();

        var repayments = await _context.LoanRepayments
            .Where(r => r.RepaymentDate >= from && r.RepaymentDate <= to && !r.IsReversal)
            .ToListAsync();

        var impairments = await _context.LoanImpairments
            .Where(i => i.CreatedAt >= from && i.CreatedAt <= to)
            .ToListAsync();

        var byProduct = loanSet
            .GroupBy(l => l.LoanProductId ?? "UNMAPPED")
            .Select(group =>
            {
                var ids = group.Select(x => x.Id).ToHashSet();
                var interestIncome = repayments.Where(r => ids.Contains(r.LoanId)).Sum(r => r.InterestComponent);
                var penaltyIncome = repayments.Where(r => ids.Contains(r.LoanId)).Sum(r => r.PenaltyComponent);
                var impairmentExpense = impairments.Where(i => ids.Contains(i.LoanId)).Sum(i => i.ImpairmentExpense);
                var recovery = impairments.Where(i => ids.Contains(i.LoanId)).Sum(i => i.RecoveryAmount);
                var processingFees = group.Sum(g => Math.Round(g.Principal * 0.01m, 2));

                return new LoanProfitabilityItemDto
                {
                    GroupingKey = group.Key,
                    InterestIncome = interestIncome,
                    ProcessingFeeIncome = processingFees,
                    PenaltyIncome = penaltyIncome,
                    ImpairmentExpense = impairmentExpense,
                    RecoveryIncome = recovery,
                    NetContribution = interestIncome + processingFees + penaltyIncome + recovery - impairmentExpense
                };
            })
            .OrderByDescending(x => x.NetContribution)
            .ToList();

        var byBranch = new List<LoanProfitabilityItemDto>
        {
            new()
            {
                GroupingKey = "BR001",
                InterestIncome = byProduct.Sum(p => p.InterestIncome),
                ProcessingFeeIncome = byProduct.Sum(p => p.ProcessingFeeIncome),
                PenaltyIncome = byProduct.Sum(p => p.PenaltyIncome),
                ImpairmentExpense = byProduct.Sum(p => p.ImpairmentExpense),
                RecoveryIncome = byProduct.Sum(p => p.RecoveryIncome),
                NetContribution = byProduct.Sum(p => p.NetContribution)
            }
        };

        return new LoanProfitabilityReportDto
        {
            FromDate = fromDate,
            ToDate = toDate,
            ProductLevel = byProduct,
            BranchLevel = byBranch
        };
    }

    public async Task<LoanBalanceSheetReportDto> GetLoanBalanceSheetReportAsync(DateOnly asOfDate)
    {
        var loans = await _context.Loans
            .Where(l => l.Status == "ACTIVE" || l.Status == "APPROVED" || l.Status == "WRITTEN_OFF" || l.Status == "RECOVERED")
            .ToListAsync();

        var accruals = await _context.LoanAccruals.Where(a => a.AccrualDate <= asOfDate).ToListAsync();
        var impairments = await _context.LoanImpairments.Where(i => i.CreatedAt <= asOfDate.ToDateTime(TimeOnly.MaxValue)).ToListAsync();

        var gross = loans.Sum(l => l.OutstandingBalance ?? 0m);
        var interestReceivable = accruals.Sum(a => a.InterestAccrued);
        var penaltyReceivable = accruals.Sum(a => a.PenaltyAccrued);
        var allowance = impairments.Sum(i => i.AllowanceAmount - i.RecoveryAmount);
        var net = gross + interestReceivable + penaltyReceivable - allowance;

        var contribution = new LoanProfitabilityItemDto
        {
            GroupingKey = "BR001",
            InterestIncome = interestReceivable,
            PenaltyIncome = penaltyReceivable,
            ImpairmentExpense = allowance,
            NetContribution = net
        };

        return new LoanBalanceSheetReportDto
        {
            AsOfDate = asOfDate,
            Total = new LoanBalanceSheetSummaryDto
            {
                GrossLoanPortfolio = gross,
                AccruedInterestReceivable = interestReceivable,
                AccruedPenaltyReceivable = penaltyReceivable,
                ImpairmentAllowance = allowance,
                NetLoanPortfolio = net
            },
            BranchContributions = new List<LoanProfitabilityItemDto> { contribution }
        };
    }

    public async Task<List<LoanGlPostingDto>> GetLoanGlPostingsAsync(string loanId)
    {
        var entries = await _loanAccountingPostingService.GetLoanPostingsAsync(loanId);

        return entries.Select(e => new LoanGlPostingDto
        {
            JournalId = e.Id,
            Reference = e.Reference,
            Description = e.Description,
            CreatedAt = e.CreatedAt,
            Lines = e.Lines.Select(l => new LoanGlLineDto
            {
                AccountCode = l.AccountCode ?? string.Empty,
                Debit = l.Debit,
                Credit = l.Credit
            }).ToList()
        }).ToList();
    }

    public List<CreditBureauProviderDto> GetCreditBureauProviders()
    {
        return _creditBureauService
            .GetAvailableProviders()
            .Select(p => new CreditBureauProviderDto { ProviderName = p })
            .ToList();
    }

    private static LoanInterestMethod ParseInterestMethod(string? value)
    {
        return Enum.TryParse<LoanInterestMethod>(value ?? string.Empty, true, out var method)
            ? method
            : LoanInterestMethod.Flat;
    }

    private static LoanRepaymentFrequency ParseRepaymentFrequency(string? value)
    {
        return Enum.TryParse<LoanRepaymentFrequency>(value ?? string.Empty, true, out var frequency)
            ? frequency
            : LoanRepaymentFrequency.Monthly;
    }

    private static DateOnly AddPeriod(DateOnly startDate, LoanRepaymentFrequency frequency, int period)
    {
        return frequency switch
        {
            LoanRepaymentFrequency.Weekly => startDate.AddDays(7 * period),
            _ => startDate.AddMonths(period)
        };
    }

    private static int NormalizeTermMonths(string repaymentFrequency, int termInPeriods)
    {
        var frequency = ParseRepaymentFrequency(repaymentFrequency);
        return frequency switch
        {
            LoanRepaymentFrequency.Weekly => Math.Max(1, (int)Math.Ceiling(termInPeriods / 4.0)),
            _ => termInPeriods
        };
    }

    private static int ResolveTermInPeriods(Loan loan)
    {
        var frequency = ParseRepaymentFrequency(loan.RepaymentFrequency);
        return frequency == LoanRepaymentFrequency.Weekly
            ? Math.Max(1, loan.TermMonths * 4)
            : Math.Max(1, loan.TermMonths);
    }

    private static void ValidateProductBusinessRules(LoanProduct product, ApplyLoanRequest request)
    {
        if (product.ProductType == LoanProductType.DigitalLoan30Days)
        {
            if (!request.ScheduleType.Equals(nameof(LoanRepaymentFrequency.Bullet), StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Digital loans must use bullet repayment schedule.");
            }

            if (request.TermInPeriods != 1)
            {
                throw new InvalidOperationException("Digital loans must have fixed 30-day tenor.");
            }
        }

        if (product.ProductType == LoanProductType.WeeklyGroupLoan &&
            !request.RepaymentFrequency.Equals(nameof(LoanRepaymentFrequency.Weekly), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Group loans must have weekly schedules.");
        }

        if ((product.ProductType == LoanProductType.MonthlyBusinessLoan || product.ProductType == LoanProductType.MonthlyConsumerLoan) &&
            !request.RepaymentFrequency.Equals(nameof(LoanRepaymentFrequency.Monthly), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Business and consumer loans must have monthly schedules.");
        }
    }

    private async Task<string?> ResolveStaffIdAsync(string? userClaim)
    {
        if (string.IsNullOrWhiteSpace(userClaim))
        {
            return null;
        }

        var trimmed = userClaim.Trim();

        var byId = await _context.Staff
            .Where(s => s.Id == trimmed)
            .Select(s => s.Id)
            .FirstOrDefaultAsync();

        if (!string.IsNullOrWhiteSpace(byId))
        {
            return byId;
        }

        var byEmail = await _context.Staff
            .Where(s => s.Email == trimmed)
            .Select(s => s.Id)
            .FirstOrDefaultAsync();

        return string.IsNullOrWhiteSpace(byEmail) ? null : byEmail;
    }

    private async Task<decimal> GetDecimalConfigAsync(string key, decimal defaultValue)
    {
        var value = await _context.SystemConfigs
            .Where(c => c.Key == key)
            .Select(c => c.Value)
            .FirstOrDefaultAsync();

        return decimal.TryParse(value, out var parsed) ? parsed : defaultValue;
    }
}










