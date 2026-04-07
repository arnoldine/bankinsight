using HybridTransfer.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace HybridTransfer.Api.Controllers;

[ApiController]
[Route("api/v1/customers")]
public sealed class CustomersController : ControllerBase
{
    [HttpGet("me")]
    [ProducesResponseType(typeof(CustomerSummaryResponse), StatusCodes.Status200OK)]
    public ActionResult<CustomerSummaryResponse> Me()
    {
        return Ok(new CustomerSummaryResponse(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "CUST-000001", "Ama Mensah", "Tier2", "Approved", "Medium", "Active"));
    }
}
