using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoreBanker.Services
{
    public class ApprovalService : ApiClientBase
    {
        public ApprovalService(HttpClient httpClient) : base(httpClient) { }

        // API model for compatibility with backend
        public class ApprovalApiModel
        {
            public string? Id { get; set; }
            public string? Type { get; set; }
            public string? Reference { get; set; }
            public string? Status { get; set; }
            public string? RequestedBy { get; set; }
        }

        private static string NormalizeStatus(string? value)
        {
            var normalized = (value ?? "PENDING").Trim().ToUpperInvariant();
            if (normalized == "APPROVED") return "Approved";
            return "Pending";
        }

        private static ApprovalDto MapApproval(ApprovalApiModel approval)
        {
            return new ApprovalDto
            {
                Id = approval.Id ?? string.Empty,
                Type = approval.Type ?? string.Empty,
                Reference = approval.Reference ?? string.Empty,
                Status = NormalizeStatus(approval.Status),
                RequestedBy = approval.RequestedBy ?? string.Empty
            };
        }

        public async Task<List<ApprovalDto>> GetApprovalsAsync(CancellationToken cancellationToken = default)
        {
            var result = await GetAsync<List<ApprovalApiModel>>("/api/approvals", cancellationToken);
            return (result ?? new List<ApprovalApiModel>()).ConvertAll(MapApproval);
        }
    }

    public class ApprovalDto
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string RequestedBy { get; set; } = string.Empty;
    }
}
