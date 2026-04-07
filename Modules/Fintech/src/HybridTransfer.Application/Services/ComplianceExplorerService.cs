using System.Text.Json;
using HybridTransfer.Application.Abstractions;
using HybridTransfer.Application.DTOs;

namespace HybridTransfer.Application.Services;

public sealed class ComplianceExplorerService
{
    private readonly IApprovalRequestRepository _approvalRequestRepository;
    private readonly IAlertRepository _alertRepository;

    public ComplianceExplorerService(IApprovalRequestRepository approvalRequestRepository, IAlertRepository alertRepository)
    {
        _approvalRequestRepository = approvalRequestRepository;
        _alertRepository = alertRepository;
    }

    public async Task<PagedResponse<ApprovalExplorerItemResponse>> SearchApprovalsAsync(ApprovalExplorerQueryRequest request, CancellationToken cancellationToken)
    {
        var criteria = new ApprovalSearchCriteria(request.Status, request.ActionCode, request.TransferOrderId, request.RequestedBy, request.CreatedFromUtc, request.CreatedToUtc, NormalizePage(request.Page), NormalizePageSize(request.PageSize));
        var result = await _approvalRequestRepository.SearchAsync(criteria, cancellationToken);
        var items = result.Items
            .Select(x => new ApprovalExplorerItemResponse(x.Id, x.TransferOrderId, x.ActionCode, x.Status, x.RequestedBy, x.ApprovedBy, x.Reason, x.CreatedAtUtc))
            .ToArray();
        return new PagedResponse<ApprovalExplorerItemResponse>(result.Page, result.PageSize, result.TotalCount, items);
    }

    public async Task<PagedResponse<AlertExplorerItemResponse>> SearchAlertsAsync(AlertExplorerQueryRequest request, CancellationToken cancellationToken)
    {
        var criteria = new AlertSearchCriteria(request.Status, request.Severity, request.CustomerId, request.AlertCode, request.CreatedFromUtc, request.CreatedToUtc, NormalizePage(request.Page), NormalizePageSize(request.PageSize));
        var result = await _alertRepository.SearchAsync(criteria, cancellationToken);
        var items = result.Items
            .Select(x => new AlertExplorerItemResponse(x.Id, x.CustomerId, x.AlertCode, x.Severity, x.Score, x.Status, x.CreatedAtUtc, SummarizePayload(x.PayloadJson)))
            .ToArray();
        return new PagedResponse<AlertExplorerItemResponse>(result.Page, result.PageSize, result.TotalCount, items);
    }

    private static string SummarizePayload(string payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return "No payload captured.";
        }

        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in root.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.String)
                    {
                        return property.Value.GetString() ?? "Alert payload captured.";
                    }
                }
            }
        }
        catch (JsonException)
        {
        }

        return payloadJson.Length <= 120 ? payloadJson : payloadJson[..120] + "...";
    }

    private static int NormalizePage(int page) => page <= 0 ? 1 : page;

    private static int NormalizePageSize(int pageSize) => pageSize switch
    {
        <= 0 => 25,
        > 100 => 100,
        _ => pageSize
    };
}
