using AttendanceSystem.Data;
using AttendanceSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AttendanceSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InstructorsController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public InstructorsController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Instructor>>> GetAsync([FromQuery] string universityName, [FromQuery] string collegeName, [FromQuery] string departmentName)
    {
        var instructors = await _dbContext.Instructors
            .Include(i => i.Department)!
                .ThenInclude(d => d!.College)!
                    .ThenInclude(c => c!.University)
            .Where(i => i.Department!.College!.University!.Name == universityName &&
                        i.Department.College.Name == collegeName &&
                        i.Department.Name == departmentName)
            .OrderBy(i => i.FullName)
            .ToListAsync();
        return Ok(instructors);
    }

    [HttpPost("create-by-name")]
    public async Task<ActionResult<Instructor>> CreateAsync(CreateInstructorRequest request)
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

        var exists = await _dbContext.Instructors.AnyAsync(i => i.DepartmentId == department.Id && i.FullName == request.InstructorName);
        if (exists)
        {
            return Conflict("Instructor already exists");
        }

        var instructor = new Instructor { FullName = request.InstructorName, Department = department };
        _dbContext.Instructors.Add(instructor);
        await _dbContext.SaveChangesAsync();
        return Ok(instructor);
    }

    [HttpPut("rename")]
    public async Task<IActionResult> RenameAsync(RenameInstructorRequest request)
    {
        var instructor = await _dbContext.Instructors
            .Include(i => i.Department)!
                .ThenInclude(d => d!.College)!
                    .ThenInclude(c => c!.University)
            .FirstOrDefaultAsync(i => i.Department!.College!.University!.Name == request.UniversityName &&
                                      i.Department!.College!.Name == request.CollegeName &&
                                      i.Department!.Name == request.DepartmentName &&
                                      i.FullName == request.CurrentName);
        if (instructor == null)
        {
            return NotFound();
        }

        instructor.FullName = request.NewName;
        await _dbContext.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("by-name")]
    public async Task<IActionResult> DeleteAsync([FromQuery] string universityName, [FromQuery] string collegeName, [FromQuery] string departmentName, [FromQuery] string instructorName)
    {
        var instructor = await _dbContext.Instructors
            .Include(i => i.Department)!
                .ThenInclude(d => d!.College)!
                    .ThenInclude(c => c!.University)
            .FirstOrDefaultAsync(i => i.Department!.College!.University!.Name == universityName &&
                                      i.Department!.College!.Name == collegeName &&
                                      i.Department!.Name == departmentName &&
                                      i.FullName == instructorName);
        if (instructor == null)
        {
            return NotFound();
        }

        _dbContext.Instructors.Remove(instructor);
        await _dbContext.SaveChangesAsync();
        return NoContent();
    }
}

public record CreateInstructorRequest(string UniversityName, string CollegeName, string DepartmentName, string InstructorName);
public record RenameInstructorRequest(string UniversityName, string CollegeName, string DepartmentName, string CurrentName, string NewName);
