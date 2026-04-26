using System.Security.Claims;
using BankInsight.API.Data;
using BankInsight.API.Entities;
using BankInsight.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Controllers;

[ApiController]
[Route("api/client-files")]
[Authorize]
public class ClientFileController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IClientFileStorageService _clientFileStorageService;

    public ClientFileController(ApplicationDbContext context, IClientFileStorageService clientFileStorageService)
    {
        _context = context;
        _clientFileStorageService = clientFileStorageService;
    }

    [HttpGet("customer-media/{mediaId}")]
    public async Task<IActionResult> GetCustomerMediaContent(string mediaId, CancellationToken cancellationToken)
    {
        var asset = await _context.CustomerMediaAssets
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == mediaId, cancellationToken);
        if (asset == null)
        {
            return NotFound(new { message = "Media file not found." });
        }

        if (!await CanAccessCustomerRecordAsync(asset.CustomerId, cancellationToken))
        {
            return Forbid();
        }

        var content = await _clientFileStorageService.ReadAsync(asset.DataUrl, asset.FileName, asset.ContentType, cancellationToken);
        return content == null
            ? NotFound(new { message = "Media content is not available." })
            : File(content.Bytes, content.ContentType, asset.FileName);
    }

    [HttpGet("complaint-attachments/{attachmentId}")]
    public async Task<IActionResult> GetComplaintAttachmentContent(string attachmentId, CancellationToken cancellationToken)
    {
        var attachment = await _context.Set<ClientComplaintAttachment>()
            .AsNoTracking()
            .Include(item => item.Complaint)
            .FirstOrDefaultAsync(item => item.Id == attachmentId, cancellationToken);
        if (attachment?.Complaint == null)
        {
            return NotFound(new { message = "Complaint attachment not found." });
        }

        if (!await CanAccessCustomerRecordAsync(attachment.Complaint.CustomerId, cancellationToken))
        {
            return Forbid();
        }

        var content = await _clientFileStorageService.ReadAsync(attachment.DataUrl, attachment.FileName, attachment.ContentType, cancellationToken);
        return content == null
            ? NotFound(new { message = "Attachment content is not available." })
            : File(content.Bytes, content.ContentType, attachment.FileName);
    }

    private async Task<bool> CanAccessCustomerRecordAsync(string customerId, CancellationToken cancellationToken)
    {
        var actorType = User.FindFirst("actor_type")?.Value;
        if (string.Equals(actorType, "customer", StringComparison.OrdinalIgnoreCase))
        {
            var linkedCustomerId = User.FindFirst("customer_id")?.Value;
            return string.Equals(linkedCustomerId, customerId, StringComparison.Ordinal);
        }

        var hasCustomerPermission = User.FindAll("permissions")
            .Select(claim => claim.Value)
            .Any(permission =>
                string.Equals(permission, "customers.view", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(permission, "customers.edit", StringComparison.OrdinalIgnoreCase));

        if (!hasCustomerPermission)
        {
            return false;
        }

        var branchId = User.FindFirst("branch_id")?.Value;
        var scopeType = User.FindFirst("access_scope_type")?.Value;
        if (string.Equals(scopeType, "BranchOnly", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(branchId))
        {
            return await _context.Customers
                .AsNoTracking()
                .AnyAsync(customer => customer.Id == customerId && customer.BranchId == branchId, cancellationToken);
        }

        return true;
    }
}
