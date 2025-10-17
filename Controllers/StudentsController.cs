using AttendanceSystem.Data;
using AttendanceSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AttendanceSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StudentsController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public StudentsController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Student>>> GetAsync([FromQuery] string universityName, [FromQuery] string collegeName, [FromQuery] string departmentName, [FromQuery] string? stageName, [FromQuery] string groupName, [FromQuery] GroupKind kind)
    {
        var query = _dbContext.Students
            .Include(s => s.Group)!
                .ThenInclude(g => g!.Stage)
            .Include(s => s.Group)!
                .ThenInclude(g => g!.Department)!
                    .ThenInclude(d => d!.College)!
                        .ThenInclude(c => c!.University)
            .Where(s => s.Group!.Department!.College!.University!.Name == universityName &&
                        s.Group.Department.College.Name == collegeName &&
                        s.Group.Department.Name == departmentName &&
                        s.Group.Name == groupName &&
                        s.Group.Kind == kind);

        if (!string.IsNullOrWhiteSpace(stageName))
        {
            query = query.Where(s => s.Group!.Stage != null && s.Group.Stage.Name == stageName);
        }
        else
        {
            query = query.Where(s => s.Group!.Stage == null);
        }

        var students = await query.OrderBy(s => s.LastName).ThenBy(s => s.FirstName).ToListAsync();
        return Ok(students);
    }

    [HttpPost("create")]
    public async Task<ActionResult<Student>> CreateAsync(CreateStudentRequest request)
    {
        var group = await _dbContext.Groups
            .Include(g => g.Department)!
                .ThenInclude(d => d!.College)!
                    .ThenInclude(c => c!.University)
            .Include(g => g.Stage)
            .FirstOrDefaultAsync(g => g.Department!.College!.University!.Name == request.UniversityName &&
                                     g.Department.College.Name == request.CollegeName &&
                                     g.Department.Name == request.DepartmentName &&
                                     g.Name == request.GroupName &&
                                     g.Kind == request.Kind &&
                                     ((string.IsNullOrWhiteSpace(request.StageName) && g.Stage == null) ||
                                      (!string.IsNullOrWhiteSpace(request.StageName) && g.Stage != null && g.Stage.Name == request.StageName)));
        if (group == null)
        {
            return NotFound("Group not found");
        }

        var exists = await _dbContext.Students.AnyAsync(s => s.UniversityId == request.UniversityId);
        if (exists)
        {
            return Conflict("Student already exists");
        }

        var student = new Student
        {
            UniversityId = request.UniversityId,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Group = group,
            IsActive = request.IsActive
        };

        _dbContext.Students.Add(student);
        await _dbContext.SaveChangesAsync();
        return Ok(student);
    }

    [HttpPut("update")]
    public async Task<IActionResult> UpdateAsync(UpdateStudentRequest request)
    {
        var student = await _dbContext.Students.FirstOrDefaultAsync(s => s.UniversityId == request.UniversityId);
        if (student == null)
        {
            return NotFound();
        }

        student.FirstName = request.FirstName;
        student.LastName = request.LastName;
        student.IsActive = request.IsActive;
        await _dbContext.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("by-university-id/{universityId}")]
    public async Task<IActionResult> DeleteAsync(string universityId)
    {
        var student = await _dbContext.Students.FirstOrDefaultAsync(s => s.UniversityId == universityId);
        if (student == null)
        {
            return NotFound();
        }

        _dbContext.Students.Remove(student);
        await _dbContext.SaveChangesAsync();
        return NoContent();
    }
}

public record CreateStudentRequest(string UniversityId, string FirstName, string LastName, bool IsActive, string UniversityName, string CollegeName, string DepartmentName, string? StageName, string GroupName, GroupKind Kind);
public record UpdateStudentRequest(string UniversityId, string FirstName, string LastName, bool IsActive);
