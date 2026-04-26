using System.ComponentModel.DataAnnotations;

namespace BankInsight.API.DTOs;

public class ClientChannelBootstrapResponse
{
    public ClientIdentityDto Identity { get; set; } = new();
    public ClientLinkedCustomerDto? LinkedCustomer { get; set; }
    public List<string> Warnings { get; set; } = new();
}

public class ClientIdentityDto
{
    public string UserId { get; set; } = string.Empty;
    public string? CustomerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string[] Permissions { get; set; } = [];
    public bool HasTransactionPin { get; set; }
}

public class ClientLinkedCustomerDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? KycLevel { get; set; }
    public string? RiskRating { get; set; }
}

public class ClientAccountDto
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Currency { get; set; } = "GHS";
    public decimal Balance { get; set; }
    public decimal LienAmount { get; set; }
    public string Status { get; set; } = "ACTIVE";
    public string? ProductCode { get; set; }
    public string? LastTransDate { get; set; }
}

public class ClientSessionDto
{
    public string Id { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
    public string ExpiresAt { get; set; } = string.Empty;
    public string LastActivity { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class ClientComplaintListItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string OwnerTeam { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
    public string SlaDueAt { get; set; } = string.Empty;
}

public class ClientComplaintDetailDto : ClientComplaintListItemDto
{
    public string Details { get; set; } = string.Empty;
    public string? ClosedAt { get; set; }
    public List<ClientComplaintEventDto> Events { get; set; } = new();
    public List<ClientComplaintAttachmentDto> Attachments { get; set; } = new();
}

public class ClientComplaintEventDto
{
    public string Id { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Visibility { get; set; } = string.Empty;
    public string? ActorName { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
}

public class CreateClientComplaintRequest
{
    [Required(ErrorMessage = "Complaint category is required")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Complaint category must be between 3 and 100 characters")]
    public string Category { get; set; } = string.Empty;

    [Required(ErrorMessage = "Complaint summary is required")]
    [StringLength(255, MinimumLength = 5, ErrorMessage = "Complaint summary must be between 5 and 255 characters")]
    public string Summary { get; set; } = string.Empty;

    [Required(ErrorMessage = "Complaint details are required")]
    [StringLength(4000, MinimumLength = 10, ErrorMessage = "Complaint details must be between 10 and 4000 characters")]
    public string Details { get; set; } = string.Empty;
}

public class ClientComplaintAttachmentDto
{
    public string Id { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string? ContentUrl { get; set; }
    public string Status { get; set; } = string.Empty;
    public string UploadedAt { get; set; } = string.Empty;
}

public class UploadClientComplaintAttachmentRequest
{
    [Required]
    [StringLength(255, MinimumLength = 2)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string ContentType { get; set; } = string.Empty;

    [Required]
    public string DataUrl { get; set; } = string.Empty;
}

public class UpdateClientProfileRequest
{
    [StringLength(255, MinimumLength = 2)]
    public string? Name { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    [Phone]
    public string? Phone { get; set; }

    [StringLength(50)]
    public string? DigitalAddress { get; set; }

    [Required]
    public string StepUpToken { get; set; } = string.Empty;
}

public class UploadClientProfileMediaRequest
{
    [Required]
    [StringLength(30)]
    public string MediaType { get; set; } = string.Empty;

    [StringLength(10)]
    public string? MediaSide { get; set; }

    [Required]
    [StringLength(255, MinimumLength = 2)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string ContentType { get; set; } = string.Empty;

    [Required]
    public string DataUrl { get; set; } = string.Empty;

    [Required]
    public string StepUpToken { get; set; } = string.Empty;
}

public class ClientKycChecklistItemDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool IsSatisfied { get; set; }
    public string Detail { get; set; } = string.Empty;
}

public class ClientKycReadinessDto
{
    public bool IsReadyForAccountOpening { get; set; }
    public bool IsReadyForLoanOrigination { get; set; }
    public List<string> MissingRequirements { get; set; } = new();
    public List<ClientKycChecklistItemDto> Checklist { get; set; } = new();
}

public class ClientKycCaseEventDto
{
    public string Id { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ActorName { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
}

public class ClientKycCaseDto
{
    public string Id { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string SubmittedAt { get; set; } = string.Empty;
    public string? ReviewedAt { get; set; }
    public string? ReviewerName { get; set; }
    public string? DecisionNote { get; set; }
    public List<ClientKycCaseEventDto> Events { get; set; } = new();
}

public class ClientKycOverviewDto
{
    public string CustomerId { get; set; } = string.Empty;
    public string KycLevel { get; set; } = string.Empty;
    public ClientKycReadinessDto Readiness { get; set; } = new();
    public List<ClientKycCaseDto> Cases { get; set; } = new();
}

public class SubmitClientKycRefreshRequest
{
    [Required]
    [StringLength(255, MinimumLength = 5)]
    public string Reason { get; set; } = string.Empty;

    [Required]
    [StringLength(1000, MinimumLength = 10)]
    public string Summary { get; set; } = string.Empty;

    [Required]
    public string StepUpToken { get; set; } = string.Empty;
}

public class ClientStatementSummaryDto
{
    public string StatementId { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public string PeriodLabel { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public int EntryCount { get; set; }
    public decimal TotalDebits { get; set; }
    public decimal TotalCredits { get; set; }
    public string GeneratedAt { get; set; } = string.Empty;
}

public class ClientStatementDetailDto
{
    public string StatementId { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public string PeriodLabel { get; set; } = string.Empty;
    public string Currency { get; set; } = "GHS";
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }
    public decimal TotalDebits { get; set; }
    public decimal TotalCredits { get; set; }
    public List<ClientStatementEntryDto> Entries { get; set; } = new();
}

public class ClientStatementExportDto
{
    public string StatementId { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public string PeriodLabel { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "text/csv";
    public string ExportedAt { get; set; } = string.Empty;
    public string ChecksumSha256 { get; set; } = string.Empty;
    public int LineCount { get; set; }
    public string ContentBase64 { get; set; } = string.Empty;
}

public class ClientStatementEntryDto
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Narration { get; set; }
    public string? Reference { get; set; }
    public string Date { get; set; } = string.Empty;
}

public class ClientBankingOverviewDto
{
    public decimal TotalVisibleBalance { get; set; }
    public int ActiveAccountCount { get; set; }
    public int ActiveStandingOrderCount { get; set; }
    public int ActiveLoanCount { get; set; }
    public int ActiveInvestmentCount { get; set; }
    public decimal TotalLoanExposure { get; set; }
    public decimal TotalInvestmentBalance { get; set; }
}

public class ClientMerchantDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string SettlementType { get; set; } = string.Empty;
    public string Currency { get; set; } = "GHS";
    public string? DestinationAccountId { get; set; }
    public string MerchantKind { get; set; } = "CATALOG";
    public string? MerchantProfileId { get; set; }
    public string? SettlementCustomerId { get; set; }
    public bool AcceptsQrPayments { get; set; }
    public string? QrScheme { get; set; }
}

public class ClientMerchantAcceptanceEligibilityDto
{
    public bool CanEnroll { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerType { get; set; } = string.Empty;
    public string BusinessName { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public List<ClientAccountDto> EligibleSettlementAccounts { get; set; } = new();
}

public class ClientMerchantProfileDto
{
    public string Id { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string MerchantCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string SettlementAccountId { get; set; } = string.Empty;
    public string SettlementAccountLabel { get; set; } = string.Empty;
    public string Currency { get; set; } = "GHS";
    public string Status { get; set; } = string.Empty;
    public string QrScheme { get; set; } = "BANKINSIGHT_QR";
    public string QrPayload { get; set; } = string.Empty;
    public bool AcceptsAppPayments { get; set; }
    public bool GhQrReady { get; set; }
    public string FutureScheme { get; set; } = "GH_QR";
    public string CreatedAt { get; set; } = string.Empty;
    public string? LastPaymentAt { get; set; }
}

public class ClientQrPaymentPreviewDto
{
    public string MerchantCode { get; set; } = string.Empty;
    public string MerchantName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Currency { get; set; } = "GHS";
    public string QrScheme { get; set; } = "BANKINSIGHT_QR";
    public bool GhQrReady { get; set; }
    public decimal? SuggestedAmount { get; set; }
    public string DestinationAccountId { get; set; } = string.Empty;
    public string MerchantProfileId { get; set; } = string.Empty;
}

public class ClientTransferResultDto
{
    public string TransactionId { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string Narration { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal AppliedFees { get; set; }
    public decimal NetAmount { get; set; }
    public decimal NewBalance { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class ClientStandingOrderDto
{
    public string Id { get; set; } = string.Empty;
    public string SourceAccountId { get; set; } = string.Empty;
    public string InstructionType { get; set; } = string.Empty;
    public string? MerchantCode { get; set; }
    public string? MerchantName { get; set; }
    public string? DestinationAccountId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "GHS";
    public string Frequency { get; set; } = string.Empty;
    public string Narration { get; set; } = string.Empty;
    public string StartDate { get; set; } = string.Empty;
    public string NextRunAt { get; set; } = string.Empty;
    public string? EndDate { get; set; }
    public string? LastRunAt { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class ClientFixedDepositDto
{
    public string Id { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public decimal Principal { get; set; }
    public decimal Rate { get; set; }
    public int TenureDays { get; set; }
    public string StartDate { get; set; } = string.Empty;
    public string MaturityDate { get; set; } = string.Empty;
    public string Currency { get; set; } = "GHS";
    public string Status { get; set; } = string.Empty;
    public decimal MaturityValue { get; set; }
}

public class ClientLoanProductDto
{
    public string Id { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ProductType { get; set; } = string.Empty;
    public string RepaymentFrequency { get; set; } = string.Empty;
    public int TermInPeriods { get; set; }
    public decimal AnnualInterestRate { get; set; }
    public decimal MinAmount { get; set; }
    public decimal MaxAmount { get; set; }
}

public class ClientLoanSummaryDto
{
    public string Id { get; set; } = string.Empty;
    public string? ProductCode { get; set; }
    public string? ProductName { get; set; }
    public decimal Principal { get; set; }
    public decimal Rate { get; set; }
    public int TermMonths { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal? OutstandingBalance { get; set; }
    public string? ServicingAccountId { get; set; }
    public string? RepaymentFrequency { get; set; }
    public string? DisbursementDate { get; set; }
    public string ParBucket { get; set; } = string.Empty;
}

public class CreateClientInternalTransferRequest
{
    [Required]
    [StringLength(50)]
    public string FromAccountId { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string ToAccountId { get; set; } = string.Empty;

    [Range(0.01, 999999999.99)]
    public decimal Amount { get; set; }

    [Required]
    [StringLength(500, MinimumLength = 3)]
    public string Narration { get; set; } = string.Empty;

    [Required]
    public string StepUpToken { get; set; } = string.Empty;
}

public class CreateClientMerchantPaymentRequest
{
    [Required]
    [StringLength(50)]
    public string MerchantCode { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string SourceAccountId { get; set; } = string.Empty;

    [Range(0.01, 999999999.99)]
    public decimal Amount { get; set; }

    [StringLength(500)]
    public string? Narration { get; set; }

    [Required]
    public string StepUpToken { get; set; } = string.Empty;
}

public class CreateClientMerchantProfileRequest
{
    [Required]
    [StringLength(50)]
    public string SettlementAccountId { get; set; } = string.Empty;

    [Required]
    [StringLength(200, MinimumLength = 3)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string Category { get; set; } = "General";

    [Required]
    public string StepUpToken { get; set; } = string.Empty;
}

public class ResolveClientQrPaymentRequest
{
    [Required]
    public string QrPayload { get; set; } = string.Empty;
}

public class CreateClientQrPaymentRequest
{
    [Required]
    public string QrPayload { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string SourceAccountId { get; set; } = string.Empty;

    [Range(0.01, 999999999.99)]
    public decimal Amount { get; set; }

    [StringLength(500)]
    public string? Narration { get; set; }

    [Required]
    public string StepUpToken { get; set; } = string.Empty;
}

public class CreateClientStandingOrderRequest
{
    [Required]
    [StringLength(50)]
    public string SourceAccountId { get; set; } = string.Empty;

    [Required]
    [StringLength(30)]
    public string InstructionType { get; set; } = string.Empty;

    [StringLength(50)]
    public string? MerchantCode { get; set; }

    [StringLength(50)]
    public string? DestinationAccountId { get; set; }

    [Range(0.01, 999999999.99)]
    public decimal Amount { get; set; }

    [Required]
    [StringLength(20)]
    public string Frequency { get; set; } = string.Empty;

    [Required]
    [StringLength(500, MinimumLength = 3)]
    public string Narration { get; set; } = string.Empty;

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    [Required]
    public string StepUpToken { get; set; } = string.Empty;
}

public class UpdateClientStandingOrderStatusRequest
{
    [Required]
    [StringLength(20)]
    public string Status { get; set; } = string.Empty;
}

public class CreateClientFixedDepositRequest
{
    [Required]
    [StringLength(50)]
    public string SourceAccountId { get; set; } = string.Empty;

    [Range(0.01, 999999999.99)]
    public decimal Principal { get; set; }

    [Range(0.01, 100)]
    public decimal Rate { get; set; }

    [Range(1, 3650)]
    public int TenureDays { get; set; }

    [Required]
    [StringLength(10)]
    public string Currency { get; set; } = "GHS";

    [Required]
    public string StepUpToken { get; set; } = string.Empty;
}

public class CreateClientLoanApplicationRequest
{
    [Required]
    [StringLength(50)]
    public string LoanProductId { get; set; } = string.Empty;

    [Range(0.01, 999999999.99)]
    public decimal Principal { get; set; }

    [StringLength(50)]
    public string? ServicingAccountId { get; set; }

    [Required]
    public string StepUpToken { get; set; } = string.Empty;
}

public class ClientLoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(255, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;
}

public class ClientVerifyMfaRequest
{
    [Required]
    public string MfaToken { get; set; } = string.Empty;

    [Required]
    public string Code { get; set; } = string.Empty;
}

public class ClientResendMfaRequest
{
    [Required]
    public string MfaToken { get; set; } = string.Empty;
}

public class ClientRefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

public class ClientLoginResponse
{
    public ClientIdentityDto? User { get; set; }
    public string? Token { get; set; }
    public string? RefreshToken { get; set; }
    public bool MfaRequired { get; set; }
    public string? MfaToken { get; set; }
    public string? DeliveryChannel { get; set; }
    public string? DeliveryHint { get; set; }
    public string? DeliveryStatus { get; set; }
    public string? DeliveryMessage { get; set; }
    public DateTime? MfaExpiresAtUtc { get; set; }
    public string[] AllowedFactors { get; set; } = [];
    public string? DebugCode { get; set; }
}

public class ClientRegisterRequest
{
    [Required]
    [StringLength(255, MinimumLength = 3)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Phone]
    public string Phone { get; set; } = string.Empty;

    [StringLength(50)]
    public string? DigitalAddress { get; set; }

    [StringLength(50)]
    public string? GhanaCard { get; set; }

    [Required]
    [StringLength(255, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;
}

public class ClientVerifyRegistrationRequest
{
    [Required]
    public string RegistrationToken { get; set; } = string.Empty;

    [Required]
    public string Code { get; set; } = string.Empty;
}

public class ClientStartPasswordResetRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public class ClientCompletePasswordResetRequest
{
    [Required]
    public string ResetToken { get; set; } = string.Empty;

    [Required]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(255, MinimumLength = 8)]
    public string NewPassword { get; set; } = string.Empty;
}

public class ClientStartStepUpRequest
{
    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string Purpose { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Factor { get; set; }
}

public class ClientVerifyStepUpRequest
{
    [Required]
    public string ChallengeToken { get; set; } = string.Empty;

    [Required]
    public string Code { get; set; } = string.Empty;
}

public class ClientVerificationChallengeResponse
{
    public bool ChallengeRequired { get; set; }
    public string ChallengeToken { get; set; } = string.Empty;
    public string DeliveryChannel { get; set; } = string.Empty;
    public string DeliveryHint { get; set; } = string.Empty;
    public string DeliveryStatus { get; set; } = string.Empty;
    public string DeliveryMessage { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public string? DebugCode { get; set; }
    public string Factor { get; set; } = "otp";
    public string[] AllowedFactors { get; set; } = [];
}

public class ClientVerifiedStepUpResponse
{
    public string StepUpToken { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public string Factor { get; set; } = "otp";
}

public class ClientSetTransactionPinRequest
{
    [Required]
    [StringLength(255, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^\d{4}$")]
    public string Pin { get; set; } = string.Empty;
}

public class ClientPasswordResetStartResponse
{
    public bool Accepted { get; set; }
    public string? ResetToken { get; set; }
    public string? DeliveryHint { get; set; }
    public string? DeliveryChannel { get; set; }
    public string? DeliveryStatus { get; set; }
    public string? DeliveryMessage { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public string? DebugCode { get; set; }
}

public class ClientOperationResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class StaffComplaintQueueItemDto : ClientComplaintListItemDto
{
    public string CustomerId { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public int AttachmentCount { get; set; }
    public int EventCount { get; set; }
    public bool IsSlaBreached { get; set; }
    public int SlaHoursRemaining { get; set; }
}

public class ComplaintQueueSummaryDto
{
    public int TotalOpen { get; set; }
    public int TotalBreached { get; set; }
    public int DueWithin24Hours { get; set; }
    public int AwaitingCustomerInput { get; set; }
    public int UnderReview { get; set; }
    public int Escalated { get; set; }
}

public class ComplaintSlaProcessingResultDto
{
    public int ProcessedCount { get; set; }
    public int BreachedCount { get; set; }
    public int EscalatedCount { get; set; }
    public List<string> ComplaintIds { get; set; } = new();
}

public class TriageClientComplaintRequest
{
    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string OwnerTeam { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string Status { get; set; } = string.Empty;

    [Required]
    [StringLength(1000, MinimumLength = 5)]
    public string Note { get; set; } = string.Empty;
}

public class EscalateClientComplaintRequest
{
    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string EscalationTeam { get; set; } = string.Empty;

    [Required]
    [StringLength(1000, MinimumLength = 5)]
    public string Reason { get; set; } = string.Empty;

    public bool ResetSlaWindow { get; set; } = true;
}

public class CloseClientComplaintRequest
{
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string ResolutionCode { get; set; } = string.Empty;

    [Required]
    [StringLength(1000, MinimumLength = 5)]
    public string ResolutionNote { get; set; } = string.Empty;
}

public class ReopenClientComplaintRequest
{
    [Required]
    [StringLength(1000, MinimumLength = 5)]
    public string Reason { get; set; } = string.Empty;
}

public class ReviewClientKycCaseRequest
{
    [Required]
    [StringLength(30, MinimumLength = 3)]
    public string Decision { get; set; } = string.Empty;

    [Required]
    [StringLength(1000, MinimumLength = 5)]
    public string Note { get; set; } = string.Empty;
}
