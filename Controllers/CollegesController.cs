using AttendanceSystem.Data;
using AttendanceSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AttendanceSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CollegesController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public CollegesController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<College>>> GetAsync([FromQuery] string? universityName)
    {
        var query = _dbContext.Colleges.Include(c => c.University).AsQueryable();
        if (!string.IsNullOrWhiteSpace(universityName))
        {
            query = query.Where(c => c.University!.Name == universityName);
        }

        var items = await query.OrderBy(c => c.Name).ToListAsync();
        return Ok(items);
    }

    [HttpPost("create-by-name")]
    public async Task<ActionResult<College>> CreateAsync(CreateCollegeRequest request)
    {
        var university = await _dbContext.Universities.FirstOrDefaultAsync(u => u.Name == request.UniversityName);
        if (university == null)
        {
            return NotFound("University not found");
        }

        var exists = await _dbContext.Colleges.AnyAsync(c => c.UniversityId == university.Id && c.Name == request.CollegeName);
        if (exists)
        {
            return Conflict("College already exists");
        }

        var college = new College { Name = request.CollegeName, University = university };
        _dbContext.Colleges.Add(college);
        await _dbContext.SaveChangesAsync();
        return Ok(college);
    }

    [HttpPut("rename")]
    public async Task<IActionResult> RenameAsync(RenameCollegeRequest request)
    {
        var college = await _dbContext.Colleges
            .Include(c => c.University)
            .FirstOrDefaultAsync(c => c.University!.Name == request.UniversityName && c.Name == request.CurrentName);

        if (college == null)
        {
            return NotFound();
        }

        college.Name = request.NewName;
        await _dbContext.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("by-name")]
    public async Task<IActionResult> DeleteAsync([FromQuery] string universityName, [FromQuery] string collegeName)
    {
        var college = await _dbContext.Colleges
            .Include(c => c.University)
            .FirstOrDefaultAsync(c => c.University!.Name == universityName && c.Name == collegeName);
        if (college == null)
        {
            return NotFound();
        }

        _dbContext.Colleges.Remove(college);
        await _dbContext.SaveChangesAsync();
        return NoContent();
    }
}

public record CreateCollegeRequest(string UniversityName, string CollegeName);
public record RenameCollegeRequest(string UniversityName, string CurrentName, string NewName);
