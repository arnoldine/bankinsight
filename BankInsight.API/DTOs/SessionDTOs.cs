namespace BankInsight.API.DTOs;

// Session Management DTOs
public class SessionDto
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

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class RefreshTokenResponse
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public class SessionStatsDto
{
    public int TotalActiveSessions { get; set; }
    public int TotalSessionsToday { get; set; }
    public Dictionary<string, int> SessionsByBranch { get; set; } = new();
}

// Login Attempt DTOs
public class LoginAttemptDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
    public DateTime AttemptedAt { get; set; }
}

// User Activity DTOs
public class UserActivityDto
{
    public long Id { get; set; }
    public string StaffId { get; set; } = string.Empty;
    public string StaffName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateActivityRequest
{
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? BeforeValue { get; set; }
    public string? AfterValue { get; set; }
}

public class UserActivityReportDto
{
    public string StaffId { get; set; } = string.Empty;
    public string StaffName { get; set; } = string.Empty;
    public int TotalActions { get; set; }
    public Dictionary<string, int> ActionCounts { get; set; } = new();
    public DateTime FirstActivity { get; set; }
    public DateTime LastActivity { get; set; }
}
