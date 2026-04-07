using HybridTransfer.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace HybridTransfer.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    public ActionResult<AuthResponse> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            return ValidationProblem("Email and idempotency key are required.");
        }

        var response = new AuthResponse(Guid.NewGuid(), "sandbox-access-token", "sandbox-refresh-token", DateTimeOffset.UtcNow.AddHours(1));
        return Created($"/api/v1/customers/{response.CustomerId}", response);
    }
}
