using HybridTransfer.Application.Abstractions;
using HybridTransfer.Application.DTOs;
using HybridTransfer.Domain.Common;
using HybridTransfer.Domain.Crypto;
using Microsoft.AspNetCore.Mvc;

namespace HybridTransfer.Api.Controllers;

[ApiController]
[Route("api/v1/crypto")]
public sealed class CryptoController : ControllerBase
{
    private readonly ICryptoCustodyProvider _cryptoCustodyProvider;

    public CryptoController(ICryptoCustodyProvider cryptoCustodyProvider)
    {
        _cryptoCustodyProvider = cryptoCustodyProvider;
    }

    [HttpPost("deposits/addresses")]
    [ProducesResponseType(typeof(DepositAddressResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DepositAddressResponse>> CreateDepositAddress([FromBody] DepositAddressRequest request, CancellationToken cancellationToken)
    {
        var result = await _cryptoCustodyProvider.CreateDepositAddressAsync(request.WalletId, request.Asset, request.Network, cancellationToken);
        return Ok(new DepositAddressResponse(result.WalletAddress, result.Asset, result.Network, result.RequiredConfirmations));
    }

    [HttpPost("withdrawals")]
    [ProducesResponseType(typeof(TransferResponse), StatusCodes.Status202Accepted)]
    public ActionResult<TransferResponse> RequestWithdrawal([FromBody] CryptoWithdrawalRequest request, [FromHeader(Name = "Idempotency-Key")] string idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return ValidationProblem("Idempotency-Key header is required.");
        }

        var withdrawal = new CryptoWithdrawal(request.SourceWalletId, request.DestinationAddress, request.Asset, request.Network, request.Amount, idempotencyKey);
        withdrawal.SetRisk(request.Amount >= 500 ? RiskStatus.Hold : RiskStatus.Monitor);

        return Accepted(new TransferResponse(withdrawal.Id, withdrawal.Status.ToString(), withdrawal.RiskStatus.ToString(), "PendingReview", null));
    }
}
