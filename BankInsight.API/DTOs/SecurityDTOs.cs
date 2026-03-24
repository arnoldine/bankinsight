using System;
using System.Collections.Generic;

namespace BankInsight.API.DTOs;

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
