using AttendanceSystem.Data;
using AttendanceSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AttendanceSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StagesController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public StagesController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Stage>>> GetAsync([FromQuery] string universityName, [FromQuery] string collegeName, [FromQuery] string departmentName)
    {
        var stages = await _dbContext.Stages
            .Include(s => s.Department)!
                .ThenInclude(d => d!.College)!
                    .ThenInclude(c => c!.University)
            .Where(s => s.Department!.College!.University!.Name == universityName &&
                        s.Department.College.Name == collegeName &&
                        s.Department.Name == departmentName)
            .OrderBy(s => s.Name)
            .ToListAsync();
        return Ok(stages);
    }

    [HttpPost("create-by-name")]
    public async Task<ActionResult<Stage>> CreateAsync(CreateStageRequest request)
    {
        var department = await _dbContext.Departments
            .Include(d => d.College)!
                .ThenInclude(c => c!.University)
            .FirstOrDefaultAsync(d => d.College!.University!.Name == request.UniversityName &&
                                     d.College!.Name == request.CollegeName &&
                                     d.Name == request.DepartmentName);
        if (department == null)
        {
            return NotFound("Department not found");
        }

        var exists = await _dbContext.Stages.AnyAsync(s => s.DepartmentId == department.Id && s.Name == request.StageName);
        if (exists)
        {
            return Conflict("Stage already exists");
        }

        var stage = new Stage { Name = request.StageName, Department = department };
        _dbContext.Stages.Add(stage);
        await _dbContext.SaveChangesAsync();
        return Ok(stage);
    }

    [HttpPut("rename")]
    public async Task<IActionResult> RenameAsync(RenameStageRequest request)
    {
        var stage = await _dbContext.Stages
            .Include(s => s.Department)!
                .ThenInclude(d => d!.College)!
                    .ThenInclude(c => c!.University)
            .FirstOrDefaultAsync(s => s.Department!.College!.University!.Name == request.UniversityName &&
                                      s.Department!.College!.Name == request.CollegeName &&
                                      s.Department!.Name == request.DepartmentName &&
                                      s.Name == request.CurrentName);
        if (stage == null)
        {
            return NotFound();
        }

        stage.Name = request.NewName;
        await _dbContext.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("by-name")]
    public async Task<IActionResult> DeleteAsync([FromQuery] string universityName, [FromQuery] string collegeName, [FromQuery] string departmentName, [FromQuery] string stageName)
    {
        var stage = await _dbContext.Stages
            .Include(s => s.Department)!
                .ThenInclude(d => d!.College)!
                    .ThenInclude(c => c!.University)
            .FirstOrDefaultAsync(s => s.Department!.College!.University!.Name == universityName &&
                                      s.Department!.College!.Name == collegeName &&
                                      s.Department!.Name == departmentName &&
                                      s.Name == stageName);
        if (stage == null)
        {
            return NotFound();
        }

        _dbContext.Stages.Remove(stage);
        await _dbContext.SaveChangesAsync();
        return NoContent();
    }
}

public record CreateStageRequest(string UniversityName, string CollegeName, string DepartmentName, string StageName);
public record RenameStageRequest(string UniversityName, string CollegeName, string DepartmentName, string CurrentName, string NewName);
