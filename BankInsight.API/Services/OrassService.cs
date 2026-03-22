using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Services;

public interface IOrassService
{
    Task<OrassProfileDto> GetProfileAsync();
    Task<OrassReadinessDto> GetReadinessAsync();
    Task<IReadOnlyList<OrassQueueItemDto>> GetQueueAsync();
    Task<IReadOnlyList<OrassSubmissionHistoryItemDto>> GetHistoryAsync(int take = 20);
    Task<OrassSubmissionHistoryItemDto> SubmitAsync(int returnId, string submittedBy);
    Task<OrassSubmissionEvidenceDto?> GetEvidenceAsync(int returnId);
    Task<OrassSubmissionHistoryItemDto> UpdateAcknowledgementAsync(int returnId, UpdateOrassAcknowledgementRequest request, string updatedBy);
    Task<OrassReconciliationResultDto> ReconcileAcknowledgementsAsync(string executedBy);
}

public class OrassService : IOrassService
{
    private const string OrassConfigKey = "orass";
    private const string OrassEvidenceKeyPrefix = "orass_submission_";

    private readonly ApplicationDbContext _context;
    private readonly IRegulatoryReportService _regulatoryReportService;
    private readonly IAuditLoggingService _auditLoggingService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OrassService> _logger;

    public OrassService(
        ApplicationDbContext context,
        IRegulatoryReportService regulatoryReportService,
        IAuditLoggingService auditLoggingService,
        IHttpClientFactory httpClientFactory,
        ILogger<OrassService> logger)
    {
        _context = context;
        _regulatoryReportService = regulatoryReportService;
        _auditLoggingService = auditLoggingService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<OrassProfileDto> GetProfileAsync()
    {
        var configValue = await _context.SystemConfigs
            .Where(config => config.Key == OrassConfigKey)
            .Select(config => config.Value)
            .FirstOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(configValue))
        {
            return new OrassProfileDto();
        }

        try
        {
            return JsonSerializer.Deserialize<OrassProfileDto>(configValue, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new OrassProfileDto();
        }
        catch
        {
            return new OrassProfileDto();
        }
    }

    public async Task<OrassReadinessDto> GetReadinessAsync()
    {
        var profile = await GetProfileAsync();
        var missing = GetMissingRequirements(profile);
        var notes = new List<string>();

        var pendingStatuses = new[] { "PendingApproval", "Approved" };
        var pendingReturns = await _context.RegulatoryReturns
            .Where(ret => ret.ReturnType == profile.SourceReportCode && pendingStatuses.Contains(ret.SubmissionStatus))
            .CountAsync();

        var readyReturns = await _context.RegulatoryReturns
            .Where(ret => ret.ReturnType == profile.SourceReportCode && ret.SubmissionStatus == "Approved")
            .CountAsync();

        var latestPreparedReturn = await _context.RegulatoryReturns
            .Where(ret => ret.ReturnType == profile.SourceReportCode)
            .OrderByDescending(ret => ret.ReturnDate)
            .Select(ret => ret.ReturnDate)
            .FirstOrDefaultAsync();

        if (pendingReturns == 0)
        {
            notes.Add("No ORASS regulatory returns are currently queued for submission.");
        }

        if (profile.SubmissionMode.Equals("TEST", StringComparison.OrdinalIgnoreCase))
        {
            notes.Add("Profile is in TEST mode. Outbound submissions are simulated until the regulator endpoint is certified.");
        }

        notes.Add("Submission evidence and acknowledgement tracking are active. Full automated reconciliation still needs a dedicated worker.");

        return new OrassReadinessDto
        {
            ProfileConfigured = missing.Count == 0,
            ReadyForSubmission = missing.Count == 0 && readyReturns > 0,
            SubmissionMode = profile.SubmissionMode,
            SourceReportCode = profile.SourceReportCode,
            PendingReturns = pendingReturns,
            ReturnsReadyForSubmission = readyReturns,
            MissingRequirements = missing.ToArray(),
            Notes = notes.ToArray(),
            LastPreparedReturnDate = latestPreparedReturn == default ? null : latestPreparedReturn.ToString("yyyy-MM-dd"),
            LastSubmissionAt = profile.LastSubmissionAt
        };
    }

    public async Task<IReadOnlyList<OrassQueueItemDto>> GetQueueAsync()
    {
        var profile = await GetProfileAsync();
        if (string.IsNullOrWhiteSpace(profile.SourceReportCode))
        {
            return [];
        }

        var queueStatuses = new[] { "Draft", "PendingApproval", "Approved", "Rejected" };
        var items = await _context.RegulatoryReturns
            .Where(ret => ret.ReturnType == profile.SourceReportCode && queueStatuses.Contains(ret.SubmissionStatus))
            .OrderByDescending(ret => ret.ReturnDate)
            .ThenByDescending(ret => ret.UpdatedAt)
            .ToListAsync();

        return items.Select(MapQueueItem).ToList();
    }

    public async Task<IReadOnlyList<OrassSubmissionHistoryItemDto>> GetHistoryAsync(int take = 20)
    {
        var profile = await GetProfileAsync();
        if (string.IsNullOrWhiteSpace(profile.SourceReportCode))
        {
            return [];
        }

        var items = await _context.RegulatoryReturns
            .Where(ret => ret.ReturnType == profile.SourceReportCode && ret.SubmissionDate != null)
            .OrderByDescending(ret => ret.SubmissionDate)
            .Take(Math.Clamp(take, 1, 100))
            .ToListAsync();

        var evidenceLookup = await LoadEvidenceLookupAsync(items.Select(item => item.Id).ToArray());
        return items.Select(item => MapHistoryItem(item, evidenceLookup.GetValueOrDefault(item.Id))).ToList();
    }

    public async Task<OrassSubmissionHistoryItemDto> SubmitAsync(int returnId, string submittedBy)
    {
        var profile = await GetProfileAsync();
        var missingRequirements = GetMissingRequirements(profile);
        if (missingRequirements.Count > 0)
        {
            throw new InvalidOperationException($"ORASS profile is not ready: {string.Join(" ", missingRequirements)}");
        }

        var regulatoryReturn = await _context.RegulatoryReturns.FirstOrDefaultAsync(ret => ret.Id == returnId);
        if (regulatoryReturn == null)
        {
            throw new KeyNotFoundException($"ORASS return {returnId} was not found.");
        }

        if (!string.Equals(regulatoryReturn.ReturnType, profile.SourceReportCode, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Selected return does not match the configured ORASS source report code.");
        }

        var evidence = await PerformTransportAsync(regulatoryReturn, profile, submittedBy);
        var submitted = await _regulatoryReportService.SubmitReturnToBogAsync(returnId, submittedBy);

        evidence.SubmittedAt = submitted.SubmissionDate?.ToString("yyyy-MM-dd HH:mm:ss");
        evidence.TransportStatus = profile.SubmissionMode.Equals("TEST", StringComparison.OrdinalIgnoreCase) ? "SIMULATED_SENT" : "SENT";
        evidence.AcknowledgementStatus = "PENDING";
        await SaveEvidenceAsync(returnId, evidence);
        await UpdateLastSubmissionAsync(profile, regulatoryReturn.ReturnType, submitted.SubmissionDate);

        var updatedEntity = await _context.RegulatoryReturns.FirstAsync(ret => ret.Id == returnId);
        await _auditLoggingService.LogActionAsync(
            "ORASS_SUBMIT",
            "RegulatoryReturn",
            returnId.ToString(),
            submittedBy,
            $"ORASS return {returnId} submitted with transport status {evidence.TransportStatus}.",
            status: "SUCCESS",
            newValues: new
            {
                evidence.TransmissionId,
                evidence.TransportStatus,
                evidence.AcknowledgementStatus,
                updatedEntity.BogReferenceNumber
            });

        _logger.LogInformation("ORASS return {ReturnId} submitted by {SubmittedBy} with reference {Reference}", returnId, submittedBy, updatedEntity.BogReferenceNumber);
        return MapHistoryItem(updatedEntity, evidence);
    }

    public async Task<OrassSubmissionEvidenceDto?> GetEvidenceAsync(int returnId)
    {
        return await LoadEvidenceAsync(returnId);
    }

    public async Task<OrassSubmissionHistoryItemDto> UpdateAcknowledgementAsync(int returnId, UpdateOrassAcknowledgementRequest request, string updatedBy)
    {
        var regulatoryReturn = await _context.RegulatoryReturns.FirstOrDefaultAsync(ret => ret.Id == returnId);
        if (regulatoryReturn == null)
        {
            throw new KeyNotFoundException($"ORASS return {returnId} was not found.");
        }

        var evidence = await LoadEvidenceAsync(returnId);
        if (evidence == null)
        {
            throw new InvalidOperationException("No ORASS submission evidence was found for this return.");
        }

        var normalizedStatus = NormalizeAcknowledgementStatus(request.Status);
        evidence.AcknowledgementStatus = normalizedStatus;
        evidence.AcknowledgementReference = request.AcknowledgementReference?.Trim();
        evidence.AcknowledgedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        if (!string.IsNullOrWhiteSpace(request.Message))
        {
            evidence.TransportMessage = request.Message.Trim();
            evidence.Notes = evidence.Notes.Concat([
                $"Acknowledgement updated to {normalizedStatus} by {updatedBy} at {DateTime.UtcNow:O}. {request.Message.Trim()}"
            ]).ToArray();
        }

        if (normalizedStatus == "ACCEPTED")
        {
            regulatoryReturn.SubmissionStatus = "Accepted";
        }
        else if (normalizedStatus == "REJECTED")
        {
            regulatoryReturn.SubmissionStatus = "Rejected";
            regulatoryReturn.ValidationErrors = MergeValidationNote(
                regulatoryReturn.ValidationErrors,
                request.Message ?? "ORASS acknowledgement rejected the submission.");
        }

        regulatoryReturn.UpdatedAt = DateTime.UtcNow;
        await SaveEvidenceAsync(returnId, evidence);
        await _context.SaveChangesAsync();

        await _auditLoggingService.LogActionAsync(
            "ORASS_ACKNOWLEDGEMENT_UPDATE",
            "RegulatoryReturn",
            returnId.ToString(),
            updatedBy,
            $"Acknowledgement updated to {normalizedStatus}.",
            status: "SUCCESS",
            newValues: new
            {
                evidence.AcknowledgementStatus,
                evidence.AcknowledgementReference,
                evidence.AcknowledgedAt
            });

        return MapHistoryItem(regulatoryReturn, evidence);
    }

    public async Task<OrassReconciliationResultDto> ReconcileAcknowledgementsAsync(string executedBy)
    {
        var profile = await GetProfileAsync();
        var pendingItems = await GetPendingAcknowledgementItemsAsync(profile);
        var updatedItems = new List<OrassSubmissionHistoryItemDto>();
        var notes = new List<string>();

        foreach (var pendingItem in pendingItems)
        {
            var updated = await TryReconcileItemAsync(profile, pendingItem.RegulatoryReturn, pendingItem.Evidence, executedBy);
            if (updated != null)
            {
                updatedItems.Add(updated);
            }
        }

        var pendingCount = pendingItems.Count - updatedItems.Count;
        if (profile.SubmissionMode.Equals("TEST", StringComparison.OrdinalIgnoreCase))
        {
            notes.Add("TEST mode reconciles pending acknowledgements locally to unblock operational verification.");
        }
        else
        {
            notes.Add("PRODUCTION mode reconciliation polls the configured acknowledgement endpoint path derived from the ORASS base URL.");
        }

        if (pendingItems.Count == 0)
        {
            notes.Add("No ORASS submissions were waiting for acknowledgement reconciliation.");
        }

        await _auditLoggingService.LogActionAsync(
            "ORASS_RECONCILIATION_RUN",
            "ORASS",
            "ACK_RECONCILIATION",
            executedBy,
            $"ORASS acknowledgement reconciliation scanned {pendingItems.Count} items and updated {updatedItems.Count}.",
            status: "SUCCESS",
            newValues: new
            {
                scannedCount = pendingItems.Count,
                updatedCount = updatedItems.Count,
                pendingCount,
                profile.SubmissionMode
            });

        return new OrassReconciliationResultDto
        {
            ScannedCount = pendingItems.Count,
            UpdatedCount = updatedItems.Count,
            PendingCount = Math.Max(pendingCount, 0),
            ExecutionMode = profile.SubmissionMode,
            ExecutedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
            Notes = notes.ToArray(),
            UpdatedItems = updatedItems.ToArray()
        };
    }

    private static List<string> GetMissingRequirements(OrassProfileDto profile)
    {
        var missing = new List<string>();

        if (!profile.Enabled)
        {
            missing.Add("ORASS profile is disabled.");
        }

        if (string.IsNullOrWhiteSpace(profile.InstitutionCode))
        {
            missing.Add("Institution code is missing.");
        }

        if (string.IsNullOrWhiteSpace(profile.EndpointUrl))
        {
            missing.Add("Endpoint URL is missing.");
        }

        if (string.IsNullOrWhiteSpace(profile.Username))
        {
            missing.Add("Service username is missing.");
        }

        if (string.IsNullOrWhiteSpace(profile.CertificateAlias))
        {
            missing.Add("Certificate alias is missing.");
        }

        if (string.IsNullOrWhiteSpace(profile.SourceReportCode))
        {
            missing.Add("Source report code is missing.");
        }

        if (profile.SubmissionMode.Equals("PRODUCTION", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(profile.FallbackEmail))
        {
            missing.Add("Fallback escalation email is required for production mode.");
        }

        return missing;
    }

    private static string DetermineValidationStatus(string? rawValue)
    {
        var messages = ParseValidationErrors(rawValue);
        if (messages.Any(message => message.StartsWith("ERROR:", StringComparison.OrdinalIgnoreCase)))
        {
            return "ERROR";
        }

        if (messages.Any(message => message.StartsWith("WARN:", StringComparison.OrdinalIgnoreCase)))
        {
            return "WARNING";
        }

        return "VALID";
    }

    private static List<string> ParseValidationErrors(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue) || rawValue == "[]")
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(rawValue, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? [];
        }
        catch
        {
            return [rawValue];
        }
    }

    private static OrassQueueItemDto MapQueueItem(RegulatoryReturn ret)
    {
        var validationMessages = ParseValidationErrors(ret.ValidationErrors);
        var validationStatus = DetermineValidationStatus(ret.ValidationErrors);

        return new OrassQueueItemDto
        {
            Id = ret.Id,
            ReturnType = ret.ReturnType,
            ReturnDate = ret.ReturnDate.ToString("yyyy-MM-dd"),
            ReportingPeriodStart = ret.ReportingPeriodStart == default ? string.Empty : ret.ReportingPeriodStart.ToString("yyyy-MM-dd"),
            ReportingPeriodEnd = ret.ReportingPeriodEnd == default ? string.Empty : ret.ReportingPeriodEnd.ToString("yyyy-MM-dd"),
            SubmissionStatus = ret.SubmissionStatus,
            TotalRecords = ret.TotalRecords,
            ValidationStatus = validationStatus,
            ValidationMessages = validationMessages.ToArray(),
            IsReadyForSubmission = string.Equals(ret.SubmissionStatus, "Approved", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(validationStatus, "ERROR", StringComparison.OrdinalIgnoreCase)
        };
    }

    private static OrassSubmissionHistoryItemDto MapHistoryItem(RegulatoryReturn ret, OrassSubmissionEvidenceDto? evidence)
    {
        return new OrassSubmissionHistoryItemDto
        {
            Id = ret.Id,
            ReturnType = ret.ReturnType,
            ReturnDate = ret.ReturnDate.ToString("yyyy-MM-dd"),
            SubmissionStatus = ret.SubmissionStatus,
            SubmissionDate = ret.SubmissionDate?.ToString("yyyy-MM-dd HH:mm:ss"),
            SubmittedBy = ret.SubmittedBy ?? string.Empty,
            BogReferenceNumber = ret.BogReferenceNumber ?? string.Empty,
            TransportStatus = evidence?.TransportStatus ?? "NOT_SENT",
            AcknowledgementStatus = evidence?.AcknowledgementStatus ?? "PENDING",
            AcknowledgementReference = evidence?.AcknowledgementReference,
            AcknowledgedAt = evidence?.AcknowledgedAt,
            TransportMessage = evidence?.TransportMessage,
            ValidationMessages = ParseValidationErrors(ret.ValidationErrors).ToArray()
        };
    }

    private async Task UpdateLastSubmissionAsync(OrassProfileDto profile, string returnType, DateTime? submissionDate)
    {
        profile.LastSubmissionAt = (submissionDate ?? DateTime.UtcNow).ToString("yyyy-MM-dd HH:mm:ss");

        var configRow = await _context.SystemConfigs.FirstOrDefaultAsync(config => config.Key == OrassConfigKey);
        var serialized = JsonSerializer.Serialize(new
        {
            enabled = profile.Enabled,
            institutionCode = profile.InstitutionCode,
            submissionMode = profile.SubmissionMode,
            endpointUrl = profile.EndpointUrl,
            username = profile.Username,
            certificateAlias = profile.CertificateAlias,
            sourceReportCode = string.IsNullOrWhiteSpace(profile.SourceReportCode) ? returnType : profile.SourceReportCode,
            autoSubmit = profile.AutoSubmit,
            cutoffTimeUtc = profile.CutoffTimeUtc,
            fallbackEmail = profile.FallbackEmail,
            lastSubmissionAt = profile.LastSubmissionAt
        });

        if (configRow == null)
        {
            _context.SystemConfigs.Add(new SystemConfig
            {
                Id = $"CFG{Guid.NewGuid():N}"[..12].ToUpperInvariant(),
                Key = OrassConfigKey,
                Value = serialized,
                UpdatedAt = DateTime.UtcNow
            });
        }
        else
        {
            configRow.Value = serialized;
            configRow.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    private async Task<Dictionary<int, OrassSubmissionEvidenceDto>> LoadEvidenceLookupAsync(int[] returnIds)
    {
        if (returnIds.Length == 0)
        {
            return [];
        }

        var keys = returnIds.Select(GetEvidenceKey).ToArray();
        var rows = await _context.SystemConfigs
            .Where(config => keys.Contains(config.Key))
            .ToListAsync();

        var result = new Dictionary<int, OrassSubmissionEvidenceDto>();
        foreach (var row in rows)
        {
            var evidence = DeserializeEvidence(row.Value);
            if (evidence != null)
            {
                result[evidence.ReturnId] = evidence;
            }
        }

        return result;
    }

    private async Task<OrassSubmissionEvidenceDto?> LoadEvidenceAsync(int returnId)
    {
        var value = await _context.SystemConfigs
            .Where(config => config.Key == GetEvidenceKey(returnId))
            .Select(config => config.Value)
            .FirstOrDefaultAsync();

        return string.IsNullOrWhiteSpace(value) ? null : DeserializeEvidence(value);
    }

    private async Task SaveEvidenceAsync(int returnId, OrassSubmissionEvidenceDto evidence)
    {
        var key = GetEvidenceKey(returnId);
        var serialized = JsonSerializer.Serialize(evidence);
        var configRow = await _context.SystemConfigs.FirstOrDefaultAsync(config => config.Key == key);

        if (configRow == null)
        {
            _context.SystemConfigs.Add(new SystemConfig
            {
                Id = $"CFG{Guid.NewGuid():N}"[..12].ToUpperInvariant(),
                Key = key,
                Value = serialized,
                Description = $"ORASS submission evidence for return {returnId}",
                UpdatedAt = DateTime.UtcNow
            });
        }
        else
        {
            configRow.Value = serialized;
            configRow.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    private async Task<OrassSubmissionEvidenceDto> PerformTransportAsync(RegulatoryReturn regulatoryReturn, OrassProfileDto profile, string submittedBy)
    {
        var payload = new
        {
            institutionCode = profile.InstitutionCode,
            returnId = regulatoryReturn.Id,
            returnType = regulatoryReturn.ReturnType,
            returnDate = regulatoryReturn.ReturnDate.ToString("yyyy-MM-dd"),
            reportingPeriodStart = regulatoryReturn.ReportingPeriodStart == default ? null : regulatoryReturn.ReportingPeriodStart.ToString("yyyy-MM-dd"),
            reportingPeriodEnd = regulatoryReturn.ReportingPeriodEnd == default ? null : regulatoryReturn.ReportingPeriodEnd.ToString("yyyy-MM-dd"),
            totalRecords = regulatoryReturn.TotalRecords,
            filePath = regulatoryReturn.FilePath,
            submittedBy,
            generatedAt = DateTime.UtcNow.ToString("O")
        };

        var payloadJson = JsonSerializer.Serialize(payload);
        var evidence = new OrassSubmissionEvidenceDto
        {
            ReturnId = regulatoryReturn.Id,
            TransmissionId = $"ORASS-{DateTime.UtcNow:yyyyMMddHHmmss}-{regulatoryReturn.Id}",
            SubmissionMode = profile.SubmissionMode,
            EndpointUrl = profile.EndpointUrl,
            PayloadHash = ComputeSha256(payloadJson),
            AcknowledgementStatus = "PENDING"
        };

        if (profile.SubmissionMode.Equals("TEST", StringComparison.OrdinalIgnoreCase))
        {
            evidence.ProviderStatusCode = "SIMULATED";
            evidence.TransportStatus = "SIMULATED_SENT";
            evidence.TransportMessage = "Submission simulated in TEST mode. No outbound network call was made.";
            evidence.Notes = ["Switch to PRODUCTION only after the regulator endpoint and acknowledgement contract are validated."];
            return evidence;
        }

        try
        {
            using var client = _httpClientFactory.CreateClient();
            using var content = new StringContent(payloadJson, Encoding.UTF8, "application/json");
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.Add("X-ORASS-INSTITUTION", profile.InstitutionCode);
            client.DefaultRequestHeaders.Add("X-ORASS-TRANSMISSION-ID", evidence.TransmissionId);

            var response = await client.PostAsync(profile.EndpointUrl, content);
            var responseBody = await response.Content.ReadAsStringAsync();
            evidence.ProviderStatusCode = ((int)response.StatusCode).ToString();
            evidence.TransportMessage = string.IsNullOrWhiteSpace(responseBody)
                ? $"Provider returned {(int)response.StatusCode}."
                : responseBody[..Math.Min(responseBody.Length, 500)];

            if (!response.IsSuccessStatusCode)
            {
                evidence.TransportStatus = "FAILED";
                evidence.AcknowledgementStatus = "NOT_REQUESTED";
                evidence.Notes = [$"ORASS transport failed with provider status {(int)response.StatusCode}."];
                await SaveEvidenceAsync(regulatoryReturn.Id, evidence);
                await _auditLoggingService.LogActionAsync(
                    "ORASS_TRANSPORT_FAILED",
                    "RegulatoryReturn",
                    regulatoryReturn.Id.ToString(),
                    submittedBy,
                    "ORASS transport failed before submission finalization.",
                    status: "FAILED",
                    errorMessage: evidence.TransportMessage,
                    newValues: new { evidence.TransmissionId, evidence.ProviderStatusCode });
                throw new InvalidOperationException($"ORASS transport failed with provider status {(int)response.StatusCode}.");
            }

            evidence.TransportStatus = "SENT";
            evidence.Notes = ["ORASS payload accepted by the configured endpoint. Await acknowledgement reconciliation."];
            return evidence;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            evidence.TransportStatus = "FAILED";
            evidence.AcknowledgementStatus = "NOT_REQUESTED";
            evidence.TransportMessage = ex.Message;
            evidence.Notes = [$"Transport exception: {ex.Message}"];
            await SaveEvidenceAsync(regulatoryReturn.Id, evidence);
            await _auditLoggingService.LogActionAsync(
                "ORASS_TRANSPORT_EXCEPTION",
                "RegulatoryReturn",
                regulatoryReturn.Id.ToString(),
                submittedBy,
                "ORASS transport raised an exception before submission finalization.",
                status: "FAILED",
                errorMessage: ex.Message,
                newValues: new { evidence.TransmissionId });
            throw new InvalidOperationException("ORASS transport failed before submission could be finalized.", ex);
        }
    }

    private static OrassSubmissionEvidenceDto? DeserializeEvidence(string serialized)
    {
        try
        {
            return JsonSerializer.Deserialize<OrassSubmissionEvidenceDto>(serialized, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return null;
        }
    }

    private static string GetEvidenceKey(int returnId) => $"{OrassEvidenceKeyPrefix}{returnId}";

    private async Task<List<(RegulatoryReturn RegulatoryReturn, OrassSubmissionEvidenceDto Evidence)>> GetPendingAcknowledgementItemsAsync(OrassProfileDto profile)
    {
        if (string.IsNullOrWhiteSpace(profile.SourceReportCode))
        {
            return [];
        }

        var submittedItems = await _context.RegulatoryReturns
            .Where(ret =>
                ret.ReturnType == profile.SourceReportCode &&
                ret.SubmissionDate != null &&
                (ret.SubmissionStatus == "Submitted" || ret.SubmissionStatus == "Accepted"))
            .OrderByDescending(ret => ret.SubmissionDate)
            .ToListAsync();

        var evidenceLookup = await LoadEvidenceLookupAsync(submittedItems.Select(item => item.Id).ToArray());
        return submittedItems
            .Where(item =>
                evidenceLookup.TryGetValue(item.Id, out var evidence) &&
                evidence != null &&
                (evidence.AcknowledgementStatus == "PENDING" || evidence.AcknowledgementStatus == "RECEIVED"))
            .Select(item => (item, evidenceLookup[item.Id]))
            .ToList();
    }

    private async Task<OrassSubmissionHistoryItemDto?> TryReconcileItemAsync(
        OrassProfileDto profile,
        RegulatoryReturn regulatoryReturn,
        OrassSubmissionEvidenceDto evidence,
        string executedBy)
    {
        if (profile.SubmissionMode.Equals("TEST", StringComparison.OrdinalIgnoreCase))
        {
            evidence.AcknowledgementStatus = "ACCEPTED";
            evidence.AcknowledgementReference ??= $"TEST-ACK-{regulatoryReturn.Id}-{DateTime.UtcNow:HHmmss}";
            evidence.AcknowledgedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            evidence.TransportMessage = "Automatically acknowledged in TEST mode during reconciliation.";
            evidence.Notes = evidence.Notes
                .Concat([$"Test-mode reconciliation marked the submission as ACCEPTED at {DateTime.UtcNow:O}."])
                .ToArray();
            regulatoryReturn.SubmissionStatus = "Accepted";
            regulatoryReturn.UpdatedAt = DateTime.UtcNow;

            await SaveEvidenceAsync(regulatoryReturn.Id, evidence);
            await _context.SaveChangesAsync();

            return MapHistoryItem(regulatoryReturn, evidence);
        }

        var acknowledgementEndpoint = BuildAcknowledgementEndpoint(profile.EndpointUrl, evidence.TransmissionId);
        if (string.IsNullOrWhiteSpace(acknowledgementEndpoint))
        {
            return null;
        }

        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.Add("X-ORASS-INSTITUTION", profile.InstitutionCode);
            client.DefaultRequestHeaders.Add("X-ORASS-TRANSMISSION-ID", evidence.TransmissionId);

            using var response = await client.GetAsync(acknowledgementEndpoint);
            if (!response.IsSuccessStatusCode)
            {
                evidence.ProviderStatusCode = ((int)response.StatusCode).ToString();
                evidence.Notes = evidence.Notes
                    .Concat([$"Acknowledgement poll returned provider status {(int)response.StatusCode} at {DateTime.UtcNow:O}."])
                    .ToArray();
                await SaveEvidenceAsync(regulatoryReturn.Id, evidence);
                return null;
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            var providerStatus = ExtractJsonValue(responseBody, "status");
            var providerReference = ExtractJsonValue(responseBody, "acknowledgementReference")
                ?? ExtractJsonValue(responseBody, "reference");
            var providerMessage = ExtractJsonValue(responseBody, "message")
                ?? responseBody[..Math.Min(responseBody.Length, 500)];
            var normalizedStatus = NormalizeAcknowledgementStatus(providerStatus);

            if (normalizedStatus == "RECEIVED" && string.IsNullOrWhiteSpace(providerReference))
            {
                evidence.TransportMessage = providerMessage;
                evidence.Notes = evidence.Notes
                    .Concat([$"Acknowledgement poll responded without a terminal status at {DateTime.UtcNow:O}."])
                    .ToArray();
                await SaveEvidenceAsync(regulatoryReturn.Id, evidence);
                return null;
            }

            evidence.AcknowledgementStatus = normalizedStatus;
            evidence.AcknowledgementReference = providerReference ?? evidence.AcknowledgementReference;
            evidence.AcknowledgedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            evidence.TransportMessage = providerMessage;
            evidence.Notes = evidence.Notes
                .Concat([$"Acknowledgement poll reconciled the submission as {normalizedStatus} at {DateTime.UtcNow:O}."])
                .ToArray();

            if (normalizedStatus == "ACCEPTED")
            {
                regulatoryReturn.SubmissionStatus = "Accepted";
            }
            else if (normalizedStatus == "REJECTED")
            {
                regulatoryReturn.SubmissionStatus = "Rejected";
                regulatoryReturn.ValidationErrors = MergeValidationNote(
                    regulatoryReturn.ValidationErrors,
                    providerMessage ?? "ORASS acknowledgement rejected the submission.");
            }

            regulatoryReturn.UpdatedAt = DateTime.UtcNow;
            await SaveEvidenceAsync(regulatoryReturn.Id, evidence);
            await _context.SaveChangesAsync();

            await _auditLoggingService.LogActionAsync(
                "ORASS_ACKNOWLEDGEMENT_RECONCILED",
                "RegulatoryReturn",
                regulatoryReturn.Id.ToString(),
                executedBy,
                $"Acknowledgement reconciled to {normalizedStatus}.",
                status: "SUCCESS",
                newValues: new
                {
                    evidence.AcknowledgementStatus,
                    evidence.AcknowledgementReference,
                    evidence.AcknowledgedAt,
                    acknowledgementEndpoint
                });

            return MapHistoryItem(regulatoryReturn, evidence);
        }
        catch (Exception ex)
        {
            evidence.Notes = evidence.Notes
                .Concat([$"Acknowledgement reconciliation exception at {DateTime.UtcNow:O}: {ex.Message}"])
                .ToArray();
            evidence.TransportMessage = ex.Message;
            await SaveEvidenceAsync(regulatoryReturn.Id, evidence);
            _logger.LogWarning(ex, "Failed to reconcile ORASS acknowledgement for return {ReturnId}", regulatoryReturn.Id);
            return null;
        }
    }

    private static string NormalizeAcknowledgementStatus(string? status)
    {
        return (status ?? "RECEIVED").Trim().ToUpperInvariant() switch
        {
            "ACCEPTED" => "ACCEPTED",
            "REJECTED" => "REJECTED",
            _ => "RECEIVED"
        };
    }

    private static string MergeValidationNote(string? existingValue, string note)
    {
        var safeNote = note.Replace("\"", "'");
        if (string.IsNullOrWhiteSpace(existingValue) || existingValue == "[]")
        {
            return $"[\"{safeNote}\"]";
        }

        return existingValue.TrimEnd(']') + $",\"{safeNote}\"]";
    }

    private static string ComputeSha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }

    private static string? BuildAcknowledgementEndpoint(string endpointUrl, string transmissionId)
    {
        if (string.IsNullOrWhiteSpace(endpointUrl) || string.IsNullOrWhiteSpace(transmissionId))
        {
            return null;
        }

        return $"{endpointUrl.TrimEnd('/')}/acknowledgements/{Uri.EscapeDataString(transmissionId)}";
    }

    private static string? ExtractJsonValue(string json, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.TryGetProperty(propertyName, out var property))
            {
                return property.ValueKind == JsonValueKind.String
                    ? property.GetString()
                    : property.GetRawText();
            }
        }
        catch
        {
            return null;
        }

        return null;
    }
}
