using System.Security.Claims;
using BankInsight.API.DTOs;
using BankInsight.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankInsight.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VaultController : ControllerBase
{
    private readonly IVaultManagementService _vaultService;

    public VaultController(IVaultManagementService vaultService)
    {
        _vaultService = vaultService;
    }

    [HttpGet("{branchId}/{currency}")]
    public async Task<ActionResult<BranchVaultDto>> GetVault(string branchId, string currency)
    {
        var vault = await _vaultService.GetVaultAsync(branchId, currency);
        if (vault == null)
        {
            return NotFound(new { message = "Vault not found" });
        }

        return Ok(vault);
    }

    [HttpGet]
    public async Task<ActionResult<List<BranchVaultDto>>> GetAllVaults()
    {
        var vaults = await _vaultService.GetAllVaultsAsync();
        return Ok(vaults);
    }

    [HttpGet("branch/{branchId}")]
    public async Task<ActionResult<List<BranchVaultDto>>> GetBranchVaults(string branchId)
    {
        var vaults = await _vaultService.GetBranchVaultsAsync(branchId);
        return Ok(vaults);
    }

    [HttpGet("tills")]
    public async Task<ActionResult<List<TellerTillSummaryDto>>> GetTillSummaries([FromQuery] string? branchId, [FromQuery] string currency = "GHS")
    {
        var tills = await _vaultService.GetTillSummariesAsync(branchId, currency);
        return Ok(tills);
    }

    [HttpPost("tills/open")]
    public async Task<ActionResult<TellerTillSummaryDto>> OpenTill([FromBody] OpenTillRequest request)
    {
        try
        {
            var staffId = GetCurrentStaffId();
            if (string.IsNullOrEmpty(staffId))
            {
                return Unauthorized();
            }

            var till = await _vaultService.OpenTillAsync(request, staffId);
            return Ok(till);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("tills/allocate")]
    public async Task<ActionResult<TellerTillSummaryDto>> AllocateTillCash([FromBody] TillCashTransferRequest request)
    {
        try
        {
            var staffId = GetCurrentStaffId();
            if (string.IsNullOrEmpty(staffId))
            {
                return Unauthorized();
            }

            var till = await _vaultService.AllocateTillCashAsync(request, staffId);
            return Ok(till);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("tills/return")]
    public async Task<ActionResult<TellerTillSummaryDto>> ReturnTillCash([FromBody] TillCashTransferRequest request)
    {
        try
        {
            var staffId = GetCurrentStaffId();
            if (string.IsNullOrEmpty(staffId))
            {
                return Unauthorized();
            }

            var till = await _vaultService.ReturnTillCashAsync(request, staffId);
            return Ok(till);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("tills/close")]
    public async Task<ActionResult<TellerTillSummaryDto>> CloseTill([FromBody] CloseTillRequest request)
    {
        try
        {
            var staffId = GetCurrentStaffId();
            if (string.IsNullOrEmpty(staffId))
            {
                return Unauthorized();
            }

            var till = await _vaultService.CloseTillAsync(request, staffId);
            return Ok(till);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("count")]
    public async Task<ActionResult<BranchVaultDto>> RecordVaultCount([FromBody] VaultCountRequest request)
    {
        try
        {
            var staffId = GetCurrentStaffId();
            if (string.IsNullOrEmpty(staffId))
            {
                return Unauthorized();
            }

            var vault = await _vaultService.RecordVaultCountAsync(request, staffId);
            return Ok(vault);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("transaction")]
    public async Task<ActionResult<BranchVaultDto>> ProcessVaultTransaction([FromBody] VaultTransactionRequest request)
    {
        try
        {
            var staffId = GetCurrentStaffId();
            if (string.IsNullOrEmpty(staffId))
            {
                return Unauthorized();
            }

            var vault = await _vaultService.ProcessVaultTransactionAsync(request, staffId);
            return Ok(vault);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private string? GetCurrentStaffId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}
