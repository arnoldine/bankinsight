using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Entities;
using BankInsight.API.Security;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Services;

public class MicrofinanceService
{
    private readonly ApplicationDbContext _context;
    private readonly ILedgerEngine _ledgerEngine;
    private readonly LoanService _loanService;
    private readonly IAuditLoggingService _auditLoggingService;
    private readonly ICurrentUserContext _currentUser;

    public MicrofinanceService(
        ApplicationDbContext context,
        ILedgerEngine ledgerEngine,
        LoanService loanService,
        IAuditLoggingService auditLoggingService,
        ICurrentUserContext currentUser)
    {
        _context = context;
        _ledgerEngine = ledgerEngine;
        _loanService = loanService;
        _auditLoggingService = auditLoggingService;
        _currentUser = currentUser;
    }

    public async Task<MicrofinanceSummaryDto> GetSummaryAsync(string? collectorStaffId = null, CancellationToken cancellationToken = default)
    {
        var resolvedCollectorId = ResolveCollectorStaffId(collectorStaffId);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var assignments = await _context.Set<CollectorPortfolioAssignment>()
            .AsNoTracking()
            .Include(a => a.Customer)
            .Include(a => a.Account)
            .Include(a => a.CollectorStaff)
            .Include(a => a.LoanProduct)
            .Where(a => a.Status == "ACTIVE")
            .Where(a => string.IsNullOrWhiteSpace(resolvedCollectorId) || a.CollectorStaffId == resolvedCollectorId)
            .OrderBy(a => a.NextCollectionDate)
            .ThenBy(a => a.Customer!.Name)
            .ToListAsync(cancellationToken);

        var activeBatch = await _context.Set<FieldCollectionBatch>()
            .AsNoTracking()
            .Include(b => b.CollectorStaff)
            .Include(b => b.Lines)
                .ThenInclude(l => l.Customer)
            .Where(b =>
                (b.Status == "OPEN" || b.Status == "SUBMITTED") &&
                (string.IsNullOrWhiteSpace(resolvedCollectorId) || b.CollectorStaffId == resolvedCollectorId))
            .OrderByDescending(b => b.BatchDate)
            .ThenByDescending(b => b.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var staffDirectory = await _context.Staff
            .AsNoTracking()
            .Where(s => s.Status == "Active")
            .OrderBy(s => s.Name)
            .Select(s => new FieldStaffDirectoryItemDto
            {
                StaffId = s.Id,
                Name = s.Name,
                Email = s.Email,
                BranchId = s.BranchId,
                Status = s.Status
            })
            .ToListAsync(cancellationToken);

        var mappedAssignments = assignments.Select(MapAssignment).ToList();
        var dueToday = mappedAssignments
            .Where(a => !a.NextCollectionDate.HasValue || a.NextCollectionDate.Value <= today)
            .OrderBy(a => a.NextCollectionDate)
            .ThenBy(a => a.CustomerName)
            .ToList();

        var compulsorySavingsAlerts = new List<CompulsorySavingsAlertDto>();
        foreach (var assignment in assignments.Where(a => !string.IsNullOrWhiteSpace(a.LoanProductId)))
        {
            var assessment = await _loanService.EvaluateCompulsorySavingsAsync(
                assignment.CustomerId,
                assignment.LoanProductId!,
                assignment.TargetAmount,
                cancellationToken);

            if (assessment.RequiresCompulsorySavings && !assessment.IsEligible)
            {
                compulsorySavingsAlerts.Add(new CompulsorySavingsAlertDto
                {
                    CustomerId = assignment.CustomerId,
                    CustomerName = assignment.Customer?.Name ?? assignment.CustomerId,
                    LoanProductId = assignment.LoanProductId!,
                    LoanProductName = assignment.LoanProduct?.Name ?? assignment.LoanProductId!,
                    ExampleLoanAmount = assignment.TargetAmount,
                    RequiredSavingsBalance = assessment.RequiredSavingsBalance,
                    AvailableSavingsBalance = assessment.AvailableSavingsBalance,
                    Shortfall = assessment.Shortfall,
                    Recommendation = assessment.Recommendation
                });
            }
        }

        var loanPolicies = await GetLoanPoliciesAsync(cancellationToken);
        var totalCollectedToday = activeBatch?.CollectedAmount ?? 0m;

        return new MicrofinanceSummaryDto
        {
            BusinessDate = today,
            Metrics =
            [
                new() { Key = "assignments", Label = "Active Assignments", Value = mappedAssignments.Count.ToString("N0"), Severity = "INFO", Subtitle = "Collector and field collection portfolios" },
                new() { Key = "dueToday", Label = "Due Today", Value = dueToday.Count.ToString("N0"), Severity = dueToday.Count > 0 ? "WARN" : "INFO", Subtitle = "Customers scheduled for collection today" },
                new() { Key = "activeBatch", Label = "Open Batch", Value = activeBatch is null ? "No" : "Yes", Severity = activeBatch is null ? "INFO" : "SUCCESS", Subtitle = activeBatch is null ? "No active collector batch" : $"Batch {activeBatch.Id}" },
                new() { Key = "collectedToday", Label = "Collected Today", Value = totalCollectedToday.ToString("N2"), Severity = totalCollectedToday > 0 ? "SUCCESS" : "INFO", Subtitle = "Savings and repayments posted via field collections" },
                new() { Key = "compulsoryAlerts", Label = "Savings Shortfalls", Value = compulsorySavingsAlerts.Count.ToString("N0"), Severity = compulsorySavingsAlerts.Count > 0 ? "WARN" : "SUCCESS", Subtitle = "Customers below compulsory-savings threshold" }
            ],
            Assignments = mappedAssignments,
            DueToday = dueToday,
            ActiveBatch = activeBatch is null ? null : MapBatch(activeBatch),
            StaffDirectory = staffDirectory,
            LoanPolicies = loanPolicies,
            CompulsorySavingsAlerts = compulsorySavingsAlerts
        };
    }

    public async Task<List<CustomerSearchItemDto>> SearchCustomersAsync(string query, CancellationToken cancellationToken = default)
    {
        var normalized = (query ?? string.Empty).Trim();
        if (normalized.Length < 2)
        {
            return new List<CustomerSearchItemDto>();
        }

        var lookup = normalized.ToUpperInvariant();
        return await _context.Customers
            .AsNoTracking()
            .Where(c =>
                c.Id.ToUpper().Contains(lookup) ||
                c.Name.ToUpper().Contains(lookup) ||
                (c.Phone != null && c.Phone.Contains(normalized)) ||
                (c.GhanaCard != null && c.GhanaCard.ToUpper().Contains(lookup)))
            .OrderBy(c => c.Name)
            .Take(20)
            .Select(c => new CustomerSearchItemDto
            {
                CustomerId = c.Id,
                CustomerName = c.Name,
                Phone = c.Phone,
                GhanaCard = c.GhanaCard,
                BranchId = c.BranchId
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<AccountSearchItemDto>> SearchAccountsAsync(string query, string? customerId = null, CancellationToken cancellationToken = default)
    {
        var normalized = (query ?? string.Empty).Trim();
        if (normalized.Length < 2)
        {
            return new List<AccountSearchItemDto>();
        }

        var lookup = normalized.ToUpperInvariant();
        return await _context.Accounts
            .AsNoTracking()
            .Include(a => a.Customer)
            .Where(a =>
                a.Id.ToUpper().Contains(lookup) ||
                (a.CustomerId != null && a.CustomerId.ToUpper().Contains(lookup)) ||
                (a.Customer != null && a.Customer.Name.ToUpper().Contains(lookup)))
            .Where(a => string.IsNullOrWhiteSpace(customerId) || a.CustomerId == customerId)
            .OrderBy(a => a.Id)
            .Take(20)
            .Select(a => new AccountSearchItemDto
            {
                AccountId = a.Id,
                CustomerId = a.CustomerId ?? string.Empty,
                CustomerName = a.Customer != null ? a.Customer.Name : (a.CustomerId ?? string.Empty),
                Type = a.Type,
                Currency = a.Currency,
                Balance = a.Balance,
                LienAmount = a.LienAmount,
                Status = a.Status,
                ProductCode = a.ProductCode,
                IsCompulsorySavings = a.ProductCode != null && a.ProductCode.ToUpper().Contains("COMP")
            })
            .ToListAsync(cancellationToken);
    }

    public Task<List<MicrofinanceLoanPolicyDto>> GetLoanPoliciesAsync(CancellationToken cancellationToken = default)
        => BuildLoanPoliciesAsync(cancellationToken);

    public async Task<CollectorAssignmentDto> UpsertAssignmentAsync(UpsertCollectorAssignmentRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == request.CustomerId.Trim(), cancellationToken)
            ?? throw new InvalidOperationException("Customer not found.");
        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == request.AccountId.Trim(), cancellationToken)
            ?? throw new InvalidOperationException("Savings account not found.");

        if (!string.Equals(account.CustomerId, customer.Id, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Selected account does not belong to the selected customer.");
        }

        LoanProduct? loanProduct = null;
        if (!string.IsNullOrWhiteSpace(request.LoanProductId))
        {
            loanProduct = await _context.LoanProducts.FirstOrDefaultAsync(lp => lp.Id == request.LoanProductId.Trim(), cancellationToken)
                ?? throw new InvalidOperationException("Loan product not found.");
        }

        CollectorPortfolioAssignment assignment;
        if (string.IsNullOrWhiteSpace(request.Id))
        {
            assignment = new CollectorPortfolioAssignment
            {
                Id = $"COLL-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}",
                CreatedAt = DateTime.UtcNow
            };
            _context.Add(assignment);
        }
        else
        {
            assignment = await _context.Set<CollectorPortfolioAssignment>()
                .Include(a => a.Customer)
                .Include(a => a.Account)
                .Include(a => a.CollectorStaff)
                .Include(a => a.LoanProduct)
                .FirstOrDefaultAsync(a => a.Id == request.Id.Trim(), cancellationToken)
                ?? throw new InvalidOperationException("Assignment not found.");
        }

        assignment.CustomerId = customer.Id;
        assignment.AccountId = account.Id;
        assignment.CollectorStaffId = string.IsNullOrWhiteSpace(request.CollectorStaffId) ? _currentUser.UserId : request.CollectorStaffId.Trim();
        assignment.LoanProductId = string.IsNullOrWhiteSpace(request.LoanProductId) ? null : request.LoanProductId.Trim();
        assignment.CollectionType = request.CollectionType.Trim().ToUpperInvariant();
        assignment.Frequency = request.Frequency.Trim().ToUpperInvariant();
        assignment.TargetAmount = request.TargetAmount;
        assignment.MinimumContributionAmount = request.MinimumContributionAmount;
        assignment.RouteName = string.IsNullOrWhiteSpace(request.RouteName) ? null : request.RouteName.Trim();
        assignment.MeetingDay = string.IsNullOrWhiteSpace(request.MeetingDay) ? null : request.MeetingDay.Trim();
        assignment.Status = request.Status.Trim().ToUpperInvariant();
        assignment.NextCollectionDate = request.NextCollectionDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        assignment.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        assignment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditLoggingService.LogActionAsync(
            action: "MICROFINANCE_ASSIGNMENT_UPSERT",
            entityType: "COLLECTOR_ASSIGNMENT",
            entityId: assignment.Id,
            userId: _currentUser.UserId,
            description: $"Collector assignment saved for customer {customer.Id} on account {account.Id}",
            status: "SUCCESS",
            newValues: new { assignment.Id, assignment.CustomerId, assignment.AccountId, assignment.CollectorStaffId, assignment.CollectionType, assignment.Frequency, assignment.TargetAmount });

        assignment.Customer = customer;
        assignment.Account = account;
        assignment.LoanProduct = loanProduct;
        assignment.CollectorStaff = !string.IsNullOrWhiteSpace(assignment.CollectorStaffId)
            ? await _context.Staff.FirstOrDefaultAsync(s => s.Id == assignment.CollectorStaffId, cancellationToken)
            : null;

        return MapAssignment(assignment);
    }

    public async Task<FieldCollectionBatchDto> OpenBatchAsync(OpenFieldCollectionBatchRequest request, CancellationToken cancellationToken = default)
    {
        var collectorId = ResolveCollectorStaffId(request.CollectorStaffId)
            ?? throw new InvalidOperationException("Collector staff is required to open a batch.");
        var batchDate = request.BatchDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var existing = await _context.Set<FieldCollectionBatch>()
            .Include(b => b.CollectorStaff)
            .Include(b => b.Lines)
                .ThenInclude(l => l.Customer)
            .FirstOrDefaultAsync(b => b.CollectorStaffId == collectorId && b.BatchDate == batchDate && b.Status == "OPEN", cancellationToken);

        if (existing != null)
        {
            return MapBatch(existing);
        }

        var expectedAmount = await _context.Set<CollectorPortfolioAssignment>()
            .AsNoTracking()
            .Where(a => a.CollectorStaffId == collectorId && a.Status == "ACTIVE" && (!a.NextCollectionDate.HasValue || a.NextCollectionDate.Value <= batchDate))
            .SumAsync(a => (decimal?)a.TargetAmount, cancellationToken) ?? 0m;

        var batch = new FieldCollectionBatch
        {
            Id = $"FCB-{batchDate:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
            CollectorStaffId = collectorId,
            BranchId = string.IsNullOrWhiteSpace(request.BranchId) ? _currentUser.BranchId : request.BranchId.Trim(),
            BatchDate = batchDate,
            RouteName = string.IsNullOrWhiteSpace(request.RouteName) ? null : request.RouteName.Trim(),
            OpeningFloat = request.OpeningFloat,
            ExpectedAmount = expectedAmount,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim()
        };

        _context.Add(batch);
        await _context.SaveChangesAsync(cancellationToken);

        batch.CollectorStaff = await _context.Staff.FirstOrDefaultAsync(s => s.Id == collectorId, cancellationToken);

        return MapBatch(batch);
    }

    public async Task<FieldCollectionBatchDto> RecordCollectionAsync(string batchId, RecordFieldCollectionRequest request, CancellationToken cancellationToken = default)
    {
        var batch = await _context.Set<FieldCollectionBatch>()
            .Include(b => b.CollectorStaff)
            .Include(b => b.Lines)
                .ThenInclude(l => l.Customer)
            .FirstOrDefaultAsync(b => b.Id == batchId, cancellationToken)
            ?? throw new InvalidOperationException("Field collection batch not found.");

        if (!string.Equals(batch.Status, "OPEN", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only open batches can accept new collection lines.");
        }

        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == request.CustomerId.Trim(), cancellationToken)
            ?? throw new InvalidOperationException("Customer not found.");
        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == request.AccountId.Trim(), cancellationToken)
            ?? throw new InvalidOperationException("Target account not found.");

        if (!string.Equals(account.CustomerId, customer.Id, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The target account does not belong to the selected customer.");
        }

        CollectorPortfolioAssignment? assignment = null;
        if (!string.IsNullOrWhiteSpace(request.AssignmentId))
        {
            assignment = await _context.Set<CollectorPortfolioAssignment>()
                .FirstOrDefaultAsync(a => a.Id == request.AssignmentId.Trim(), cancellationToken)
                ?? throw new InvalidOperationException("Assignment not found.");
        }

        string? postedTransactionId = null;
        var normalizedType = request.TransactionType.Trim().ToUpperInvariant();
        if (!request.MarkAsMissed)
        {
            if (request.Amount <= 0m)
            {
                throw new InvalidOperationException("Amount must be greater than zero for a posted collection.");
            }

            if (normalizedType == "LOAN_REPAYMENT")
            {
                if (string.IsNullOrWhiteSpace(request.LoanId))
                {
                    throw new InvalidOperationException("LoanId is required for loan repayment field collections.");
                }

                var clientReference = $"FIELD-{batch.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";
                await _loanService.RepayLoanAsync(
                    request.LoanId.Trim(),
                    new LoanRepayRequest
                    {
                        AccountId = account.Id,
                        Amount = request.Amount,
                        ClientReference = clientReference
                    },
                    bypassAccessCheck: true);

                postedTransactionId = clientReference;
            }
            else
            {
                var postingResult = await _ledgerEngine.PostDepositAsync(new DepositRequest
                {
                    AccountId = account.Id,
                    CustomerId = customer.Id,
                    Amount = request.Amount,
                    DepositMethod = "CASH",
                    Narration = string.IsNullOrWhiteSpace(request.Narration)
                        ? $"{normalizedType.Replace('_', ' ')} collected via field operations"
                        : request.Narration.Trim(),
                    TellerId = batch.CollectorStaffId ?? _currentUser.UserId ?? string.Empty,
                    CustomerGhanaCard = customer.GhanaCard ?? string.Empty,
                    BranchId = batch.BranchId ?? customer.BranchId
                });

                if (!postingResult.Success)
                {
                    throw new InvalidOperationException(postingResult.Message);
                }

                postedTransactionId = postingResult.TransactionId;
            }
        }

        var line = new FieldCollectionBatchLine
        {
            BatchId = batch.Id,
            AssignmentId = assignment?.Id,
            CustomerId = customer.Id,
            AccountId = account.Id,
            LoanId = string.IsNullOrWhiteSpace(request.LoanId) ? null : request.LoanId.Trim(),
            TransactionType = normalizedType,
            Amount = request.MarkAsMissed ? 0m : request.Amount,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? account.Currency : request.Currency.Trim().ToUpperInvariant(),
            Status = request.MarkAsMissed ? "MISSED" : "POSTED",
            Narration = string.IsNullOrWhiteSpace(request.Narration)
                ? (request.MarkAsMissed ? "Marked as missed collection" : $"{normalizedType.Replace('_', ' ')} collected via field operations")
                : request.Narration.Trim(),
            PostedTransactionId = postedTransactionId,
            DueAmount = request.DueAmount,
            WasMissed = request.MarkAsMissed,
            CollectedAt = DateTime.UtcNow
        };

        _context.Add(line);

        if (!request.MarkAsMissed)
        {
            batch.CollectedAmount = Math.Round(batch.CollectedAmount + request.Amount, 2, MidpointRounding.AwayFromZero);
        }

        if (assignment != null)
        {
            assignment.LastCollectionAt = DateTime.UtcNow;
            assignment.NextCollectionDate = GetNextCollectionDate(batch.BatchDate, assignment.Frequency);
            assignment.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _auditLoggingService.LogActionAsync(
            action: request.MarkAsMissed ? "FIELD_COLLECTION_MISSED" : "FIELD_COLLECTION_POSTED",
            entityType: "FIELD_COLLECTION_BATCH",
            entityId: batch.Id,
            userId: _currentUser.UserId,
            description: $"{normalizedType} recorded for customer {customer.Id} in batch {batch.Id}",
            status: "SUCCESS",
            newValues: new
            {
                BatchId = batch.Id,
                CustomerId = customer.Id,
                AccountId = account.Id,
                request.LoanId,
                TransactionType = normalizedType,
                request.Amount,
                request.MarkAsMissed,
                postedTransactionId
            });

        var refreshed = await _context.Set<FieldCollectionBatch>()
            .AsNoTracking()
            .Include(b => b.CollectorStaff)
            .Include(b => b.Lines)
                .ThenInclude(l => l.Customer)
            .FirstAsync(b => b.Id == batch.Id, cancellationToken);

        return MapBatch(refreshed);
    }

    public async Task<FieldCollectionBatchDto> SubmitBatchAsync(string batchId, SubmitFieldCollectionBatchRequest request, CancellationToken cancellationToken = default)
    {
        var batch = await _context.Set<FieldCollectionBatch>()
            .Include(b => b.CollectorStaff)
            .Include(b => b.Lines)
                .ThenInclude(l => l.Customer)
            .FirstOrDefaultAsync(b => b.Id == batchId, cancellationToken)
            ?? throw new InvalidOperationException("Field collection batch not found.");

        if (!string.Equals(batch.Status, "OPEN", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only open batches can be submitted.");
        }

        batch.Status = "SUBMITTED";
        batch.SubmittedAt = DateTime.UtcNow;
        batch.Notes = string.IsNullOrWhiteSpace(request.Notes)
            ? batch.Notes
            : string.Join(" | ", new[] { batch.Notes, request.Notes.Trim() }.Where(v => !string.IsNullOrWhiteSpace(v)));

        await _context.SaveChangesAsync(cancellationToken);
        return MapBatch(batch);
    }

    public async Task<FieldCollectionBatchDto> SettleBatchAsync(string batchId, SettleFieldCollectionBatchRequest request, CancellationToken cancellationToken = default)
    {
        var batch = await _context.Set<FieldCollectionBatch>()
            .Include(b => b.CollectorStaff)
            .Include(b => b.Lines)
                .ThenInclude(l => l.Customer)
            .FirstOrDefaultAsync(b => b.Id == batchId, cancellationToken)
            ?? throw new InvalidOperationException("Field collection batch not found.");

        if (string.Equals(batch.Status, "SETTLED", StringComparison.OrdinalIgnoreCase))
        {
            return MapBatch(batch);
        }

        batch.Status = "SETTLED";
        batch.SettledAmount = request.SettledAmount;
        batch.VarianceAmount = Math.Round(request.SettledAmount - batch.CollectedAmount, 2, MidpointRounding.AwayFromZero);
        batch.SettledAt = DateTime.UtcNow;
        batch.Notes = string.IsNullOrWhiteSpace(request.Notes)
            ? batch.Notes
            : string.Join(" | ", new[] { batch.Notes, request.Notes.Trim() }.Where(v => !string.IsNullOrWhiteSpace(v)));

        await _context.SaveChangesAsync(cancellationToken);

        await _auditLoggingService.LogActionAsync(
            action: "FIELD_COLLECTION_BATCH_SETTLED",
            entityType: "FIELD_COLLECTION_BATCH",
            entityId: batch.Id,
            userId: _currentUser.UserId,
            description: $"Field collection batch {batch.Id} settled",
            status: "SUCCESS",
            newValues: new { batch.Id, batch.CollectedAmount, batch.SettledAmount, batch.VarianceAmount });

        return MapBatch(batch);
    }

    private async Task<List<MicrofinanceLoanPolicyDto>> BuildLoanPoliciesAsync(CancellationToken cancellationToken)
    {
        var loanProducts = await _context.LoanProducts
            .AsNoTracking()
            .Where(lp => lp.IsActive)
            .OrderBy(lp => lp.Name)
            .ToListAsync(cancellationToken);

        var corePolicies = await _context.Products
            .AsNoTracking()
            .Where(p => p.Type == "LOAN")
            .ToListAsync(cancellationToken);

        return loanProducts.Select(lp =>
        {
            var policy = ResolveCompulsorySavingsPolicy(lp, corePolicies);
            return new MicrofinanceLoanPolicyDto
            {
                LoanProductId = lp.Id,
                Code = lp.Code,
                Name = lp.Name,
                RepaymentFrequency = lp.RepaymentFrequency.ToString(),
                InterestMethod = lp.InterestMethod.ToString(),
                RequiresCompulsorySavings = policy?.RequiresCompulsorySavings ?? false,
                MinimumSavingsToLoanRatio = policy?.MinimumSavingsToLoanRatio,
                SupportsWeeklyRepayment = policy?.SupportsWeeklyRepayment ?? lp.RepaymentFrequency == LoanRepaymentFrequency.Weekly
            };
        }).ToList();
    }

    private static Product? ResolveCompulsorySavingsPolicy(LoanProduct loanProduct, IEnumerable<Product> policies)
    {
        return policies.FirstOrDefault(p => string.Equals(p.Id, loanProduct.Id, StringComparison.OrdinalIgnoreCase))
            ?? policies.FirstOrDefault(p => string.Equals(p.Id, loanProduct.Code, StringComparison.OrdinalIgnoreCase))
            ?? policies.FirstOrDefault(p => string.Equals(p.Name, loanProduct.Name, StringComparison.OrdinalIgnoreCase));
    }

    private CollectorAssignmentDto MapAssignment(CollectorPortfolioAssignment assignment)
    {
        var availableBalance = assignment.Account is null
            ? 0m
            : Math.Max(0m, assignment.Account.Balance - assignment.Account.LienAmount);

        return new CollectorAssignmentDto
        {
            Id = assignment.Id,
            CustomerId = assignment.CustomerId,
            CustomerName = assignment.Customer?.Name ?? assignment.CustomerId,
            AccountId = assignment.AccountId,
            AccountType = assignment.Account?.Type ?? "SAVINGS",
            CollectorStaffId = assignment.CollectorStaffId ?? string.Empty,
            CollectorName = assignment.CollectorStaff?.Name ?? "Unassigned",
            LoanProductId = assignment.LoanProductId,
            LoanProductName = assignment.LoanProduct?.Name,
            CollectionType = assignment.CollectionType,
            Frequency = assignment.Frequency,
            TargetAmount = assignment.TargetAmount,
            MinimumContributionAmount = assignment.MinimumContributionAmount,
            RouteName = assignment.RouteName,
            MeetingDay = assignment.MeetingDay,
            Status = assignment.Status,
            NextCollectionDate = assignment.NextCollectionDate,
            LastCollectionAt = assignment.LastCollectionAt,
            Notes = assignment.Notes,
            AvailableSavingsBalance = availableBalance
        };
    }

    private FieldCollectionBatchDto MapBatch(FieldCollectionBatch batch)
    {
        return new FieldCollectionBatchDto
        {
            Id = batch.Id,
            CollectorStaffId = batch.CollectorStaffId ?? string.Empty,
            CollectorName = batch.CollectorStaff?.Name ?? "Unassigned",
            BranchId = batch.BranchId,
            BatchDate = batch.BatchDate,
            RouteName = batch.RouteName,
            Status = batch.Status,
            ExpectedAmount = batch.ExpectedAmount,
            CollectedAmount = batch.CollectedAmount,
            SettledAmount = batch.SettledAmount,
            VarianceAmount = batch.VarianceAmount,
            OpeningFloat = batch.OpeningFloat,
            Notes = batch.Notes,
            SubmittedAt = batch.SubmittedAt,
            SettledAt = batch.SettledAt,
            Lines = batch.Lines
                .OrderByDescending(l => l.CollectedAt)
                .Select(line => new FieldCollectionBatchLineDto
                {
                    Id = line.Id,
                    AssignmentId = line.AssignmentId,
                    CustomerId = line.CustomerId,
                    CustomerName = line.Customer?.Name ?? line.CustomerId,
                    AccountId = line.AccountId,
                    LoanId = line.LoanId,
                    TransactionType = line.TransactionType,
                    Amount = line.Amount,
                    Currency = line.Currency,
                    Status = line.Status,
                    Narration = line.Narration,
                    PostedTransactionId = line.PostedTransactionId,
                    DueAmount = line.DueAmount,
                    WasMissed = line.WasMissed,
                    CollectedAt = line.CollectedAt
                })
                .ToList()
        };
    }

    private string? ResolveCollectorStaffId(string? collectorStaffId)
        => string.IsNullOrWhiteSpace(collectorStaffId) ? _currentUser.UserId : collectorStaffId.Trim();

    private static DateOnly GetNextCollectionDate(DateOnly referenceDate, string frequency)
    {
        return frequency.Trim().ToUpperInvariant() switch
        {
            "DAILY" => referenceDate.AddDays(1),
            "WEEKLY" => referenceDate.AddDays(7),
            "BIWEEKLY" => referenceDate.AddDays(14),
            "MONTHLY" => referenceDate.AddMonths(1),
            _ => referenceDate.AddDays(1)
        };
    }
}
