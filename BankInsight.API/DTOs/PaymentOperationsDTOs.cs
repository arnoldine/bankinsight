using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BankInsight.API.DTOs;

public class CreateBulkPaymentBatchRequest
{
    [StringLength(10)]
    public string Currency { get; set; } = "GHS";

    [StringLength(500)]
    public string? Narration { get; set; }

    [StringLength(50)]
    public string? SubmittedBy { get; set; }

    [MinLength(1)]
    public List<BulkPaymentItemRequest> Items { get; set; } = new();
}

public class BulkPaymentItemRequest
{
    [Required]
    [StringLength(50)]
    public string AccountId { get; set; } = string.Empty;

    [Required]
    [StringLength(30)]
    public string TransactionType { get; set; } = string.Empty;

    [Range(0.01, 999999999.99)]
    public decimal Amount { get; set; }

    [StringLength(500)]
    public string? Narration { get; set; }

    [StringLength(50)]
    public string? TellerId { get; set; }

    [StringLength(100)]
    public string? ClientReference { get; set; }
}

public class BulkPaymentBatchDto
{
    public string Id { get; set; } = string.Empty;
    public string BatchReference { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Currency { get; set; } = "GHS";
    public string? Narration { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal ProcessedAmount { get; set; }
    public int ItemCount { get; set; }
    public int ProcessedCount { get; set; }
    public int FailedCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public List<BulkPaymentItemDto> Items { get; set; } = new();
}

public class BulkPaymentItemDto
{
    public string Id { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public string TransactionType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Narration { get; set; }
    public string? TellerId { get; set; }
    public string? ClientReference { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? PostedTransactionId { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? ProcessedAt { get; set; }
}

public class LodgeChequeDepositRequest
{
    [Required]
    [StringLength(50)]
    public string AccountId { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string ChequeNumber { get; set; } = string.Empty;

    [Range(0.01, 999999999.99)]
    public decimal Amount { get; set; }

    [StringLength(10)]
    public string Currency { get; set; } = "GHS";

    [StringLength(200)]
    public string? DrawerName { get; set; }

    [StringLength(50)]
    public string? DrawerAccountNumber { get; set; }

    [Required]
    [StringLength(20)]
    public string PresentingBankCode { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string DraweeBankCode { get; set; } = string.Empty;

    public bool IsOtherBankCheque { get; set; } = true;

    [StringLength(30)]
    public string ClearingChannel { get; set; } = "GHIPSS";

    [StringLength(30)]
    public string BogRegulatoryClass { get; set; } = "LOCAL";

    [StringLength(50)]
    public string? TellerId { get; set; }

    [StringLength(500)]
    public string? Narration { get; set; }
}

public class ChequeWithdrawalRequest
{
    [Required]
    [StringLength(50)]
    public string AccountId { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string ChequeNumber { get; set; } = string.Empty;

    [Range(0.01, 999999999.99)]
    public decimal Amount { get; set; }

    [StringLength(10)]
    public string Currency { get; set; } = "GHS";

    [Required]
    [StringLength(50)]
    public string TellerId { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Narration { get; set; }
}

public class ReturnChequeRequest
{
    [Required]
    [StringLength(500)]
    public string Reason { get; set; } = string.Empty;
}

public class ChequeClearingItemDto
{
    public string Id { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public string TransactionType { get; set; } = string.Empty;
    public string ChequeNumber { get; set; } = string.Empty;
    public string? DrawerName { get; set; }
    public string? DrawerAccountNumber { get; set; }
    public string PresentingBankCode { get; set; } = string.Empty;
    public string DraweeBankCode { get; set; } = string.Empty;
    public string ClearingChannel { get; set; } = string.Empty;
    public string BogRegulatoryClass { get; set; } = string.Empty;
    public bool IsOtherBankCheque { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "GHS";
    public string Status { get; set; } = string.Empty;
    public int HoldDays { get; set; }
    public DateTime LodgedAt { get; set; }
    public DateOnly ClearingDate { get; set; }
    public DateTime? ClearedAt { get; set; }
    public string? PostedTransactionId { get; set; }
    public string? ReturnReason { get; set; }
    public string? FailureReason { get; set; }
    public string? Narration { get; set; }
}

public class ChequeClearingBatchResultDto
{
    public DateOnly BusinessDate { get; set; }
    public int ItemsEvaluated { get; set; }
    public int ItemsCleared { get; set; }
    public int ItemsReturned { get; set; }
    public int ItemsPending { get; set; }
    public decimal TotalClearedAmount { get; set; }
    public List<string> ClearedItemIds { get; set; } = new();
    public List<string> PendingItemIds { get; set; } = new();
}

public class CreateChequeBookStockRequest
{
    [Required]
    [StringLength(50)]
    public string BranchId { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string SeriesPrefix { get; set; } = string.Empty;

    [Range(1, long.MaxValue)]
    public long StartSerialNumber { get; set; }

    [Range(1, 500)]
    public int LeafCount { get; set; }

    [StringLength(500)]
    public string? Remarks { get; set; }
}

public class IssueChequeBookRequest
{
    [Required]
    [StringLength(50)]
    public string AccountId { get; set; } = string.Empty;

    [StringLength(50)]
    public string? IssuedBy { get; set; }

    [StringLength(500)]
    public string? Remarks { get; set; }
}

public class CancelChequeLeafRequest
{
    [Required]
    [StringLength(500)]
    public string Reason { get; set; } = string.Empty;
}

public class ChequeBookLeafDto
{
    public string Id { get; set; } = string.Empty;
    public long SerialNumber { get; set; }
    public string ChequeNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? AccountId { get; set; }
    public string? UsedTransactionId { get; set; }
    public DateTime? UsedAt { get; set; }
    public string? CancelReason { get; set; }
}

public class ChequeBookInventoryDto
{
    public string Id { get; set; } = string.Empty;
    public string BookReference { get; set; } = string.Empty;
    public string BranchId { get; set; } = string.Empty;
    public string SeriesPrefix { get; set; } = string.Empty;
    public long StartSerialNumber { get; set; }
    public long EndSerialNumber { get; set; }
    public int LeafCount { get; set; }
    public int AvailableLeafCount { get; set; }
    public int UsedLeafCount { get; set; }
    public int CancelledLeafCount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? AccountId { get; set; }
    public string? CustomerId { get; set; }
    public DateTime? IssuedAt { get; set; }
    public string? IssuedBy { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ChequeBookLeafDto> Leaves { get; set; } = new();
}
