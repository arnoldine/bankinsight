namespace BankInsight.API.DTOs;

public class OrassProfileDto
{
    public bool Enabled { get; set; }
    public string InstitutionCode { get; set; } = string.Empty;
    public string SubmissionMode { get; set; } = "TEST";
    public string EndpointUrl { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string CertificateAlias { get; set; } = string.Empty;
    public string SourceReportCode { get; set; } = "REG-BOG-DBK-ORASS";
    public bool AutoSubmit { get; set; }
    public string CutoffTimeUtc { get; set; } = "17:00";
    public string FallbackEmail { get; set; } = string.Empty;
    public string? LastSubmissionAt { get; set; }
}

public class OrassReadinessDto
{
    public bool ProfileConfigured { get; set; }
    public bool ReadyForSubmission { get; set; }
    public string SubmissionMode { get; set; } = "TEST";
    public string SourceReportCode { get; set; } = string.Empty;
    public int PendingReturns { get; set; }
    public int ReturnsReadyForSubmission { get; set; }
    public string[] MissingRequirements { get; set; } = [];
    public string[] Notes { get; set; } = [];
    public string? LastPreparedReturnDate { get; set; }
    public string? LastSubmissionAt { get; set; }
}

public class OrassQueueItemDto
{
    public int Id { get; set; }
    public string ReturnType { get; set; } = string.Empty;
    public string ReturnDate { get; set; } = string.Empty;
    public string ReportingPeriodStart { get; set; } = string.Empty;
    public string ReportingPeriodEnd { get; set; } = string.Empty;
    public string SubmissionStatus { get; set; } = string.Empty;
    public int TotalRecords { get; set; }
    public bool IsReadyForSubmission { get; set; }
    public string ValidationStatus { get; set; } = "VALID";
    public string[] ValidationMessages { get; set; } = [];
}

public class OrassSubmissionHistoryItemDto
{
    public int Id { get; set; }
    public string ReturnType { get; set; } = string.Empty;
    public string ReturnDate { get; set; } = string.Empty;
    public string SubmissionStatus { get; set; } = string.Empty;
    public string? SubmissionDate { get; set; }
    public string SubmittedBy { get; set; } = string.Empty;
    public string BogReferenceNumber { get; set; } = string.Empty;
    public string TransportStatus { get; set; } = "NOT_SENT";
    public string AcknowledgementStatus { get; set; } = "PENDING";
    public string? AcknowledgementReference { get; set; }
    public string? AcknowledgedAt { get; set; }
    public string? TransportMessage { get; set; }
    public string[] ValidationMessages { get; set; } = [];
}

public class OrassReconciliationResultDto
{
    public int ScannedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int PendingCount { get; set; }
    public string ExecutionMode { get; set; } = "TEST";
    public string ExecutedAt { get; set; } = string.Empty;
    public string[] Notes { get; set; } = [];
    public OrassSubmissionHistoryItemDto[] UpdatedItems { get; set; } = [];
}

public class OrassSubmissionEvidenceDto
{
    public int ReturnId { get; set; }
    public string TransmissionId { get; set; } = string.Empty;
    public string SubmissionMode { get; set; } = "TEST";
    public string EndpointUrl { get; set; } = string.Empty;
    public string TransportStatus { get; set; } = "NOT_SENT";
    public string AcknowledgementStatus { get; set; } = "PENDING";
    public string? AcknowledgementReference { get; set; }
    public string? SubmittedAt { get; set; }
    public string? AcknowledgedAt { get; set; }
    public string? PayloadHash { get; set; }
    public string? ProviderStatusCode { get; set; }
    public string? TransportMessage { get; set; }
    public string[] Notes { get; set; } = [];
}

public class UpdateOrassAcknowledgementRequest
{
    public string Status { get; set; } = "RECEIVED";
    public string? AcknowledgementReference { get; set; }
    public string? Message { get; set; }
}
