namespace CoreBanker.Services
{
    public class LoanService : ApiClientBase
    {
        public LoanService(HttpClient httpClient, CoreBanker.State.AppState appState) : base(httpClient, appState) { }

        public async Task<List<LoanDto>> GetLoansAsync(CancellationToken cancellationToken = default)
        {
            var result = await GetAsync<List<LoanDto>>("/api/loans", cancellationToken);
            return result ?? new List<LoanDto>();
        }

        public async Task<List<LoanProductDefinitionDto>> GetLoanProductsAsync(CancellationToken cancellationToken = default)
        {
            var result = await GetAsync<List<LoanProductDefinitionDto>>("/api/loans/products", cancellationToken);
            return result ?? new List<LoanProductDefinitionDto>();
        }

        public async Task<List<CreditProviderDto>> GetCreditProvidersAsync(CancellationToken cancellationToken = default)
        {
            var result = await GetAsync<List<CreditProviderDto>>("/api/loans/credit-bureau/providers", cancellationToken);
            return result ?? new List<CreditProviderDto>();
        }

        public async Task<LoanDto?> ApplyLoanAsync(ApplyLoanRequest request, CancellationToken cancellationToken = default)
        {
            var normalizedRequest = new ApplyLoanRequest
            {
                CustomerId = request.CustomerId.Trim(),
                GroupId = string.IsNullOrWhiteSpace(request.GroupId) ? null : request.GroupId.Trim(),
                LoanProductId = request.LoanProductId.Trim(),
                Principal = request.Principal,
                ClientReference = string.IsNullOrWhiteSpace(request.ClientReference) ? null : request.ClientReference.Trim()
            };

            return await PostAsync<ApplyLoanRequest, LoanDto>("/api/loans/apply", normalizedRequest, cancellationToken);
        }

        public async Task<LoanDto?> ApproveLoanAsync(ApproveLoanRequest request, CancellationToken cancellationToken = default)
        {
            var normalizedRequest = new ApproveLoanRequest
            {
                LoanId = request.LoanId.Trim(),
                DecisionNotes = string.IsNullOrWhiteSpace(request.DecisionNotes) ? null : request.DecisionNotes.Trim()
            };

            return await PostAsync<ApproveLoanRequest, LoanDto>("/api/loans/approve", normalizedRequest, cancellationToken);
        }

        public async Task<LoanDto?> AppraiseLoanAsync(AppraiseLoanRequest request, CancellationToken cancellationToken = default)
        {
            var normalizedRequest = new AppraiseLoanRequest
            {
                LoanId = request.LoanId.Trim(),
                Decision = string.IsNullOrWhiteSpace(request.Decision) ? "APPRAISED" : request.Decision.Trim().ToUpperInvariant(),
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim()
            };

            return await PostAsync<AppraiseLoanRequest, LoanDto>("/api/loans/appraise", normalizedRequest, cancellationToken);
        }

        public async Task<CreditCheckDto?> CheckCreditAsync(CheckCreditRequest request, CancellationToken cancellationToken = default)
        {
            var normalizedRequest = new CheckCreditRequest
            {
                CustomerId = request.CustomerId.Trim(),
                LoanId = string.IsNullOrWhiteSpace(request.LoanId) ? null : request.LoanId.Trim(),
                ProviderName = string.IsNullOrWhiteSpace(request.ProviderName) ? null : request.ProviderName.Trim()
            };

            return await PostAsync<CheckCreditRequest, CreditCheckDto>("/api/loans/check-credit", normalizedRequest, cancellationToken);
        }

        public async Task<LoanScheduleGenerationResultDto?> GenerateScheduleAsync(GenerateLoanScheduleRequest request, CancellationToken cancellationToken = default)
        {
            var normalizedRequest = new GenerateLoanScheduleRequest
            {
                LoanProductId = string.IsNullOrWhiteSpace(request.LoanProductId) ? null : request.LoanProductId.Trim(),
                Principal = request.Principal,
                AnnualInterestRate = request.AnnualInterestRate,
                TermInPeriods = request.TermInPeriods,
                InterestMethod = string.IsNullOrWhiteSpace(request.InterestMethod) ? "Flat" : request.InterestMethod.Trim(),
                RepaymentFrequency = string.IsNullOrWhiteSpace(request.RepaymentFrequency) ? "Monthly" : request.RepaymentFrequency.Trim(),
                ScheduleType = string.IsNullOrWhiteSpace(request.ScheduleType) ? "Monthly" : request.ScheduleType.Trim(),
                StartDate = request.StartDate
            };

            return await PostAsync<GenerateLoanScheduleRequest, LoanScheduleGenerationResultDto>("/api/loans/generate-schedule", normalizedRequest, cancellationToken);
        }

        public async Task<List<LoanScheduleDto>> GetLoanScheduleAsync(string loanId, CancellationToken cancellationToken = default)
        {
            var result = await GetAsync<List<LoanScheduleDto>>($"/api/loans/{Uri.EscapeDataString(loanId)}/schedule", cancellationToken);
            return result ?? new List<LoanScheduleDto>();
        }

        public async Task<LoanDto?> RepayLoanAsync(RepayLoanRequest request, CancellationToken cancellationToken = default)
        {
            var normalizedRequest = new RepayLoanRequest
            {
                LoanId = request.LoanId.Trim(),
                AccountId = request.AccountId.Trim(),
                Amount = request.Amount,
                ClientReference = string.IsNullOrWhiteSpace(request.ClientReference) ? null : request.ClientReference.Trim()
            };

            return await PostAsync<RepayLoanRequest, LoanDto>("/api/loans/repay", normalizedRequest, cancellationToken);
        }

        public async Task<LoanPenaltyDto?> AssessPenaltyAsync(string loanId, AssessLoanPenaltyRequest request, CancellationToken cancellationToken = default)
        {
            return await PostAsync<AssessLoanPenaltyRequest, LoanPenaltyDto>($"/api/loans/{Uri.EscapeDataString(loanId)}/penalty", request, cancellationToken);
        }

        public async Task<LoanClassificationDto?> ClassifyLoanAsync(string loanId, CancellationToken cancellationToken = default)
        {
            return await PostAsync<object, LoanClassificationDto>($"/api/loans/{Uri.EscapeDataString(loanId)}/classify", new { }, cancellationToken);
        }
    }

    public class LoanDto
    {
        public string Id { get; set; } = string.Empty;
        public string Cif { get; set; } = string.Empty;
        public string? GroupId { get; set; }
        public string? ProductCode { get; set; }
        public string? ProductName { get; set; }
        public decimal Principal { get; set; }
        public decimal Rate { get; set; }
        public int TermMonths { get; set; }
        public DateOnly? DisbursementDate { get; set; }
        public string ParBucket { get; set; } = string.Empty;
        public decimal? OutstandingBalance { get; set; }
        public string? CollateralType { get; set; }
        public decimal? CollateralValue { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? InterestMethod { get; set; }
        public string? RepaymentFrequency { get; set; }
        public string? ScheduleType { get; set; }
        public string? LoanProductId { get; set; }
    }

    public class LoanProductDefinitionDto
    {
        public string Id { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ProductType { get; set; } = string.Empty;
        public string InterestMethod { get; set; } = string.Empty;
        public string RepaymentFrequency { get; set; } = string.Empty;
        public int TermInPeriods { get; set; }
        public decimal AnnualInterestRate { get; set; }
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
        public bool IsActive { get; set; }
    }

    public class ApplyLoanRequest
    {
        public string CustomerId { get; set; } = string.Empty;
        public string? GroupId { get; set; }
        public string LoanProductId { get; set; } = string.Empty;
        public decimal Principal { get; set; }
        public string? ClientReference { get; set; }
    }

    public class ApproveLoanRequest
    {
        public string LoanId { get; set; } = string.Empty;
        public string? DecisionNotes { get; set; }
    }

    public class AppraiseLoanRequest
    {
        public string LoanId { get; set; } = string.Empty;
        public string Decision { get; set; } = "APPRAISED";
        public string? Notes { get; set; }
    }

    public class RepayLoanRequest
    {
        public string LoanId { get; set; } = string.Empty;
        public string AccountId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? ClientReference { get; set; }
    }

    public class CheckCreditRequest
    {
        public string CustomerId { get; set; } = string.Empty;
        public string? LoanId { get; set; }
        public string? ProviderName { get; set; }
    }

    public class CreditCheckDto
    {
        public string CustomerId { get; set; } = string.Empty;
        public string? LoanId { get; set; }
        public int Score { get; set; }
        public string RiskBand { get; set; } = string.Empty;
        public string RiskGrade { get; set; } = string.Empty;
        public string Decision { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
        public string ProviderName { get; set; } = string.Empty;
        public string InquiryReference { get; set; } = string.Empty;
        public DateTime CheckedAt { get; set; }
    }

    public class CreditProviderDto
    {
        public string ProviderName { get; set; } = string.Empty;
    }

    public class GenerateLoanScheduleRequest
    {
        public string? LoanProductId { get; set; }
        public decimal Principal { get; set; }
        public decimal AnnualInterestRate { get; set; }
        public int TermInPeriods { get; set; }
        public string InterestMethod { get; set; } = "Flat";
        public string RepaymentFrequency { get; set; } = "Monthly";
        public string ScheduleType { get; set; } = "Monthly";
        public DateOnly? StartDate { get; set; }
    }

    public class LoanScheduleGenerationResultDto
    {
        public decimal Principal { get; set; }
        public decimal AnnualInterestRate { get; set; }
        public string InterestMethod { get; set; } = string.Empty;
        public string RepaymentFrequency { get; set; } = string.Empty;
        public string ScheduleType { get; set; } = string.Empty;
        public List<LoanScheduleLineDto> Lines { get; set; } = new();
    }

    public class LoanScheduleLineDto
    {
        public int Period { get; set; }
        public DateOnly DueDate { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal Principal { get; set; }
        public decimal Interest { get; set; }
        public decimal Installment { get; set; }
        public decimal ClosingBalance { get; set; }
    }

    public class LoanScheduleDto
    {
        public int Period { get; set; }
        public DateOnly DueDate { get; set; }
        public decimal Principal { get; set; }
        public decimal Interest { get; set; }
        public decimal Total { get; set; }
        public decimal Balance { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateOnly? PaidDate { get; set; }
    }

    public class AssessLoanPenaltyRequest
    {
        public decimal PenaltyRate { get; set; }
        public string? Reason { get; set; }
        public string? ClientReference { get; set; }
    }

    public class LoanPenaltyDto
    {
        public string LoanId { get; set; } = string.Empty;
        public decimal PenaltyAmount { get; set; }
        public decimal PenaltyRate { get; set; }
        public int DaysPastDue { get; set; }
        public decimal OutstandingBalance { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime AssessedAt { get; set; }
    }

    public class LoanClassificationDto
    {
        public string LoanId { get; set; } = string.Empty;
        public string BogTier { get; set; } = string.Empty;
        public int DaysPastDue { get; set; }
        public decimal OutstandingPrincipal { get; set; }
        public decimal OutstandingInterest { get; set; }
        public decimal ProvisioningAmount { get; set; }
        public decimal ProvisioningRate { get; set; }
        public DateTime EvaluationDate { get; set; }
    }
}
