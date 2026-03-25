using System.ComponentModel.DataAnnotations;

namespace BankInsight.API.DTOs;

// Branch Hierarchy DTOs
public class BranchHierarchyDto
{
    public int Id { get; set; }
    public string BranchId { get; set; } = string.Empty;
    public string BranchCode { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string? ParentBranchId { get; set; }
    public string? ParentBranchCode { get; set; }
    public string? ParentBranchName { get; set; }
    public int Level { get; set; }
    public string? Path { get; set; }
    public List<BranchHierarchyDto> Children { get; set; } = new();
}

public class CreateBranchHierarchyRequest
{
    [Required(ErrorMessage = "BranchId is required")]
    [StringLength(50, ErrorMessage = "BranchId must not exceed 50 characters")]
    public string BranchId { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "ParentBranchId must not exceed 50 characters")]
    public string? ParentBranchId { get; set; }
}

// Branch Vault DTOs
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

public class VaultCountRequest
{
    [Required(ErrorMessage = "BranchId is required")]
    [StringLength(50, ErrorMessage = "BranchId must not exceed 50 characters")]
    public string BranchId { get; set; } = string.Empty;

    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency must be a 3-letter code")]
    public string Currency { get; set; } = "GHS";

    [Range(0.01, 999999999.99, ErrorMessage = "Amount must be between 0.01 and 999999999.99")]
    public decimal Amount { get; set; }

    [StringLength(100, ErrorMessage = "ControlReference must not exceed 100 characters")]
    public string? ControlReference { get; set; }

    [StringLength(100, ErrorMessage = "CountReason must not exceed 100 characters")]
    public string? CountReason { get; set; }

    [StringLength(100, ErrorMessage = "WitnessOfficer must not exceed 100 characters")]
    public string? WitnessOfficer { get; set; }

    [StringLength(100, ErrorMessage = "SealNumber must not exceed 100 characters")]
    public string? SealNumber { get; set; }

    public List<CashDenominationLineDto> Denominations { get; set; } = new();
}

public class VaultTransactionRequest
{
    [Required(ErrorMessage = "BranchId is required")]
    [StringLength(50, ErrorMessage = "BranchId must not exceed 50 characters")]
    public string BranchId { get; set; } = string.Empty;

    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency must be a 3-letter code")]
    public string Currency { get; set; } = "GHS";

    [Range(0.01, 999999999.99, ErrorMessage = "Amount must be between 0.01 and 999999999.99")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Type is required")]
    [StringLength(50, ErrorMessage = "Type must not exceed 50 characters")]
    public string Type { get; set; } = "Deposit";

    [StringLength(100, ErrorMessage = "Reference must not exceed 100 characters")]
    public string? Reference { get; set; }

    [StringLength(500, ErrorMessage = "Narration must not exceed 500 characters")]
    public string? Narration { get; set; }

    [StringLength(100, ErrorMessage = "ControlReference must not exceed 100 characters")]
    public string? ControlReference { get; set; }

    [StringLength(100, ErrorMessage = "WitnessOfficer must not exceed 100 characters")]
    public string? WitnessOfficer { get; set; }

    [StringLength(100, ErrorMessage = "SealNumber must not exceed 100 characters")]
    public string? SealNumber { get; set; }

    public List<CashDenominationLineDto> Denominations { get; set; } = new();
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

public class OpenTillRequest
{
    [Required(ErrorMessage = "TellerId is required")]
    [StringLength(50, ErrorMessage = "TellerId must not exceed 50 characters")]
    public string TellerId { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "BranchId must not exceed 50 characters")]
    public string? BranchId { get; set; }

    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency must be a 3-letter code")]
    public string Currency { get; set; } = "GHS";

    [Range(0, 999999999.99, ErrorMessage = "OpeningBalance must be between 0 and 999999999.99")]
    public decimal OpeningBalance { get; set; }

    [Range(0, 999999999.99, ErrorMessage = "MidDayCashLimit must be between 0 and 999999999.99")]
    public decimal? MidDayCashLimit { get; set; }

    [StringLength(500, ErrorMessage = "Notes must not exceed 500 characters")]
    public string? Notes { get; set; }

    [StringLength(100, ErrorMessage = "WitnessOfficer must not exceed 100 characters")]
    public string? WitnessOfficer { get; set; }
}

public class TillCashTransferRequest
{
    [Required(ErrorMessage = "TellerId is required")]
    [StringLength(50, ErrorMessage = "TellerId must not exceed 50 characters")]
    public string TellerId { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "BranchId must not exceed 50 characters")]
    public string? BranchId { get; set; }

    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency must be a 3-letter code")]
    public string Currency { get; set; } = "GHS";

    [Range(0.01, 999999999.99, ErrorMessage = "Amount must be between 0.01 and 999999999.99")]
    public decimal Amount { get; set; }

    [StringLength(100, ErrorMessage = "Reference must not exceed 100 characters")]
    public string? Reference { get; set; }

    [StringLength(500, ErrorMessage = "Narration must not exceed 500 characters")]
    public string? Narration { get; set; }

    [StringLength(100, ErrorMessage = "ControlReference must not exceed 100 characters")]
    public string? ControlReference { get; set; }

    [StringLength(100, ErrorMessage = "WitnessOfficer must not exceed 100 characters")]
    public string? WitnessOfficer { get; set; }

    [StringLength(100, ErrorMessage = "SealNumber must not exceed 100 characters")]
    public string? SealNumber { get; set; }

    public List<CashDenominationLineDto> Denominations { get; set; } = new();
}

public class CloseTillRequest
{
    [Required(ErrorMessage = "TellerId is required")]
    [StringLength(50, ErrorMessage = "TellerId must not exceed 50 characters")]
    public string TellerId { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "BranchId must not exceed 50 characters")]
    public string? BranchId { get; set; }

    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency must be a 3-letter code")]
    public string Currency { get; set; } = "GHS";

    [Range(0, 999999999.99, ErrorMessage = "PhysicalCashCount must be between 0 and 999999999.99")]
    public decimal PhysicalCashCount { get; set; }

    [StringLength(500, ErrorMessage = "Notes must not exceed 500 characters")]
    public string? Notes { get; set; }

    [StringLength(100, ErrorMessage = "ControlReference must not exceed 100 characters")]
    public string? ControlReference { get; set; }

    [StringLength(100, ErrorMessage = "WitnessOfficer must not exceed 100 characters")]
    public string? WitnessOfficer { get; set; }

    [StringLength(100, ErrorMessage = "SealNumber must not exceed 100 characters")]
    public string? SealNumber { get; set; }

    public List<CashDenominationLineDto> Denominations { get; set; } = new();
}

// Branch Limit DTOs
public class BranchLimitDto
{
    public int Id { get; set; }
    public string BranchId { get; set; } = string.Empty;
    public string BranchCode { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string LimitType { get; set; } = string.Empty;
    public string? TransactionType { get; set; }
    public string Currency { get; set; } = "GHS";
    public decimal? SingleTransactionLimit { get; set; }
    public decimal? DailyLimit { get; set; }
    public decimal? MonthlyLimit { get; set; }
    public bool RequiresApproval { get; set; }
    public decimal? ApprovalThreshold { get; set; }
}

public class CreateBranchLimitRequest
{
    [Required(ErrorMessage = "BranchId is required")]
    [StringLength(50, ErrorMessage = "BranchId must not exceed 50 characters")]
    public string BranchId { get; set; } = string.Empty;

    [Required(ErrorMessage = "LimitType is required")]
    [StringLength(50, ErrorMessage = "LimitType must not exceed 50 characters")]
    public string LimitType { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "TransactionType must not exceed 50 characters")]
    public string? TransactionType { get; set; }

    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency must be a 3-letter code")]
    public string Currency { get; set; } = "GHS";

    [Range(0.01, 999999999.99, ErrorMessage = "SingleTransactionLimit must be between 0.01 and 999999999.99")]
    public decimal? SingleTransactionLimit { get; set; }

    [Range(0.01, 999999999.99, ErrorMessage = "DailyLimit must be between 0.01 and 999999999.99")]
    public decimal? DailyLimit { get; set; }

    [Range(0.01, 999999999.99, ErrorMessage = "MonthlyLimit must be between 0.01 and 999999999.99")]
    public decimal? MonthlyLimit { get; set; }

    public bool RequiresApproval { get; set; }

    [Range(0.01, 999999999.99, ErrorMessage = "ApprovalThreshold must be between 0.01 and 999999999.99")]
    public decimal? ApprovalThreshold { get; set; }
}

// Inter-Branch Transfer DTOs
public class InterBranchTransferDto
{
    public string Id { get; set; } = string.Empty;
    public string FromBranchId { get; set; } = string.Empty;
    public string FromBranchCode { get; set; } = string.Empty;
    public string FromBranchName { get; set; } = string.Empty;
    public string ToBranchId { get; set; } = string.Empty;
    public string ToBranchCode { get; set; } = string.Empty;
    public string ToBranchName { get; set; } = string.Empty;
    public string Currency { get; set; } = "GHS";
    public decimal Amount { get; set; }
    public string? Reference { get; set; }
    public string? Narration { get; set; }
    public string Status { get; set; } = "Pending";
    public string TransitStage { get; set; } = "AWAITING_APPROVAL";
    public string InitiatedBy { get; set; } = string.Empty;
    public string InitiatedByName { get; set; } = string.Empty;
    public string? ApprovedBy { get; set; }
    public string? ApprovedByName { get; set; }
    public string? SentBy { get; set; }
    public string? SentByName { get; set; }
    public string? ReceivedBy { get; set; }
    public string? ReceivedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? DispatchedAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? RejectionReason { get; set; }
}

public class CreateInterBranchTransferRequest
{
    [Required(ErrorMessage = "FromBranchId is required")]
    [StringLength(50, ErrorMessage = "FromBranchId must not exceed 50 characters")]
    public string FromBranchId { get; set; } = string.Empty;

    [Required(ErrorMessage = "ToBranchId is required")]
    [StringLength(50, ErrorMessage = "ToBranchId must not exceed 50 characters")]
    public string ToBranchId { get; set; } = string.Empty;

    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency must be a 3-letter code")]
    public string Currency { get; set; } = "GHS";

    [Range(0.01, 999999999.99, ErrorMessage = "Amount must be between 0.01 and 999999999.99")]
    public decimal Amount { get; set; }

    [StringLength(100, ErrorMessage = "Reference must not exceed 100 characters")]
    public string? Reference { get; set; }

    [StringLength(500, ErrorMessage = "Narration must not exceed 500 characters")]
    public string? Narration { get; set; }
}

public class ApproveInterBranchTransferRequest
{
    [Required(ErrorMessage = "TransferId is required")]
    [StringLength(50, ErrorMessage = "TransferId must not exceed 50 characters")]
    public string TransferId { get; set; } = string.Empty;

    public bool Approved { get; set; }

    [StringLength(500, ErrorMessage = "RejectionReason must not exceed 500 characters")]
    public string? RejectionReason { get; set; }
}

public class DispatchInterBranchTransferRequest
{
    [Required(ErrorMessage = "TransferId is required")]
    [StringLength(50, ErrorMessage = "TransferId must not exceed 50 characters")]
    public string TransferId { get; set; } = string.Empty;
}

public class ReceiveInterBranchTransferRequest
{
    [Required(ErrorMessage = "TransferId is required")]
    [StringLength(50, ErrorMessage = "TransferId must not exceed 50 characters")]
    public string TransferId { get; set; } = string.Empty;
}
// Branch Config DTOs
public class BranchConfigDto
{
    public int Id { get; set; }
    public string BranchId { get; set; } = string.Empty;
    public string BranchCode { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string ConfigKey { get; set; } = string.Empty;
    public string? ConfigValue { get; set; }
    public string DataType { get; set; } = "string";
    public string? Description { get; set; }
}

public class UpdateBranchConfigRequest
{
    [Required(ErrorMessage = "BranchId is required")]
    [StringLength(50, ErrorMessage = "BranchId must not exceed 50 characters")]
    public string BranchId { get; set; } = string.Empty;

    [Required(ErrorMessage = "ConfigKey is required")]
    [StringLength(100, ErrorMessage = "ConfigKey must not exceed 100 characters")]
    public string ConfigKey { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "ConfigValue must not exceed 1000 characters")]
    public string? ConfigValue { get; set; }

    [StringLength(50, ErrorMessage = "DataType must not exceed 50 characters")]
    public string DataType { get; set; } = "string";

    [StringLength(500, ErrorMessage = "Description must not exceed 500 characters")]
    public string? Description { get; set; }
}
public class CashControlAlertDto
{
    public string AlertType { get; set; } = string.Empty;
    public string Severity { get; set; } = "INFO";
    public string ScopeType { get; set; } = string.Empty;
    public string ScopeId { get; set; } = string.Empty;
    public string ScopeName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public decimal? ThresholdAmount { get; set; }
    public decimal? ActualAmount { get; set; }
    public DateTime ObservedAt { get; set; } = DateTime.UtcNow;
}

public class VaultCashPositionDto
{
    public string BranchId { get; set; } = string.Empty;
    public string BranchCode { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string Currency { get; set; } = "GHS";
    public decimal VaultCash { get; set; }
    public decimal MinBalance { get; set; }
    public decimal MaxHoldingLimit { get; set; }
    public decimal TillCash { get; set; }
    public decimal TotalOperationalCash { get; set; }
    public decimal GlCashBalance { get; set; }
    public decimal GlVariance { get; set; }
    public DateTime? LastCountDate { get; set; }
    public string Status { get; set; } = "NORMAL";
}

public class BranchCashSummaryDto
{
    public string BranchId { get; set; } = string.Empty;
    public string BranchCode { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string Currency { get; set; } = "GHS";
    public decimal VaultCash { get; set; }
    public decimal TellerTillCash { get; set; }
    public decimal TotalOperationalCash { get; set; }
    public decimal PendingCashInTransitOut { get; set; }
    public decimal PendingCashInTransitIn { get; set; }
    public int OpenTillCount { get; set; }
    public int VarianceTillCount { get; set; }
    public int StaleTransferCount { get; set; }
    public decimal GlCashBalance { get; set; }
    public decimal ReconciliationVariance { get; set; }
    public string ReconciliationStatus { get; set; } = "BALANCED";
}

public class CashReconciliationSummaryDto
{
    public string Currency { get; set; } = "GHS";
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public decimal TotalVaultCash { get; set; }
    public decimal TotalTillCash { get; set; }
    public decimal TotalOperationalCash { get; set; }
    public decimal TotalGlCashBalance { get; set; }
    public decimal TotalVariance { get; set; }
    public int BranchesOutOfBalance { get; set; }
    public int TillsOverLimit { get; set; }
    public int TillsWithVariance { get; set; }
    public int StaleCashInTransitItems { get; set; }
    public List<BranchCashSummaryDto> Branches { get; set; } = new();
    public List<CashControlAlertDto> Alerts { get; set; } = new();
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

public class CashTransitItemDto
{
    public string TransferId { get; set; } = string.Empty;
    public string FromBranchId { get; set; } = string.Empty;
    public string FromBranchName { get; set; } = string.Empty;
    public string ToBranchId { get; set; } = string.Empty;
    public string ToBranchName { get; set; } = string.Empty;
    public string Currency { get; set; } = "GHS";
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string TransitStage { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public string? Narration { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public double HoursOpen { get; set; }
    public bool IsStale { get; set; }
}

