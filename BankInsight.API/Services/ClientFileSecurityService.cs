using BankInsight.API.Data;
using BankInsight.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Services;

public interface IClientFileSecurityService
{
    Task<int> ProcessPendingScansAsync(CancellationToken cancellationToken = default);
}

public sealed class ClientFileSecurityService : IClientFileSecurityService
{
    private readonly ApplicationDbContext _context;
    private readonly IClientFileStorageService _clientFileStorageService;
    private readonly IAuditLoggingService _auditLoggingService;

    public ClientFileSecurityService(
        ApplicationDbContext context,
        IClientFileStorageService clientFileStorageService,
        IAuditLoggingService auditLoggingService)
    {
        _context = context;
        _clientFileStorageService = clientFileStorageService;
        _auditLoggingService = auditLoggingService;
    }

    public async Task<int> ProcessPendingScansAsync(CancellationToken cancellationToken = default)
    {
        var processed = 0;

        var pendingComplaintAttachments = await _context.Set<ClientComplaintAttachment>()
            .Where(item => item.Status == "PENDING_SCAN")
            .OrderBy(item => item.UploadedAt)
            .Take(25)
            .ToListAsync(cancellationToken);

        foreach (var attachment in pendingComplaintAttachments)
        {
            var verdict = await ScanStoredFileAsync(attachment.DataUrl, attachment.FileName, attachment.ContentType, cancellationToken);
            attachment.Status = verdict.IsAllowed ? "CLEAN" : "REJECTED";
            processed += 1;

            await _auditLoggingService.LogActionAsync(
                verdict.IsAllowed ? "CLIENT_ATTACHMENT_SCAN_CLEAN" : "CLIENT_ATTACHMENT_SCAN_REJECTED",
                "CLIENT_COMPLAINT_ATTACHMENT",
                attachment.ComplaintId,
                attachment.Id,
                verdict.Message,
                null,
                null,
                verdict.IsAllowed ? "SUCCESS" : "FAILED");
        }

        var pendingCustomerMedia = await _context.CustomerMediaAssets
            .Where(item => item.Status == "PENDING" || item.Status == "PENDING_SCAN")
            .OrderBy(item => item.UploadedAt)
            .Take(25)
            .ToListAsync(cancellationToken);

        foreach (var media in pendingCustomerMedia)
        {
            var verdict = await ScanStoredFileAsync(media.DataUrl, media.FileName, media.ContentType, cancellationToken);
            media.Status = verdict.IsAllowed ? "PENDING_REVIEW" : "REJECTED";
            processed += 1;

            await _auditLoggingService.LogActionAsync(
                verdict.IsAllowed ? "CUSTOMER_MEDIA_SCAN_READY" : "CUSTOMER_MEDIA_SCAN_REJECTED",
                "CUSTOMER_MEDIA",
                media.CustomerId,
                media.Id,
                verdict.Message,
                null,
                null,
                verdict.IsAllowed ? "SUCCESS" : "FAILED");
        }

        if (processed > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return processed;
    }

    private async Task<FileScanVerdict> ScanStoredFileAsync(
        string storageReference,
        string fileName,
        string contentType,
        CancellationToken cancellationToken)
    {
        var stored = await _clientFileStorageService.ReadAsync(storageReference, fileName, contentType, cancellationToken);
        if (stored == null)
        {
            return new FileScanVerdict(false, "Stored file content could not be retrieved for scanning.");
        }

        var bytes = stored.Bytes;
        if (bytes.Length == 0)
        {
            return new FileScanVerdict(false, "Stored file is empty.");
        }

        var normalizedType = contentType.Trim().ToLowerInvariant();
        var signatureMatches = normalizedType switch
        {
            "image/png" => IsPng(bytes),
            "image/jpeg" => IsJpeg(bytes),
            "image/jpg" => IsJpeg(bytes),
            "image/webp" => IsWebp(bytes),
            "application/pdf" => IsPdf(bytes),
            _ => false
        };

        if (!signatureMatches)
        {
            return new FileScanVerdict(false, $"Stored file signature does not match declared content type {normalizedType}.");
        }

        return new FileScanVerdict(true, $"Stored file passed signature validation for {normalizedType} and is ready for the next review stage.");
    }

    private static bool IsPng(byte[] bytes) =>
        bytes.Length >= 8 &&
        bytes[0] == 0x89 &&
        bytes[1] == 0x50 &&
        bytes[2] == 0x4E &&
        bytes[3] == 0x47 &&
        bytes[4] == 0x0D &&
        bytes[5] == 0x0A &&
        bytes[6] == 0x1A &&
        bytes[7] == 0x0A;

    private static bool IsJpeg(byte[] bytes) =>
        bytes.Length >= 3 &&
        bytes[0] == 0xFF &&
        bytes[1] == 0xD8 &&
        bytes[2] == 0xFF;

    private static bool IsWebp(byte[] bytes) =>
        bytes.Length >= 12 &&
        bytes[0] == 0x52 &&
        bytes[1] == 0x49 &&
        bytes[2] == 0x46 &&
        bytes[3] == 0x46 &&
        bytes[8] == 0x57 &&
        bytes[9] == 0x45 &&
        bytes[10] == 0x42 &&
        bytes[11] == 0x50;

    private static bool IsPdf(byte[] bytes) =>
        bytes.Length >= 4 &&
        bytes[0] == 0x25 &&
        bytes[1] == 0x50 &&
        bytes[2] == 0x44 &&
        bytes[3] == 0x46;

    private sealed record FileScanVerdict(bool IsAllowed, string Message);
}
