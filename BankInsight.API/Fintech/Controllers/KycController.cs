using HybridTransfer.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace HybridTransfer.Api.Controllers;

[ApiController]
[Route("api/v1/kyc")]
public sealed class KycController : ControllerBase
{
    [HttpPost("submissions")]
    [ProducesResponseType(typeof(KycSubmissionResponse), StatusCodes.Status202Accepted)]
    public ActionResult<KycSubmissionResponse> Submit()
    {
        var response = new KycSubmissionResponse(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "PendingReview", "Tier1", false);
        return Accepted(response);
    }
}
