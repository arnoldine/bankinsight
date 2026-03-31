using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankInsight.API.Entities;

[Table("bulk_payment_batches")]
public class BulkPaymentBatch
{
    [Key]
    [Column("id")]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Column("batch_reference")]
    [MaxLength(100)]
    public string BatchReference { get; set; } = string.Empty;

    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "PENDING";

    [Column("currency")]
    [MaxLength(10)]
    public string Currency { get; set; } = "GHS";

    [Column("narration")]
    [MaxLength(500)]
    public string? Narration { get; set; }

    [Column("total_amount")]
    public decimal TotalAmount { get; set; }

    [Column("processed_amount")]
    public decimal ProcessedAmount { get; set; }

    [Column("item_count")]
    public int ItemCount { get; set; }

    [Column("processed_count")]
    public int ProcessedCount { get; set; }

    [Column("failed_count")]
    public int FailedCount { get; set; }

    [Column("submitted_by")]
    [MaxLength(50)]
    public string? SubmittedBy { get; set; }

    [Column("processed_at")]
    public DateTime? ProcessedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<BulkPaymentItem> Items { get; set; } = new List<BulkPaymentItem>();
}

[Table("bulk_payment_items")]
public class BulkPaymentItem
{
    [Key]
    [Column("id")]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Column("batch_id")]
    [MaxLength(50)]
    public string BatchId { get; set; } = string.Empty;

    [ForeignKey(nameof(BatchId))]
    public BulkPaymentBatch? Batch { get; set; }

    [Column("account_id")]
    [MaxLength(50)]
    public string AccountId { get; set; } = string.Empty;

    [ForeignKey(nameof(AccountId))]
    public Account? Account { get; set; }

    [Column("transaction_type")]
    [MaxLength(30)]
    public string TransactionType { get; set; } = string.Empty;

    [Column("amount")]
    public decimal Amount { get; set; }

    [Column("narration")]
    [MaxLength(500)]
    public string? Narration { get; set; }

    [Column("teller_id")]
    [MaxLength(50)]
    public string? TellerId { get; set; }

    [Column("client_reference")]
    [MaxLength(100)]
    public string? ClientReference { get; set; }

    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "PENDING";

    [Column("posted_transaction_id")]
    [MaxLength(50)]
    public string? PostedTransactionId { get; set; }

    [Column("error_message")]
    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }

    [Column("processed_at")]
    public DateTime? ProcessedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

[Table("cheque_clearing_items")]
public class ChequeClearingItem
{
    [Key]
    [Column("id")]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Column("account_id")]
    [MaxLength(50)]
    public string AccountId { get; set; } = string.Empty;

    [ForeignKey(nameof(AccountId))]
    public Account? Account { get; set; }

    [Column("transaction_type")]
    [MaxLength(20)]
    public string TransactionType { get; set; } = "DEPOSIT";

    [Column("cheque_number")]
    [MaxLength(50)]
    public string ChequeNumber { get; set; } = string.Empty;

    [Column("drawer_name")]
    [MaxLength(200)]
    public string? DrawerName { get; set; }

    [Column("drawer_account_number")]
    [MaxLength(50)]
    public string? DrawerAccountNumber { get; set; }

    [Column("presenting_bank_code")]
    [MaxLength(20)]
    public string PresentingBankCode { get; set; } = string.Empty;

    [Column("drawee_bank_code")]
    [MaxLength(20)]
    public string DraweeBankCode { get; set; } = string.Empty;

    [Column("clearing_channel")]
    [MaxLength(30)]
    public string ClearingChannel { get; set; } = "GHIPSS";

    [Column("bog_regulatory_class")]
    [MaxLength(30)]
    public string BogRegulatoryClass { get; set; } = "LOCAL";

    [Column("is_other_bank_cheque")]
    public bool IsOtherBankCheque { get; set; }

    [Column("amount")]
    public decimal Amount { get; set; }

    [Column("currency")]
    [MaxLength(10)]
    public string Currency { get; set; } = "GHS";

    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "LODGED";

    [Column("hold_days")]
    public int HoldDays { get; set; }

    [Column("lodged_by")]
    [MaxLength(50)]
    public string? LodgedBy { get; set; }

    [Column("lodged_at")]
    public DateTime LodgedAt { get; set; } = DateTime.UtcNow;

    [Column("clearing_date")]
    public DateOnly ClearingDate { get; set; }

    [Column("cleared_at")]
    public DateTime? ClearedAt { get; set; }

    [Column("posted_transaction_id")]
    [MaxLength(50)]
    public string? PostedTransactionId { get; set; }

    [Column("return_reason")]
    [MaxLength(500)]
    public string? ReturnReason { get; set; }

    [Column("failure_reason")]
    [MaxLength(1000)]
    public string? FailureReason { get; set; }

    [Column("narration")]
    [MaxLength(500)]
    public string? Narration { get; set; }
}

[Table("cheque_book_inventories")]
public class ChequeBookInventory
{
    [Key]
    [Column("id")]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Column("book_reference")]
    [MaxLength(100)]
    public string BookReference { get; set; } = string.Empty;

    [Column("branch_id")]
    [MaxLength(50)]
    public string BranchId { get; set; } = string.Empty;

    [Column("series_prefix")]
    [MaxLength(20)]
    public string SeriesPrefix { get; set; } = string.Empty;

    [Column("start_serial_number")]
    public long StartSerialNumber { get; set; }

    [Column("end_serial_number")]
    public long EndSerialNumber { get; set; }

    [Column("leaf_count")]
    public int LeafCount { get; set; }

    [Column("available_leaf_count")]
    public int AvailableLeafCount { get; set; }

    [Column("used_leaf_count")]
    public int UsedLeafCount { get; set; }

    [Column("cancelled_leaf_count")]
    public int CancelledLeafCount { get; set; }

    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "IN_STOCK";

    [Column("account_id")]
    [MaxLength(50)]
    public string? AccountId { get; set; }

    [ForeignKey(nameof(AccountId))]
    public Account? Account { get; set; }

    [Column("customer_id")]
    [MaxLength(50)]
    public string? CustomerId { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public Customer? Customer { get; set; }

    [Column("issued_at")]
    public DateTime? IssuedAt { get; set; }

    [Column("issued_by")]
    [MaxLength(50)]
    public string? IssuedBy { get; set; }

    [Column("remarks")]
    [MaxLength(500)]
    public string? Remarks { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ChequeBookLeaf> Leaves { get; set; } = new List<ChequeBookLeaf>();
}

[Table("cheque_book_leaves")]
public class ChequeBookLeaf
{
    [Key]
    [Column("id")]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Column("book_id")]
    [MaxLength(50)]
    public string BookId { get; set; } = string.Empty;

    [ForeignKey(nameof(BookId))]
    public ChequeBookInventory? Book { get; set; }

    [Column("serial_number")]
    public long SerialNumber { get; set; }

    [Column("cheque_number")]
    [MaxLength(50)]
    public string ChequeNumber { get; set; } = string.Empty;

    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "AVAILABLE";

    [Column("account_id")]
    [MaxLength(50)]
    public string? AccountId { get; set; }

    [ForeignKey(nameof(AccountId))]
    public Account? Account { get; set; }

    [Column("used_transaction_id")]
    [MaxLength(50)]
    public string? UsedTransactionId { get; set; }

    [Column("used_at")]
    public DateTime? UsedAt { get; set; }

    [Column("cancel_reason")]
    [MaxLength(500)]
    public string? CancelReason { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
