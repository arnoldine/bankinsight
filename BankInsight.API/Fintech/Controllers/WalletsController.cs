using HybridTransfer.Application.Abstractions;
using HybridTransfer.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace HybridTransfer.Api.Controllers;

[ApiController]
[Route("api/v1/wallets")]
public sealed class WalletsController : ControllerBase
{
    private readonly IWalletProjectionRepository _walletProjectionRepository;

    public WalletsController(IWalletProjectionRepository walletProjectionRepository)
    {
        _walletProjectionRepository = walletProjectionRepository;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<WalletSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<WalletSummaryResponse>>> GetWallets(CancellationToken cancellationToken)
    {
        var walletIds = new[]
        {
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("33333333-3333-3333-3333-333333333333")
        };

        var wallets = new List<WalletSummaryResponse>();
        foreach (var walletId in walletIds)
        {
            var projection = await _walletProjectionRepository.GetProjectionAsync(walletId, cancellationToken);
            if (projection is not null)
            {
                wallets.Add(new WalletSummaryResponse(projection.WalletId, projection.Currency, projection.AvailableBalance, projection.ReservedBalance, projection.Status));
            }
        }

        return Ok(wallets);
    }
}
