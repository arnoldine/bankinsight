using HybridTransfer.Application.DTOs;
using HybridTransfer.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace HybridTransfer.Api.Controllers;

[ApiController]
[Route("api/v1/ledger")]
public sealed class LedgerController : ControllerBase
{
    private readonly LedgerApplicationService _ledgerApplicationService;

    public LedgerController(LedgerApplicationService ledgerApplicationService)
    {
        _ledgerApplicationService = ledgerApplicationService;
    }

    [HttpPost("internal-transfer-postings")]
    [ProducesResponseType(typeof(LedgerPostingResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<LedgerPostingResponse>> PostInternalTransfer(
        [FromBody] LedgerInternalTransferRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey) || request.Amount <= 0)
        {
            return ValidationProblem("Idempotency key and positive amount are required.");
        }

        var journalEntryId = await _ledgerApplicationService.PostInternalTransferAsync(
            request.SourceWalletId,
            request.DestinationWalletId,
            request.SourceLiabilityAccountId,
            request.DestinationLiabilityAccountId,
            request.Amount,
            request.Currency,
            request.Reference,
            idempotencyKey,
            cancellationToken);

        var response = new LedgerPostingResponse(journalEntryId, "Posted", request.SourceWalletId, request.DestinationWalletId, request.Amount, request.Currency);
        return Created($"/api/v1/ledger/journal-entries/{journalEntryId}", response);
    }

    [HttpPost("payout-settlements")]
    [ProducesResponseType(typeof(LedgerSimplePostingResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<LedgerSimplePostingResponse>> PostPayoutSettlement(
        [FromBody] LedgerSettlementRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey) || request.Amount <= 0)
        {
            return ValidationProblem("Idempotency key and positive amount are required.");
        }

        var journalEntryId = await _ledgerApplicationService.PostPayoutSettlementAsync(
            request.TransferOrderId,
            request.PendingPayoutLiabilityAccountId,
            request.SettlementAssetAccountId,
            request.Amount,
            request.Currency,
            request.Reference,
            idempotencyKey,
            cancellationToken);

        return Created($"/api/v1/ledger/journal-entries/{journalEntryId}", new LedgerSimplePostingResponse(journalEntryId, "Posted", request.Amount, request.Currency, request.Reference, request.TransferOrderId));
    }

    [HttpPost("payout-reversals")]
    [ProducesResponseType(typeof(LedgerSimplePostingResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<LedgerSimplePostingResponse>> PostPayoutReversal(
        [FromBody] LedgerReversalRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey) || request.Amount <= 0)
        {
            return ValidationProblem("Idempotency key and positive amount are required.");
        }

        var journalEntryId = await _ledgerApplicationService.PostPayoutReversalAsync(
            request.TransferOrderId,
            request.CustomerWalletId,
            request.PendingPayoutLiabilityAccountId,
            request.CustomerLiabilityAccountId,
            request.Amount,
            request.Currency,
            request.Reference,
            idempotencyKey,
            request.ReversedJournalEntryId,
            cancellationToken);

        return Created($"/api/v1/ledger/journal-entries/{journalEntryId}", new LedgerSimplePostingResponse(journalEntryId, "Posted", request.Amount, request.Currency, request.Reference, request.TransferOrderId));
    }
}
