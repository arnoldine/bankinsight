using HybridTransfer.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace HybridTransfer.Api.Controllers;

[ApiController]
[Route("api/v1/conversions")]
public sealed class ConversionsController : ControllerBase
{
    [HttpPost("quotes")]
    [ProducesResponseType(typeof(ConversionQuoteResponse), StatusCodes.Status200OK)]
    public ActionResult<ConversionQuoteResponse> Quote([FromBody] ConversionQuoteRequest request)
    {
        if (request.Amount <= 0)
        {
            return ValidationProblem("Amount must be positive.");
        }

        var response = new ConversionQuoteResponse(Guid.NewGuid(), 12.45m, 0.15m, 2.5m, DateTimeOffset.UtcNow.AddMinutes(2));
        return Ok(response);
    }
}
