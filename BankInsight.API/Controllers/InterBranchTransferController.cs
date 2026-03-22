using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BankInsight.API.DTOs;
using BankInsight.API.Services;
using System.Security.Claims;

namespace BankInsight.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InterBranchTransferController : ControllerBase
{
    private readonly IInterBranchTransferService _transferService;

    public InterBranchTransferController(IInterBranchTransferService transferService)
    {
        _transferService = transferService;
    }

    [HttpPost]
    public async Task<ActionResult<InterBranchTransferDto>> InitiateTransfer([FromBody] CreateInterBranchTransferRequest request)
    {
        try
        {
            var staffId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(staffId))
            {
                return Unauthorized();
            }

            var transfer = await _transferService.InitiateTransferAsync(request, staffId);
            return Ok(transfer);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("approve")]
    public async Task<ActionResult<InterBranchTransferDto>> ApproveTransfer([FromBody] ApproveInterBranchTransferRequest request)
    {
        try
        {
            var staffId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(staffId))
            {
                return Unauthorized();
            }

            var transfer = await _transferService.ApproveTransferAsync(request, staffId);
            return Ok(transfer);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("dispatch")]
    public async Task<ActionResult<InterBranchTransferDto>> DispatchTransfer([FromBody] DispatchInterBranchTransferRequest request)
    {
        try
        {
            var staffId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(staffId))
            {
                return Unauthorized();
            }

            var transfer = await _transferService.DispatchTransferAsync(request, staffId);
            return Ok(transfer);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("receive")]
    public async Task<ActionResult<InterBranchTransferDto>> ReceiveTransfer([FromBody] ReceiveInterBranchTransferRequest request)
    {
        try
        {
            var staffId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(staffId))
            {
                return Unauthorized();
            }

            var transfer = await _transferService.ReceiveTransferAsync(request, staffId);
            return Ok(transfer);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{transferId}")]
    public async Task<ActionResult<InterBranchTransferDto>> GetTransfer(string transferId)
    {
        var transfer = await _transferService.GetTransferAsync(transferId);
        
        if (transfer == null)
        {
            return NotFound(new { message = "Transfer not found" });
        }

        return Ok(transfer);
    }

    [HttpGet]
    public async Task<ActionResult<List<InterBranchTransferDto>>> GetTransfers()
    {
        var transfers = await _transferService.GetTransfersAsync();
        return Ok(transfers);
    }

    [HttpGet("branch/{branchId}")]
    public async Task<ActionResult<List<InterBranchTransferDto>>> GetBranchTransfers(string branchId)
    {
        var transfers = await _transferService.GetBranchTransfersAsync(branchId);
        return Ok(transfers);
    }

    [HttpGet("pending")]
    public async Task<ActionResult<List<InterBranchTransferDto>>> GetPendingTransfers()
    {
        var transfers = await _transferService.GetPendingTransfersAsync();
        return Ok(transfers);
    }
}
