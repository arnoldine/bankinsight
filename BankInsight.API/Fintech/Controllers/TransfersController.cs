using HybridTransfer.Api.Contracts;
using HybridTransfer.Application.Abstractions;
using HybridTransfer.Application.DTOs;
using HybridTransfer.Application.Services;
using BankInsight.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace HybridTransfer.Api.Controllers;

[ApiController]
[Route("api/v1/transfers")]
public sealed class TransfersController : ControllerBase
{
    private readonly PayoutOrchestrator _payoutOrchestrator;
    private readonly BankTransferLifecycleService _bankTransferLifecycleService;

    public TransfersController(
        PayoutOrchestrator payoutOrchestrator,
        BankTransferLifecycleService bankTransferLifecycleService)
    {
        _payoutOrchestrator = payoutOrchestrator;
        _bankTransferLifecycleService = bankTransferLifecycleService;
    }

    [HttpPost("internal")]
    [ProducesResponseType(typeof(TransferResponse), StatusCodes.Status202Accepted)]
    public ActionResult<TransferResponse> Internal([FromBody] InternalTransferRequest request, [FromHeader(Name = "Idempotency-Key")] string idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey) || request.Amount <= 0)
        {
            return ValidationProblem("Idempotency key and positive amount are required.");
        }

        var response = new TransferResponse(Guid.NewGuid(), "Posted", "Clear", "Clear", null);
        return Accepted(response);
    }

    [HttpPost("mobile-money")]
    [ProducesResponseType(typeof(TransferResponse), StatusCodes.Status202Accepted)]
    public async Task<ActionResult<TransferResponse>> MobileMoney([FromBody] MobileMoneyTransferRequest request, [FromHeader(Name = "Idempotency-Key")] string idempotencyKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return ValidationProblem("Idempotency-Key header is required.");
        }

        var response = await _payoutOrchestrator.CreateMobileMoneyPayoutAsync(request, "customer", idempotencyKey, cancellationToken);
        return Accepted(response);
    }

    [HttpPost("bank/verify-status/{providerReference}")]
    [ProducesResponseType(typeof(TransferStatusSyncResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<TransferStatusSyncResult>> VerifyBankStatus(string providerReference, CancellationToken cancellationToken)
    {
        var response = await _bankTransferLifecycleService.VerifyBankTransferAsync(providerReference, "ops-user", cancellationToken);
        return Ok(response);
    }

    [HttpPost("bank")]
    [ProducesResponseType(typeof(TransferResponse), StatusCodes.Status202Accepted)]
    public async Task<ActionResult<TransferResponse>> Bank([FromBody] BankTransferRequest request, [FromHeader(Name = "Idempotency-Key")] string idempotencyKey, [FromServices] IBankTransferProvider bankTransferProvider, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return ValidationProblem("Idempotency-Key header is required.");
        }

        var response = await _bankTransferLifecycleService.SubmitBankPayoutAsync(request, "customer", idempotencyKey, cancellationToken);
        return Accepted(response);
    }
}
