namespace BankInsight.API.Services;

using System.Net;
using System.Net.Mail;

public interface IEmailAlertService
{
    Task SendSecurityAlertAsync(string subject, string message, object? context = null);
}

public class EmailAlertService : IEmailAlertService
{
    private readonly ILogger<EmailAlertService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IAuditLoggingService? _auditLoggingService;
    private readonly SmtpSettings? _smtpSettings;

    public EmailAlertService(
        ILogger<EmailAlertService> logger,
        IConfiguration configuration,
        IAuditLoggingService? auditLoggingService = null)
    {
        _logger = logger;
        _configuration = configuration;
        _auditLoggingService = auditLoggingService;
        _smtpSettings = configuration.GetSection("SmtpSettings").Get<SmtpSettings>();
    }

    public async Task SendSecurityAlertAsync(string subject, string message, object? context = null)
    {
        try
        {
            // If SMTP is not configured, fall back to logging
            if (_smtpSettings?.Enabled != true || string.IsNullOrEmpty(_smtpSettings?.Host))
            {
                _logger.LogWarning("SECURITY ALERT (SMTP DISABLED): {Subject} | {Message} | Context: {@Context}", subject, message, context);
                return;
            }

            var recipients = _smtpSettings.Recipients ?? new List<string>();
            if (!recipients.Any())
            {
                _logger.LogWarning("SECURITY ALERT: No recipients configured for email delivery. {Subject} | {Message}", subject, message);
                return;
            }

            using (var smtpClient = new SmtpClient(_smtpSettings.Host, _smtpSettings.Port))
            {
                smtpClient.EnableSsl = _smtpSettings.EnableSsl;
                smtpClient.Timeout = _smtpSettings.TimeoutSeconds * 1000;

                if (!string.IsNullOrEmpty(_smtpSettings.Username) && !string.IsNullOrEmpty(_smtpSettings.Password))
                {
                    smtpClient.Credentials = new NetworkCredential(_smtpSettings.Username, _smtpSettings.Password);
                }

                using (var mailMessage = new MailMessage())
                {
                    var fromAddress = _smtpSettings.FromAddress ?? "alerts@bankinsight.local";
                    mailMessage.From = new MailAddress(fromAddress, "BankInsight Security");
                    mailMessage.Subject = $"[SECURITY] {subject}";
                    mailMessage.Body = $"Timestamp: {DateTime.UtcNow:O}\n\n{message}";
                    if (context != null)
                    {
                        mailMessage.Body += $"\n\nContext: {System.Text.Json.JsonSerializer.Serialize(context)}";
                    }
                    mailMessage.IsBodyHtml = false;

                    foreach (var recipient in recipients)
                    {
                        if (!string.IsNullOrWhiteSpace(recipient))
                        {
                            mailMessage.To.Add(recipient);
                        }
                    }

                    await smtpClient.SendMailAsync(mailMessage);
                    _logger.LogInformation("Security alert email sent: {Subject} to {RecipientCount} recipients", subject, mailMessage.To.Count);

                    // Log successful email delivery to audit trail
                    if (_auditLoggingService != null)
                    {
                        await _auditLoggingService.LogActionAsync(
                            action: "SECURITY_EMAIL_SENT",
                            entityType: "SECURITY",
                            entityId: null,
                            userId: null,
                            description: $"Security alert email delivered: {subject}",
                            status: "SUCCESS",
                            newValues: new { recipients = mailMessage.To.Count, subject });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send security alert email: {Subject}", subject);

            // Log failed email delivery to audit trail
            if (_auditLoggingService != null)
            {
                await _auditLoggingService.LogActionAsync(
                    action: "SECURITY_EMAIL_FAILED",
                    entityType: "SECURITY",
                    entityId: null,
                    userId: null,
                    description: $"Failed to send security alert email: {subject}",
                    status: "FAILED",
                    errorMessage: ex.Message,
                    newValues: new { subject, errorType = ex.GetType().Name });
            }
            // Rethrow to ensure alert is not silently lost
            throw;
        }
    }
}

public class SmtpSettings
{
    public bool Enabled { get; set; } = false;
    public string? Host { get; set; }
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? FromAddress { get; set; } = "alerts@bankinsight.local";
    public List<string>? Recipients { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
}

public interface ISuspiciousActivityService
{
    Task HandleFailedLoginAsync(string email, string ipAddress, string reason);
    Task HandleLargeTransactionAsync(string accountId, decimal amount, string transactionType, string? staffId);
}

public class SuspiciousActivityService : ISuspiciousActivityService
{
    private readonly IEmailAlertService _emailAlertService;
    private readonly IAuditLoggingService _auditLoggingService;
    private readonly ILogger<SuspiciousActivityService> _logger;
    private readonly IConfiguration _configuration;

    public SuspiciousActivityService(
        IEmailAlertService emailAlertService,
        IAuditLoggingService auditLoggingService,
        ILogger<SuspiciousActivityService> logger,
        IConfiguration configuration)
    {
        _emailAlertService = emailAlertService;
        _auditLoggingService = auditLoggingService;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task HandleFailedLoginAsync(string email, string ipAddress, string reason)
    {
        _logger.LogWarning("Suspicious login event for {Email} from {IpAddress}. Reason: {Reason}", email, ipAddress, reason);

        await _auditLoggingService.LogActionAsync(
            action: "SECURITY_ALERT_FAILED_LOGIN",
            entityType: "SECURITY",
            entityId: null,
            userId: null,
            description: $"Failed login attempt for {email}",
            ipAddress: ipAddress,
            status: "ALERT",
            errorMessage: reason,
            newValues: new { email, ipAddress, reason, detectedAt = DateTime.UtcNow });

        await _emailAlertService.SendSecurityAlertAsync(
            "Failed login attempt detected",
            $"User {email} had a failed login attempt from IP {ipAddress}. Reason: {reason}",
            new { email, ipAddress, reason, detectedAt = DateTime.UtcNow });
    }

    public async Task HandleLargeTransactionAsync(string accountId, decimal amount, string transactionType, string? staffId)
    {
        var threshold = _configuration.GetValue<decimal>("Security:SuspiciousActivity:LargeTransactionThreshold");
        if (threshold <= 0)
        {
            threshold = 100000m;
        }

        if (amount < threshold)
        {
            return;
        }

        _logger.LogWarning(
            "Suspicious transaction event. Account: {AccountId}, Amount: {Amount}, Type: {TransactionType}, Staff: {StaffId}, Threshold: {Threshold}",
            accountId, amount, transactionType, staffId, threshold);

        await _auditLoggingService.LogActionAsync(
            action: "SECURITY_ALERT_LARGE_TRANSACTION",
            entityType: "TRANSACTION",
            entityId: accountId,
            userId: staffId,
            description: $"Large {transactionType} transaction detected",
            status: "ALERT",
            newValues: new { accountId, amount, transactionType, staffId, threshold, detectedAt = DateTime.UtcNow });

        await _emailAlertService.SendSecurityAlertAsync(
            "Large transaction detected",
            $"Transaction {transactionType} of {amount:N2} on account {accountId} exceeded threshold {threshold:N2}.",
            new { accountId, amount, transactionType, staffId, threshold, detectedAt = DateTime.UtcNow });
    }
}
