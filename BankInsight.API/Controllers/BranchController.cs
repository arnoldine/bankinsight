using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BankInsight.API.Entities;
using BankInsight.API.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BranchController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public BranchController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET: api/branch
    [HttpGet]
    public async Task<ActionResult<List<Branch>>> GetBranches()
    {
        return await _db.Branches.ToListAsync();
    }

    // GET: api/branch/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Branch>> GetBranch(string id)
    {
        var branch = await _db.Branches.FindAsync(id);
        if (branch == null) return NotFound();
        return branch;
    }

    // POST: api/branch
    [HttpPost]
    public async Task<ActionResult<Branch>> CreateBranch([FromBody] Branch branch)
    {
        if (string.IsNullOrWhiteSpace(branch.Id))
            branch.Id = Guid.NewGuid().ToString();
        _db.Branches.Add(branch);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetBranch), new { id = branch.Id }, branch);
    }

    // PUT: api/branch/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBranch(string id, [FromBody] Branch branch)
    {
        if (id != branch.Id) return BadRequest();
        _db.Entry(branch).State = EntityState.Modified;
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_db.Branches.Any(e => e.Id == id))
                return NotFound();
            throw;
        }
        return NoContent();
    }

    // DELETE: api/branch/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBranch(string id)
    {
        var branch = await _db.Branches.FindAsync(id);
        if (branch == null) return NotFound();
        _db.Branches.Remove(branch);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
