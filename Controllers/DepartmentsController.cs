using AttendanceSystem.Data;
using AttendanceSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AttendanceSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DepartmentsController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public DepartmentsController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Department>>> GetAsync([FromQuery] string? universityName, [FromQuery] string? collegeName)
    {
        var query = _dbContext.Departments
            .Include(d => d.College)!
                .ThenInclude(c => c!.University)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(universityName))
        {
            query = query.Where(d => d.College!.University!.Name == universityName);
        }

        if (!string.IsNullOrWhiteSpace(collegeName))
        {
            query = query.Where(d => d.College!.Name == collegeName);
        }

        var items = await query.OrderBy(d => d.Name).ToListAsync();
        return Ok(items);
    }

    [HttpPost("create-by-name")]
    public async Task<ActionResult<Department>> CreateAsync(CreateDepartmentRequest request)
    {
        var department = await FindDepartmentAsync(request.UniversityName, request.CollegeName, request.DepartmentName);
        if (department != null)
        {
            return Conflict("Department already exists");
        }

        var college = await _dbContext.Colleges
            .Include(c => c.University)
            .FirstOrDefaultAsync(c => c.University!.Name == request.UniversityName && c.Name == request.CollegeName);
        if (college == null)
        {
            return NotFound("College not found");
        }

        department = new Department { Name = request.DepartmentName, College = college };
        _dbContext.Departments.Add(department);
        await _dbContext.SaveChangesAsync();
        return Ok(department);
    }

    [HttpPut("rename")]
    public async Task<IActionResult> RenameAsync(RenameDepartmentRequest request)
    {
        var department = await FindDepartmentAsync(request.UniversityName, request.CollegeName, request.CurrentName);
        if (department == null)
        {
            return NotFound();
        }

        department.Name = request.NewName;
        await _dbContext.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("by-name")]
    public async Task<IActionResult> DeleteAsync([FromQuery] string universityName, [FromQuery] string collegeName, [FromQuery] string departmentName)
    {
        var department = await FindDepartmentAsync(universityName, collegeName, departmentName);
        if (department == null)
        {
            return NotFound();
        }

        _dbContext.Departments.Remove(department);
        await _dbContext.SaveChangesAsync();
        return NoContent();
    }

    private Task<Department?> FindDepartmentAsync(string universityName, string collegeName, string departmentName)
    {
        return _dbContext.Departments
            .Include(d => d.College)!
                .ThenInclude(c => c!.University)
            .FirstOrDefaultAsync(d => d.College!.University!.Name == universityName && d.College!.Name == collegeName && d.Name == departmentName);
    }
}

public record CreateDepartmentRequest(string UniversityName, string CollegeName, string DepartmentName);
public record RenameDepartmentRequest(string UniversityName, string CollegeName, string CurrentName, string NewName);
