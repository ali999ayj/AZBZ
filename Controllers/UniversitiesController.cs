using AttendanceSystem.Data;
using AttendanceSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AttendanceSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UniversitiesController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public UniversitiesController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<University>>> GetAllAsync()
    {
        var items = await _dbContext.Universities.OrderBy(u => u.Name).ToListAsync();
        return Ok(items);
    }

    [HttpPost("create-by-name")]
    public async Task<ActionResult<University>> CreateAsync([FromBody] string universityName)
    {
        if (string.IsNullOrWhiteSpace(universityName))
        {
            return BadRequest("University name is required");
        }

        var existing = await _dbContext.Universities.FirstOrDefaultAsync(u => u.Name == universityName);
        if (existing != null)
        {
            return Conflict("University already exists");
        }

        var university = new University { Name = universityName };
        _dbContext.Universities.Add(university);
        await _dbContext.SaveChangesAsync();
        return Ok(university);
    }

    [HttpPut("rename")]
    public async Task<IActionResult> RenameAsync(RenameRequest request)
    {
        var entity = await _dbContext.Universities.FirstOrDefaultAsync(u => u.Name == request.CurrentName);
        if (entity == null)
        {
            return NotFound();
        }

        entity.Name = request.NewName;
        await _dbContext.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("by-name/{name}")]
    public async Task<IActionResult> DeleteAsync(string name)
    {
        var entity = await _dbContext.Universities.FirstOrDefaultAsync(u => u.Name == name);
        if (entity == null)
        {
            return NotFound();
        }

        _dbContext.Universities.Remove(entity);
        await _dbContext.SaveChangesAsync();
        return NoContent();
    }
}

public record RenameRequest(string CurrentName, string NewName);
