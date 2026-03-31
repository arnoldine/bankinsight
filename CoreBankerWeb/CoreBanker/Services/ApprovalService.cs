using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoreBanker.Services
{
    public class ApprovalService : ApiClientBase
    {
        public ApprovalService(HttpClient httpClient, CoreBanker.State.AppState appState) : base(httpClient, appState) { }

        public async Task<List<ApprovalDto>> GetApprovalsAsync(CancellationToken cancellationToken = default)
        {
            var result = await GetAsync<List<ApprovalApiModel>>("/api/approvals", cancellationToken);
            return (result ?? new List<ApprovalApiModel>()).ConvertAll(MapApproval);
        }

        public async Task<ApprovalDto?> UpdateApprovalAsync(string id, UpdateApprovalRequest request, CancellationToken cancellationToken = default)
        {
            var updated = await PutAsync<UpdateApprovalRequest, ApprovalApiModel>($"/api/approvals/{Uri.EscapeDataString(id)}", request, cancellationToken);
            return updated is null ? null : MapApproval(updated);
        }

        private static ApprovalDto MapApproval(ApprovalApiModel approval)
        {
            return new ApprovalDto
            {
                Id = approval.Id ?? string.Empty,
                Type = approval.EntityType ?? approval.Type ?? string.Empty,
                Reference = approval.ReferenceNo ?? approval.Reference ?? approval.EntityId ?? string.Empty,
                Status = NormalizeStatus(approval.Status),
                RequestedBy = approval.RequestedBy ?? approval.RequesterId ?? string.Empty,
                EntityId = approval.EntityId ?? string.Empty,
                WorkflowName = approval.WorkflowName ?? string.Empty,
                Remarks = approval.Remarks ?? string.Empty,
                CreatedAt = approval.CreatedAt,
                UpdatedAt = approval.UpdatedAt,
                LoanDetails = approval.LoanDetails is null ? null : new LoanApprovalDetailsDto
                {
                    LoanId = approval.LoanDetails.LoanId ?? string.Empty,
                    CustomerId = approval.LoanDetails.CustomerId ?? string.Empty,
                    CustomerName = approval.LoanDetails.CustomerName ?? string.Empty,
                    ProductCode = approval.LoanDetails.ProductCode ?? string.Empty,
                    ProductName = approval.LoanDetails.ProductName ?? string.Empty,
                    Principal = approval.LoanDetails.Principal ?? 0m,
                    OutstandingBalance = approval.LoanDetails.OutstandingBalance ?? 0m,
                    Rate = approval.LoanDetails.Rate ?? 0m,
                    TermMonths = approval.LoanDetails.TermMonths ?? 0,
                    CollateralType = approval.LoanDetails.CollateralType ?? string.Empty,
                    CollateralValue = approval.LoanDetails.CollateralValue ?? 0m,
                    ParBucket = approval.LoanDetails.ParBucket ?? string.Empty,
                    Status = approval.LoanDetails.Status ?? string.Empty,
                    AppliedAt = approval.LoanDetails.AppliedAt
                }
            };
        }

        private static string NormalizeStatus(string? value)
        {
            var normalized = (value ?? "PENDING").Trim().ToUpperInvariant();
            return normalized switch
            {
                "APPROVED" => "Approved",
                "REJECTED" => "Rejected",
                _ => "Pending"
            };
        }

        private sealed class ApprovalApiModel
        {
            public string? Id { get; set; }
            public string? Type { get; set; }
            public string? Reference { get; set; }
            public string? Status { get; set; }
            public string? RequestedBy { get; set; }
            public string? EntityType { get; set; }
            public string? EntityId { get; set; }
            public string? RequesterId { get; set; }
            public string? WorkflowName { get; set; }
            public string? ReferenceNo { get; set; }
            public string? Remarks { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime UpdatedAt { get; set; }
            public LoanApprovalDetailsApiModel? LoanDetails { get; set; }
        }

        private sealed class LoanApprovalDetailsApiModel
        {
            public string? LoanId { get; set; }
            public string? CustomerId { get; set; }
            public string? CustomerName { get; set; }
            public string? ProductCode { get; set; }
            public string? ProductName { get; set; }
            public decimal? Principal { get; set; }
            public decimal? OutstandingBalance { get; set; }
            public decimal? Rate { get; set; }
            public int? TermMonths { get; set; }
            public string? CollateralType { get; set; }
            public decimal? CollateralValue { get; set; }
            public string? ParBucket { get; set; }
            public string? Status { get; set; }
            public DateTime? AppliedAt { get; set; }
        }
    }

    public class ApprovalDto
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string RequestedBy { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string WorkflowName { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public LoanApprovalDetailsDto? LoanDetails { get; set; }
    }

    public class LoanApprovalDetailsDto
    {
        public string LoanId { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal Principal { get; set; }
        public decimal OutstandingBalance { get; set; }
        public decimal Rate { get; set; }
        public int TermMonths { get; set; }
        public string CollateralType { get; set; } = string.Empty;
        public decimal CollateralValue { get; set; }
        public string ParBucket { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? AppliedAt { get; set; }
    }

    public class UpdateApprovalRequest
    {
        public string Status { get; set; } = string.Empty;
        public int CurrentStep { get; set; }
        public string? Remarks { get; set; }
    }
}
