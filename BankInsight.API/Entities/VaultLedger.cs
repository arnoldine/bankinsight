using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankInsight.API.Entities;

public enum VaultTransactionType
{
    MainVault_To_BranchVault_Allocation,
    BranchVault_To_MainVault_Return,
    BranchVault_To_Teller_Allocation,
    Teller_To_BranchVault_Return,
    Teller_Cash_Receipt,
    Teller_Cash_Dispense
}

public enum TellerSessionState
{
    Closed,
    Open,
    Reconciling,
    Balanced,
    Suspended_OverLimit
}

public enum DenominationUnit
{
    GHS_200, GHS_100, GHS_50, GHS_20, GHS_10, GHS_5, GHS_2, GHS_1,
    GHS_0_50, GHS_0_20, GHS_0_10, GHS_0_05, GHS_0_01
}

[Table("vault_ledger")]
public class VaultLedger
{
    [Key]
    [Column("ledger_id")]
    public Guid LedgerId { get; set; } = Guid.NewGuid();

    [Column("transaction_date")]
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

    [Column("transaction_type")]
    public VaultTransactionType TransactionType { get; set; }

    [Column("debit_account_id")]
    public Guid DebitAccountId { get; set; }

    [Column("credit_account_id")]
    public Guid CreditAccountId { get; set; }

    [Column("amount")]
    public decimal Amount { get; set; }

    [Column("currency")]
    public string Currency { get; set; } = "GHS";

    [Column("narration")]
    public string? Narration { get; set; }

    [Column("reference_number")]
    public string ReferenceNumber { get; set; } = string.Empty;

    [Column("maker_user_id")]
    public Guid MakerUserId { get; set; }

    [Column("checker_user_id")]
    public Guid? CheckerUserId { get; set; }

    [Column("is_approved")]
    public bool IsApproved { get; set; }

    [Column("approved_at")]
    public DateTime? ApprovedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<VaultTransactionDenomination> Denominations { get; set; } = new List<VaultTransactionDenomination>();
}

[Table("vault_transaction_denomination")]
public class VaultTransactionDenomination
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("ledger_id")]
    public Guid LedgerId { get; set; }

    [ForeignKey(nameof(LedgerId))]
    public VaultLedger Ledger { get; set; } = null!;

    [Column("denomination")]
    public DenominationUnit Denomination { get; set; }

    [Column("pieces")]
    public int Pieces { get; set; }

    [Column("total_value")]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public decimal TotalValue { get; set; }
}

[Table("teller_session")]
public class TellerSession
{
    [Key]
    [Column("session_id")]
    public Guid SessionId { get; set; } = Guid.NewGuid();

    [Column("teller_id")]
    public Guid TellerId { get; set; }

    [Column("branch_id")]
    public Guid BranchId { get; set; }

    [Column("opened_at")]
    public DateTime OpenedAt { get; set; } = DateTime.UtcNow;

    [Column("closed_at")]
    public DateTime? ClosedAt { get; set; }

    [Column("state")]
    public TellerSessionState State { get; set; } = TellerSessionState.Open;

    [Column("mid_day_cash_limit")]
    public decimal MidDayCashLimit { get; set; } = 50000m;

    [Column("opening_balance")]
    public decimal OpeningBalance { get; set; }

    [Column("closing_balance")]
    public decimal? ClosingBalance { get; set; }

    [Column("row_version")]
    public long RowVersion { get; set; } = 1;
}
