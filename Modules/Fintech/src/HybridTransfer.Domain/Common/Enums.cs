namespace HybridTransfer.Domain.Common;

public enum CustomerType { Individual, Business }
public enum KycTier { Tier0, Tier1, Tier2, Tier3 }
public enum KycStatus { Draft, PendingReview, Approved, Rejected, EnhancedDueDiligence, Suspended }
public enum RiskRating { Low, Medium, High, Severe }
public enum CustomerStatus { Pending, Active, Restricted, Suspended, Closed }
public enum WalletType { Fiat, CryptoCustody, Fee, Treasury, Suspense }
public enum WalletStatus { Active, Restricted, Frozen, Closed }
public enum LedgerAccountCategory { Asset, Liability, Income, Expense, Suspense, Equity }
public enum JournalEntryStatus { Pending, Posted, Failed, Reversed, Disputed }
public enum TransferType { Internal, MobileMoneyPayout, BankPayout, CryptoWithdrawal, Conversion }
public enum TransferChannel { Internal, MobileMoney, Bank, Crypto }
public enum TransferStatus { Draft, AwaitingApproval, PendingRiskReview, Authorized, Submitted, PendingSettlement, Posted, Failed, Reversed, Disputed }
public enum RiskStatus { Clear, Monitor, Hold, Blocked }
public enum ComplianceStatus { Clear, PendingReview, OnHold, Rejected }
public enum CryptoTransactionStatus { Detected, AwaitingConfirmations, ScreeningHold, Credited, Failed, Reversed }
public enum ApprovalStatus { NotRequired, Pending, Approved, Rejected }
public enum BeneficiaryType { Internal, Bank, MobileMoney, Crypto }
public enum VerificationStatus { Unverified, Pending, Verified, Rejected }
public enum AlertStatus { Open, Investigating, Closed, Escalated }
public enum AlertSeverity { Low, Medium, High, Critical }
