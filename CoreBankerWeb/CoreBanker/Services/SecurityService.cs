namespace CoreBanker.Services
{
    public class SecurityService : ApiClientBase
    {
        public SecurityService(HttpClient httpClient, CoreBanker.State.AppState appState) : base(httpClient, appState) { }

        public async Task<SecuritySummaryDto?> GetSummaryAsync(int sinceHours = 24, CancellationToken cancellationToken = default)
        {
            return await GetAsync<SecuritySummaryDto>($"/api/security/summary?sinceHours={sinceHours}", cancellationToken);
        }

        public async Task<List<SecurityAlertDto>> GetAlertsAsync(int limit = 100, CancellationToken cancellationToken = default)
        {
            var result = await GetAsync<List<SecurityAlertDto>>($"/api/security/alerts?limit={limit}", cancellationToken);
            return result ?? new List<SecurityAlertDto>();
        }

        public async Task<List<FailedLoginAttemptDto>> GetFailedLoginsAsync(int sinceMinutes = 60, int limit = 100, CancellationToken cancellationToken = default)
        {
            var result = await GetAsync<List<FailedLoginAttemptDto>>($"/api/security/failed-logins?sinceMinutes={sinceMinutes}&limit={limit}", cancellationToken);
            return result ?? new List<FailedLoginAttemptDto>();
        }

        public async Task<List<SecuritySessionDto>> GetSessionsAsync(CancellationToken cancellationToken = default)
        {
            var result = await GetAsync<List<SecuritySessionDto>>("/api/security/sessions", cancellationToken);
            return result ?? new List<SecuritySessionDto>();
        }

        public async Task<List<SecurityDeviceDto>> GetDevicesAsync(CancellationToken cancellationToken = default)
        {
            var result = await GetAsync<List<SecurityDeviceDto>>("/api/security/devices", cancellationToken);
            return result ?? new List<SecurityDeviceDto>();
        }

        public async Task<SecurityDeviceDto?> RegisterDeviceAsync(RegisterTerminalDeviceRequest request, CancellationToken cancellationToken = default)
        {
            return await PostAsync<RegisterTerminalDeviceRequest, SecurityDeviceDto>("/api/security/devices", request, cancellationToken);
        }

        public async Task<SecurityDeviceDto?> ExecuteDeviceActionAsync(string deviceId, DeviceActionRequest request, CancellationToken cancellationToken = default)
        {
            return await PostAsync<DeviceActionRequest, SecurityDeviceDto>($"/api/security/devices/{Uri.EscapeDataString(deviceId)}/actions", request, cancellationToken);
        }

        public async Task<DeviceScanResultDto?> ScanOutdatedDevicesAsync(CancellationToken cancellationToken = default)
        {
            return await PostAsync<object, DeviceScanResultDto>("/api/security/devices/scan-outdated", new { }, cancellationToken);
        }

        public async Task<List<TransactionIrregularityDto>> GetIrregularTransactionsAsync(int hours = 72, int limit = 100, CancellationToken cancellationToken = default)
        {
            var result = await GetAsync<List<TransactionIrregularityDto>>($"/api/security/irregular-transactions?hours={hours}&limit={limit}", cancellationToken);
            return result ?? new List<TransactionIrregularityDto>();
        }

        public async Task<WafProfileDto?> GetWafProfileAsync(int incidentLimit = 25, CancellationToken cancellationToken = default)
        {
            return await GetAsync<WafProfileDto>($"/api/security/waf?incidentLimit={incidentLimit}", cancellationToken);
        }

        public async Task<WafProfileDto?> UpdateWafProfileAsync(UpdateWafProfileRequest request, CancellationToken cancellationToken = default)
        {
            return await PutAsync<UpdateWafProfileRequest, WafProfileDto>("/api/security/waf", request, cancellationToken);
        }
    }

    public class SecurityAlertDto
    {
        public int Id { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string? EntityId { get; set; }
        public string? UserId { get; set; }
        public string? Description { get; set; }
        public string? IpAddress { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public string? NewValues { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SecuritySummaryDto
    {
        public int WindowHours { get; set; }
        public int FailedLoginCount { get; set; }
        public int SecurityAlertCount { get; set; }
        public int LargeTransactionAlertCount { get; set; }
        public int RegisteredDevices { get; set; }
        public int ActiveDevices { get; set; }
        public int BlockedDevices { get; set; }
        public int IsolatedDevices { get; set; }
        public int OutdatedDevices { get; set; }
        public int IrregularActivityCount { get; set; }
        public int NewlyObservedDevices { get; set; }
        public int MonitoredDevices { get; set; }
        public int SuspiciousDevices { get; set; }
        public int RestrictedDevices { get; set; }
        public int RevokedDevices { get; set; }
        public int ActiveSessions { get; set; }
        public bool WafEnabled { get; set; }
        public string WafMode { get; set; } = "DETECTION";
        public int WafDetectedCount { get; set; }
        public int WafBlockedCount { get; set; }
        public string MinimumSupportedVersion { get; set; } = "2.0.0";
        public DateTime GeneratedAt { get; set; }
    }

    public class FailedLoginAttemptDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string? FailureReason { get; set; }
        public string? UserAgent { get; set; }
        public DateTime AttemptedAt { get; set; }
    }

    public class RegisterTerminalDeviceRequest
    {
        public string DeviceId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DeviceType { get; set; } = "CASH_TERMINAL";
        public string? BranchId { get; set; }
        public string? AssignedStaffId { get; set; }
        public string? SerialNumber { get; set; }
        public string? IpAddress { get; set; }
        public string SoftwareVersion { get; set; } = "1.0.0";
        public string? MinimumSupportedVersion { get; set; }
        public string? Notes { get; set; }
    }

    public class DeviceActionRequest
    {
        public string Action { get; set; } = string.Empty;
        public string? Reason { get; set; }
        public string? SoftwareVersion { get; set; }
        public string? MinimumSupportedVersion { get; set; }
        public string? Notes { get; set; }
    }

    public class SecurityDeviceDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DeviceType { get; set; } = string.Empty;
        public string Status { get; set; } = "ACTIVE";
        public string LifecycleState { get; set; } = "ALLOWED";
        public string AccessDecision { get; set; } = "ALLOWED";
        public string RiskLevel { get; set; } = "LOW";
        public string SoftwareStatus { get; set; } = "COMPLIANT";
        public string SoftwareVersion { get; set; } = "1.0.0";
        public string MinimumSupportedVersion { get; set; } = "2.0.0";
        public string? BranchId { get; set; }
        public string? BranchName { get; set; }
        public string? AssignedStaffId { get; set; }
        public string? AssignedStaffName { get; set; }
        public string? SerialNumber { get; set; }
        public string? IpAddress { get; set; }
        public string? Notes { get; set; }
        public string? BlockReason { get; set; }
        public string? DetectionSource { get; set; }
        public string? UserAgent { get; set; }
        public string? LastSeenUserId { get; set; }
        public string? LastSeenUserName { get; set; }
        public string? LastAction { get; set; }
        public string? LastActionByUserId { get; set; }
        public bool AutoObserved { get; set; }
        public bool RequiresReview { get; set; }
        public int ObservationCount { get; set; }
        public DateTime? FirstObservedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? LastSeenAt { get; set; }
        public DateTime? LastPatchedAt { get; set; }
        public DateTime? LastBlockedAt { get; set; }
        public DateTime? LastActionAt { get; set; }
    }

    public class DeviceScanResultDto
    {
        public string MinimumSupportedVersion { get; set; } = "2.0.0";
        public int ScannedCount { get; set; }
        public int OutdatedCount { get; set; }
        public int FlaggedCount { get; set; }
        public IReadOnlyList<SecurityDeviceDto> Devices { get; set; } = Array.Empty<SecurityDeviceDto>();
        public DateTime ScannedAt { get; set; }
    }

    public class TransactionIrregularityDto
    {
        public string Id { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public string? Reference { get; set; }
        public string? AccountId { get; set; }
        public string? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string Type { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Severity { get; set; } = "LOW";
        public int RiskScore { get; set; }
        public string Summary { get; set; } = string.Empty;
        public List<string> Flags { get; set; } = new();
        public string? TellerId { get; set; }
        public string? TellerName { get; set; }
        public string? Status { get; set; }
        public DateTime TransactionDate { get; set; }
        public DateTime DetectedAt { get; set; }
    }

    public class SecuritySessionDto
    {
        public string Id { get; set; } = string.Empty;
        public string StaffId { get; set; } = string.Empty;
        public string StaffName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string? UserAgent { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime LastActivity { get; set; }
        public bool IsActive { get; set; }
    }

    public class WafIncidentDto
    {
        public int AuditLogId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Outcome { get; set; } = string.Empty;
        public string Mode { get; set; } = "DETECTION";
        public string RuleCode { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public string RequestPath { get; set; } = string.Empty;
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? Description { get; set; }
        public DateTime DetectedAt { get; set; }
    }

    public class WafProfileDto
    {
        public bool Enabled { get; set; }
        public string Mode { get; set; } = "DETECTION";
        public int MaxRequestBodyBytes { get; set; } = 262144;
        public bool BlockSqlInjection { get; set; } = true;
        public bool BlockXss { get; set; } = true;
        public bool BlockPathTraversal { get; set; } = true;
        public bool BlockBadBots { get; set; } = true;
        public List<string> ProtectedPaths { get; set; } = new();
        public List<string> TrustedIps { get; set; } = new();
        public List<string> BlockedUserAgents { get; set; } = new();
        public int DetectedCount24Hours { get; set; }
        public int BlockedCount24Hours { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime GeneratedAt { get; set; }
        public IReadOnlyList<WafIncidentDto> RecentIncidents { get; set; } = Array.Empty<WafIncidentDto>();
    }

    public class UpdateWafProfileRequest
    {
        public bool Enabled { get; set; }
        public string Mode { get; set; } = "DETECTION";
        public int MaxRequestBodyBytes { get; set; } = 262144;
        public bool BlockSqlInjection { get; set; } = true;
        public bool BlockXss { get; set; } = true;
        public bool BlockPathTraversal { get; set; } = true;
        public bool BlockBadBots { get; set; } = true;
        public List<string> ProtectedPaths { get; set; } = new();
        public List<string> TrustedIps { get; set; } = new();
        public List<string> BlockedUserAgents { get; set; } = new();
    }
}
