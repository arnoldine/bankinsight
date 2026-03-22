namespace BankInsight.API.Entities;

public static class EventTypes
{
    // Deposits
    public const string DepositPosted = "DepositPosted";
    public const string WithdrawalPosted = "WithdrawalPosted";
    public const string TransferCompleted = "TransferCompleted";

    // Loans
    public const string LoanDisbursed = "LoanDisbursed";
    public const string LoanRepaymentReceived = "LoanRepaymentReceived";
    public const string InterestAccrued = "InterestAccrued";
    
    // Fees & Penalties
    public const string PenaltyApplied = "PenaltyApplied";
    public const string ChargeApplied = "ChargeApplied";
    public const string WriteOffExecuted = "WriteOffExecuted";
    
    // Lifecycle
    public const string InterestPosted = "InterestPosted";
}
