using System.Threading.Tasks;
using BankInsight.API.DTOs;
using BankInsight.API.Infrastructure;
using BankInsight.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankInsight.API.Controllers;

[Authorize]
[ApiController]
[Route("api/gl")]
public class GlController : ControllerBase
{
    private readonly GlService _glService;

    public GlController(GlService glService)
    {
        _glService = glService;
    }

    [HttpGet("accounts")]
    [RequirePermission("VIEW_GL")]
    public async Task<IActionResult> GetGlAccounts()
    {
        var accounts = await _glService.GetGlAccountsAsync();
        return Ok(accounts);
    }

    [HttpPost("accounts")]
    [RequirePermission("MANAGE_GL")]
    public async Task<IActionResult> CreateGlAccount([FromBody] CreateGlAccountRequest request)
    {
        var account = await _glService.CreateGlAccountAsync(request);
        return StatusCode(201, account);
    }

    [HttpPost("accounts/seed-regulatory")]
    [RequirePermission("MANAGE_GL")]
    public async Task<IActionResult> SeedRegulatoryChart([FromBody] SeedChartOfAccountsRequest? request)
    {
        var result = await _glService.SeedRegulatoryChartOfAccountsAsync(request?.RegionCode ?? RegulatoryChartOfAccountsCatalog.GhanaRegionCode);
        return Ok(result);
    }

    [HttpGet("journal-entries")]
    [RequirePermission("VIEW_GL")]
    public async Task<IActionResult> GetJournalEntries()
    {
        var entries = await _glService.GetJournalEntriesAsync();
        return Ok(entries);
    }

    [HttpPost("journal-entries")]
    [RequirePermission("POST_JOURNAL")]
    public async Task<IActionResult> PostJournalEntry([FromBody] PostJournalEntryRequest request)
    {
        var entry = await _glService.PostJournalEntryAsync(request);
        return StatusCode(201, entry);
    }
}
