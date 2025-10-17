using AttendanceSystem.Data;
using AttendanceSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AttendanceSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CoursesController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public CoursesController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Course>>> GetAsync([FromQuery] string universityName, [FromQuery] string collegeName, [FromQuery] string departmentName)
    {
        var courses = await _dbContext.Courses
            .Include(c => c.Department)!
                .ThenInclude(d => d!.College)!
                    .ThenInclude(c => c!.University)
            .Where(c => c.Department!.College!.University!.Name == universityName &&
                        c.Department.College.Name == collegeName &&
                        c.Department.Name == departmentName)
            .OrderBy(c => c.Name)
            .ToListAsync();
        return Ok(courses);
    }

    [HttpPost("create-by-name")]
    public async Task<ActionResult<Course>> CreateAsync(CreateCourseRequest request)
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

        var exists = await _dbContext.Courses.AnyAsync(c => c.DepartmentId == department.Id && c.Name == request.CourseName);
        if (exists)
        {
            return Conflict("Course already exists");
        }

        var course = new Course { Name = request.CourseName, Department = department };
        _dbContext.Courses.Add(course);
        await _dbContext.SaveChangesAsync();
        return Ok(course);
    }

    [HttpPut("rename")]
    public async Task<IActionResult> RenameAsync(RenameCourseRequest request)
    {
        var course = await _dbContext.Courses
            .Include(c => c.Department)!
                .ThenInclude(d => d!.College)!
                    .ThenInclude(c => c!.University)
            .FirstOrDefaultAsync(c => c.Department!.College!.University!.Name == request.UniversityName &&
                                      c.Department!.College!.Name == request.CollegeName &&
                                      c.Department!.Name == request.DepartmentName &&
                                      c.Name == request.CurrentName);
        if (course == null)
        {
            return NotFound();
        }

        course.Name = request.NewName;
        await _dbContext.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("by-name")]
    public async Task<IActionResult> DeleteAsync([FromQuery] string universityName, [FromQuery] string collegeName, [FromQuery] string departmentName, [FromQuery] string courseName)
    {
        var course = await _dbContext.Courses
            .Include(c => c.Department)!
                .ThenInclude(d => d!.College)!
                    .ThenInclude(c => c!.University)
            .FirstOrDefaultAsync(c => c.Department!.College!.University!.Name == universityName &&
                                      c.Department!.College!.Name == collegeName &&
                                      c.Department!.Name == departmentName &&
                                      c.Name == courseName);
        if (course == null)
        {
            return NotFound();
        }

        _dbContext.Courses.Remove(course);
        await _dbContext.SaveChangesAsync();
        return NoContent();
    }
}

public record CreateCourseRequest(string UniversityName, string CollegeName, string DepartmentName, string CourseName);
public record RenameCourseRequest(string UniversityName, string CollegeName, string DepartmentName, string CurrentName, string NewName);
