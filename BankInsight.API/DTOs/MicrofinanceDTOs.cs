using System.ComponentModel.DataAnnotations;

namespace BankInsight.API.DTOs;

public class MicrofinanceMetricDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Severity { get; set; } = "INFO";
    public string? Subtitle { get; set; }
}

public class FieldStaffDirectoryItemDto
{
    public string StaffId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? BranchId { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class CustomerSearchItemDto
{
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? GhanaCard { get; set; }
    public string? BranchId { get; set; }
}

public class AccountSearchItemDto
{
    public string AccountId { get; set; } = string.Empty;
    public string AccountNumber
    {
        get => AccountId;
        set => AccountId = value;
    }
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Currency { get; set; } = "GHS";
    public decimal Balance { get; set; }
    public decimal LienAmount { get; set; }
    public decimal AvailableBalance
    {
        get => Math.Max(0m, Balance - LienAmount);
        set => Balance = value + LienAmount;
    }
    public string Status { get; set; } = string.Empty;
    public string? ProductCode { get; set; }
    public bool IsCompulsorySavings { get; set; }
}

public class MicrofinanceLoanPolicyDto
{
    public string LoanProductId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string LoanProductCode
    {
        get => Code;
        set => Code = value;
    }
    public string LoanProductName
    {
        get => Name;
        set => Name = value;
    }
    public string RepaymentFrequency { get; set; } = string.Empty;
    public string InterestMethod { get; set; } = string.Empty;
    public bool RequiresCompulsorySavings { get; set; }
    public decimal? MinimumSavingsToLoanRatio { get; set; }
    public bool SupportsWeeklyRepayment { get; set; }
}

public class CollectorAssignmentDto
{
    public string Id { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public string? TargetDepositAccountId
    {
        get => AccountId;
        set => AccountId = value ?? string.Empty;
    }
    public string AccountType { get; set; } = string.Empty;
    public string CollectorStaffId { get; set; } = string.Empty;
    public string StaffId
    {
        get => CollectorStaffId;
        set => CollectorStaffId = value;
    }
    public string CollectorName { get; set; } = "Unassigned";
    public string StaffName
    {
        get => CollectorName;
        set => CollectorName = value;
    }
    public string? LoanProductId { get; set; }
    public string? TargetLoanId
    {
        get => LoanProductId;
        set => LoanProductId = value;
    }
    public string? LoanProductName { get; set; }
    public string CollectionType { get; set; } = string.Empty;
    public string CollectionFrequency
    {
        get => Frequency;
        set => Frequency = value;
    }
    public string Frequency { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public decimal? MinimumContributionAmount { get; set; }
    public string? RouteName { get; set; }
    public string? RouteCode
    {
        get => RouteName;
        set => RouteName = value;
    }
    public string? MeetingDay { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateOnly? NextCollectionDate { get; set; }
    public DateTime? LastCollectionAt { get; set; }
    public string? Notes { get; set; }
    public decimal AvailableSavingsBalance { get; set; }
}

public class FieldCollectionBatchLineDto
{
    public Guid Id { get; set; }
    public string? AssignmentId { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public string? TargetAccountId
    {
        get => AccountId;
        set => AccountId = value ?? string.Empty;
    }
    public string? LoanId { get; set; }
    public string? TargetLoanId
    {
        get => LoanId;
        set => LoanId = value;
    }
    public string TransactionType { get; set; } = string.Empty;
    public string CollectionType
    {
        get => TransactionType;
        set => TransactionType = value;
    }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "GHS";
    public string Status { get; set; } = string.Empty;
    public string Narration { get; set; } = string.Empty;
    public string? PostedTransactionId { get; set; }
    public string? ExternalReference
    {
        get => PostedTransactionId;
        set => PostedTransactionId = value;
    }
    public decimal? DueAmount { get; set; }
    public bool WasMissed { get; set; }
    public DateTime CollectedAt { get; set; }
    public DateTime CollectedAtUtc
    {
        get => CollectedAt;
        set => CollectedAt = value;
    }
    public string? ReceiptNumber
    {
        get => PostedTransactionId;
        set => PostedTransactionId = value;
    }
}

public class FieldCollectionBatchDto
{
    public string Id { get; set; } = string.Empty;
    public string CollectorStaffId { get; set; } = string.Empty;
    public string StaffId
    {
        get => CollectorStaffId;
        set => CollectorStaffId = value;
    }
    public string CollectorName { get; set; } = "Unassigned";
    public string StaffName
    {
        get => CollectorName;
        set => CollectorName = value;
    }
    public string? BranchId { get; set; }
    public DateOnly BatchDate { get; set; }
    public DateOnly BusinessDate
    {
        get => BatchDate;
        set => BatchDate = value;
    }
    public string? RouteName { get; set; }
    public string? RouteCode
    {
        get => RouteName;
        set => RouteName = value;
    }
    public string CollectionType { get; set; } = "MIXED";
    public string Status { get; set; } = string.Empty;
    public decimal ExpectedAmount { get; set; }
    public decimal CollectedAmount { get; set; }
    public decimal SettledAmount { get; set; }
    public decimal VarianceAmount { get; set; }
    public decimal OpeningFloat { get; set; }
    public string Currency { get; set; } = "GHS";
    public string? Notes { get; set; }
    public DateTime OpenedAtUtc { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? SubmittedAtUtc
    {
        get => SubmittedAt;
        set => SubmittedAt = value;
    }
    public DateTime? SettledAt { get; set; }
    public DateTime? SettledAtUtc
    {
        get => SettledAt;
        set => SettledAt = value;
    }
    public string? SettlementReference { get; set; }
    public List<FieldCollectionBatchLineDto> Lines { get; set; } = new();
}

public class CompulsorySavingsAlertDto
{
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string LoanProductId { get; set; } = string.Empty;
    public string LoanProductName { get; set; } = string.Empty;
    public decimal ExampleLoanAmount { get; set; }
    public decimal RequiredSavingsBalance { get; set; }
    public decimal RequiredAmount
    {
        get => RequiredSavingsBalance;
        set => RequiredSavingsBalance = value;
    }
    public decimal AvailableSavingsBalance { get; set; }
    public decimal CurrentAmount
    {
        get => AvailableSavingsBalance;
        set => AvailableSavingsBalance = value;
    }
    public decimal Shortfall { get; set; }
    public decimal ShortfallAmount
    {
        get => Shortfall;
        set => Shortfall = value;
    }
    public string Recommendation { get; set; } = string.Empty;
}

public class MicrofinanceSummaryDto
{
    public DateOnly BusinessDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public List<MicrofinanceMetricDto> Metrics { get; set; } = new();
    public List<CollectorAssignmentDto> Assignments { get; set; } = new();
    public List<CollectorAssignmentDto> ActiveAssignments
    {
        get => Assignments;
        set => Assignments = value;
    }
    public List<CollectorAssignmentDto> DueToday { get; set; } = new();
    public FieldCollectionBatchDto? ActiveBatch { get; set; }
    public List<FieldCollectionBatchDto> OpenBatches
    {
        get => ActiveBatch is null ? new List<FieldCollectionBatchDto>() : new List<FieldCollectionBatchDto> { ActiveBatch };
        set => ActiveBatch = value.FirstOrDefault();
    }
    public List<FieldStaffDirectoryItemDto> StaffDirectory { get; set; } = new();
    public List<FieldStaffDirectoryItemDto> FieldStaff
    {
        get => StaffDirectory;
        set => StaffDirectory = value;
    }
    public List<MicrofinanceLoanPolicyDto> LoanPolicies { get; set; } = new();
    public List<CompulsorySavingsAlertDto> CompulsorySavingsAlerts { get; set; } = new();
}

public class UpsertCollectorAssignmentRequest
{
    [StringLength(50)]
    public string? Id { get; set; }

    [Required]
    [StringLength(50)]
    public string CustomerId { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string AccountId { get; set; } = string.Empty;
    public string? TargetDepositAccountId
    {
        get => AccountId;
        set => AccountId = value ?? string.Empty;
    }

    [StringLength(50)]
    public string? CollectorStaffId { get; set; }
    public string? StaffId
    {
        get => CollectorStaffId;
        set => CollectorStaffId = value;
    }

    [StringLength(50)]
    public string? LoanProductId { get; set; }
    public string? TargetLoanId
    {
        get => LoanProductId;
        set => LoanProductId = value;
    }

    [Required]
    [StringLength(30)]
    public string CollectionType { get; set; } = "SUSU_SAVINGS";
    public string? RequestedCollectionType
    {
        get => CollectionType;
        set => CollectionType = value ?? CollectionType;
    }

    [Required]
    [StringLength(20)]
    public string Frequency { get; set; } = "DAILY";
    public string? CollectionFrequency
    {
        get => Frequency;
        set => Frequency = value ?? Frequency;
    }

    [Range(0, 999999999.99)]
    public decimal TargetAmount { get; set; }

    [Range(0.01, 999999999.99)]
    public decimal? MinimumContributionAmount { get; set; }

    [StringLength(120)]
    public string? RouteName { get; set; }
    public string? RouteCode
    {
        get => RouteName;
        set => RouteName = value;
    }

    [StringLength(20)]
    public string? MeetingDay { get; set; }

    public bool IsPrimaryCollector { get; set; } = true;

    [StringLength(20)]
    public string Status { get; set; } = "ACTIVE";

    public DateOnly? NextCollectionDate { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
}

public class OpenFieldCollectionBatchRequest
{
    [StringLength(50)]
    public string? CollectorStaffId { get; set; }
    public string? StaffId
    {
        get => CollectorStaffId;
        set => CollectorStaffId = value;
    }

    [StringLength(50)]
    public string? BranchId { get; set; }

    [StringLength(120)]
    public string? RouteName { get; set; }
    public string? RouteCode
    {
        get => RouteName;
        set => RouteName = value;
    }

    public DateOnly? BatchDate { get; set; }
    public DateOnly? BusinessDate
    {
        get => BatchDate;
        set => BatchDate = value;
    }

    [Range(0, 999999999.99)]
    public decimal OpeningFloat { get; set; }

    [StringLength(30)]
    public string? CollectionType { get; set; }

    [StringLength(10)]
    public string? Currency { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
}

public class RecordFieldCollectionRequest
{
    [StringLength(50)]
    public string? AssignmentId { get; set; }

    [Required]
    [StringLength(50)]
    public string CustomerId { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string AccountId { get; set; } = string.Empty;
    public string? TargetAccountId
    {
        get => AccountId;
        set => AccountId = value ?? string.Empty;
    }

    [StringLength(50)]
    public string? LoanId { get; set; }
    public string? TargetLoanId
    {
        get => LoanId;
        set => LoanId = value;
    }

    [Required]
    [StringLength(30)]
    public string TransactionType { get; set; } = "SUSU_SAVINGS";
    public string? CollectionType
    {
        get => TransactionType;
        set => TransactionType = value ?? TransactionType;
    }

    [Range(0, 999999999.99)]
    public decimal Amount { get; set; }

    [StringLength(10)]
    public string Currency { get; set; } = "GHS";

    [StringLength(500)]
    public string Narration { get; set; } = string.Empty;

    [StringLength(100)]
    public string? ExternalReference { get; set; }

    [Range(0, 999999999.99)]
    public decimal? DueAmount { get; set; }

    public bool MarkAsMissed { get; set; }
}

public class SubmitFieldCollectionBatchRequest
{
    [StringLength(1000)]
    public string? Notes { get; set; }
}

public class SettleFieldCollectionBatchRequest
{
    [Range(0, 999999999.99)]
    public decimal SettledAmount { get; set; }

    [StringLength(100)]
    public string? SettlementReference { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
}
