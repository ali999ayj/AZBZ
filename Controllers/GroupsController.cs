using AttendanceSystem.Data;
using AttendanceSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AttendanceSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GroupsController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public GroupsController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Group>>> GetAsync([FromQuery] string universityName, [FromQuery] string collegeName, [FromQuery] string departmentName)
    {
        var groups = await BuildBaseQuery(universityName, collegeName, departmentName)
            .OrderBy(g => g.Kind)
            .ThenBy(g => g.Name)
            .ToListAsync();
        return Ok(groups);
    }

    [HttpGet("split-by-kind")]
    public async Task<ActionResult<GroupSplitResponse>> SplitByKindAsync([FromQuery] string universityName, [FromQuery] string collegeName, [FromQuery] string departmentName, [FromQuery] string? stageName)
    {
        var query = BuildBaseQuery(universityName, collegeName, departmentName);
        if (!string.IsNullOrWhiteSpace(stageName))
        {
            query = query.Where(g => g.Stage != null && g.Stage.Name == stageName);
        }
        else
        {
            query = query.Where(g => g.Stage == null);
        }

        var groups = await query.OrderBy(g => g.Name).ToListAsync();
        return Ok(new GroupSplitResponse(
            groups.Where(g => g.Kind == GroupKind.Theory).ToList(),
            groups.Where(g => g.Kind == GroupKind.Practical).ToList()));
    }

    [HttpPost("create-by-name")]
    public async Task<ActionResult<Group>> CreateAsync(CreateGroupRequest request)
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

        Stage? stage = null;
        if (!string.IsNullOrWhiteSpace(request.StageName))
        {
            stage = await _dbContext.Stages.FirstOrDefaultAsync(s => s.DepartmentId == department.Id && s.Name == request.StageName);
            if (stage == null)
            {
                return NotFound("Stage not found");
            }
        }

        var stageId = stage?.Id;
        var exists = await _dbContext.Groups.AnyAsync(g => g.DepartmentId == department.Id && g.Name == request.GroupName && g.Kind == request.Kind && g.StageId == stageId);
        if (exists)
        {
            return Conflict("Group already exists");
        }

        var group = new Group
        {
            Name = request.GroupName,
            Department = department,
            Stage = stage,
            Kind = request.Kind
        };

        _dbContext.Groups.Add(group);
        await _dbContext.SaveChangesAsync();
        return Ok(group);
    }

    [HttpPut("rename")]
    public async Task<IActionResult> RenameAsync(RenameGroupRequest request)
    {
        var group = await FindGroupAsync(request.UniversityName, request.CollegeName, request.DepartmentName, request.StageName, request.Kind, request.CurrentName);
        if (group == null)
        {
            return NotFound();
        }

        group.Name = request.NewName;
        await _dbContext.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("by-name")]
    public async Task<IActionResult> DeleteAsync([FromQuery] string universityName, [FromQuery] string collegeName, [FromQuery] string departmentName, [FromQuery] string? stageName, [FromQuery] GroupKind kind, [FromQuery] string groupName)
    {
        var group = await FindGroupAsync(universityName, collegeName, departmentName, stageName, kind, groupName);
        if (group == null)
        {
            return NotFound();
        }

        _dbContext.Groups.Remove(group);
        await _dbContext.SaveChangesAsync();
        return NoContent();
    }

    private IQueryable<Group> BuildBaseQuery(string universityName, string collegeName, string departmentName)
    {
        return _dbContext.Groups
            .Include(g => g.Department)!
                .ThenInclude(d => d!.College)!
                    .ThenInclude(c => c!.University)
            .Include(g => g.Stage)
            .Where(g => g.Department!.College!.University!.Name == universityName &&
                        g.Department.College.Name == collegeName &&
                        g.Department.Name == departmentName);
    }

    private Task<Group?> FindGroupAsync(string universityName, string collegeName, string departmentName, string? stageName, GroupKind kind, string groupName)
    {
        var query = BuildBaseQuery(universityName, collegeName, departmentName)
            .Where(g => g.Kind == kind && g.Name == groupName);
        if (!string.IsNullOrWhiteSpace(stageName))
        {
            query = query.Where(g => g.Stage != null && g.Stage.Name == stageName);
        }
        else
        {
            query = query.Where(g => g.Stage == null);
        }

        return query.FirstOrDefaultAsync();
    }
}

public record CreateGroupRequest(string UniversityName, string CollegeName, string DepartmentName, string? StageName, string GroupName, GroupKind Kind);
public record RenameGroupRequest(string UniversityName, string CollegeName, string DepartmentName, string? StageName, GroupKind Kind, string CurrentName, string NewName);
public record GroupSplitResponse(IList<Group> Theory, IList<Group> Practical);
