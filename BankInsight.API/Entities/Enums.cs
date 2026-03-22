namespace BankInsight.API.Entities;

public enum CustomerType
{
    INDIVIDUAL,
    CORPORATE
}

public enum RecordStatus
{
    ACTIVE,
    CLOSED,
    DISSOLVED,
    PENDING,
    RETIRED,
    DORMANT,
    FROZEN,
    WRITTEN_OFF
}

public enum TransactionType
{
    DEPOSIT,
    WITHDRAWAL,
    TRANSFER,
    LOAN_REPAYMENT
}

public enum TransactionStatus
{
    POSTED,
    PENDING,
    REJECTED
}

public enum ApprovalStatus
{
    PENDING,
    APPROVED,
    REJECTED
}
