using BankInsight.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace BankInsight.API.Controllers;

[ApiController]
[Route("api/migration")]
public class DataMigrationController : ControllerBase
{
    private readonly DataMigrationService _migrationService;

    public DataMigrationController(DataMigrationService migrationService)
    {
        _migrationService = migrationService;
    }

    [HttpGet("datasets")]
    public IActionResult GetDatasets()
    {
        return Ok(_migrationService.GetDatasets());
    }

    [HttpPost("import/{dataset}")]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> ImportDataset(string dataset, IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "CSV file is required." });
        }

        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Only CSV files are supported." });
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _migrationService.ImportAsync(dataset, stream);
            return Ok(result);
        }
        catch (NotSupportedException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Import failed: {ex.Message}" });
        }
    }
}
