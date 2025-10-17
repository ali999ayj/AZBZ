using AttendanceSystem.Data;
using AttendanceSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AttendanceSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScheduleController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public ScheduleController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpPost("entries/by-name")]
    public async Task<ActionResult<ScheduleEntry>> CreateEntryAsync(CreateScheduleEntryRequest request)
    {
        var group = await FindGroupAsync(request.UniversityName, request.CollegeName, request.DepartmentName, request.StageName, request.GroupName, request.GroupKind);
        if (group == null)
        {
            return NotFound("Group not found");
        }

        var course = await FindCourseAsync(request.UniversityName, request.CollegeName, request.DepartmentName, request.CourseName);
        var instructor = await FindInstructorAsync(request.UniversityName, request.CollegeName, request.DepartmentName, request.InstructorName);
        var room = await FindRoomAsync(request.UniversityName, request.CollegeName, request.DepartmentName, request.RoomName);
        if (course == null || instructor == null || room == null)
        {
            return NotFound("Course/Instructor/Room not found");
        }

        var entry = new ScheduleEntry
        {
            GroupId = group.Id,
            CourseId = course.Id,
            InstructorId = instructor.Id,
            RoomId = room.Id,
            DayOfWeek = request.Day,
            Start = request.Start,
            End = request.End
        };

        _dbContext.ScheduleEntries.Add(entry);
        await _dbContext.SaveChangesAsync();
        return Ok(entry);
    }

    [HttpPost("override/by-name")]
    public async Task<ActionResult<ScheduleOverride>> CreateOverrideAsync(CreateScheduleOverrideRequest request)
    {
        var group = await FindGroupAsync(request.UniversityName, request.CollegeName, request.DepartmentName, request.StageName, request.GroupName, request.GroupKind);
        if (group == null)
        {
            return NotFound("Group not found");
        }

        var course = await FindCourseAsync(request.UniversityName, request.CollegeName, request.DepartmentName, request.CourseName);
        var instructor = await FindInstructorAsync(request.UniversityName, request.CollegeName, request.DepartmentName, request.InstructorName);
        var room = await FindRoomAsync(request.UniversityName, request.CollegeName, request.DepartmentName, request.RoomName);
        if (course == null || instructor == null || room == null)
        {
            return NotFound("Course/Instructor/Room not found");
        }

        var entry = new ScheduleOverride
        {
            GroupId = group.Id,
            CourseId = course.Id,
            InstructorId = instructor.Id,
            RoomId = room.Id,
            Date = request.Date,
            Start = request.Start,
            End = request.End
        };

        _dbContext.ScheduleOverrides.Add(entry);
        await _dbContext.SaveChangesAsync();
        return Ok(entry);
    }

    [HttpGet("entries")] 
    public async Task<ActionResult<IEnumerable<ScheduleEntry>>> GetEntriesAsync([FromQuery] string universityName, [FromQuery] string collegeName, [FromQuery] string departmentName, [FromQuery] string? stageName, [FromQuery] string groupName, [FromQuery] GroupKind groupKind)
    {
        var query = _dbContext.ScheduleEntries
            .Include(e => e.Group)!
                .ThenInclude(g => g!.Department)!
                    .ThenInclude(d => d!.College)!
                        .ThenInclude(c => c!.University)
            .Include(e => e.Course)
            .Include(e => e.Instructor)
            .Include(e => e.Room)
            .Where(e => e.Group!.Department!.College!.University!.Name == universityName &&
                        e.Group.Department.College.Name == collegeName &&
                        e.Group.Department.Name == departmentName &&
                        e.Group.Name == groupName &&
                        e.Group.Kind == groupKind);

        if (!string.IsNullOrWhiteSpace(stageName))
        {
            query = query.Where(e => e.Group!.Stage != null && e.Group.Stage.Name == stageName);
        }
        else
        {
            query = query.Where(e => e.Group!.Stage == null);
        }

        var entries = await query.OrderBy(e => e.DayOfWeek).ThenBy(e => e.Start).ToListAsync();
        return Ok(entries);
    }

    [HttpGet("overrides")]
    public async Task<ActionResult<IEnumerable<ScheduleOverride>>> GetOverridesAsync([FromQuery] string universityName, [FromQuery] string collegeName, [FromQuery] string departmentName, [FromQuery] DateOnly? date)
    {
        var query = _dbContext.ScheduleOverrides
            .Include(o => o.Group)!
                .ThenInclude(g => g!.Department)!
                    .ThenInclude(d => d!.College)!
                        .ThenInclude(c => c!.University)
            .Include(o => o.Course)
            .Include(o => o.Instructor)
            .Include(o => o.Room)
            .Where(o => o.Group!.Department!.College!.University!.Name == universityName &&
                        o.Group.Department.College.Name == collegeName &&
                        o.Group.Department.Name == departmentName);

        if (date.HasValue)
        {
            query = query.Where(o => o.Date == date.Value);
        }

        var overrides = await query.OrderBy(o => o.Date).ThenBy(o => o.Start).ToListAsync();
        return Ok(overrides);
    }

    private Task<Group?> FindGroupAsync(string universityName, string collegeName, string departmentName, string? stageName, string groupName, GroupKind kind)
    {
        var query = _dbContext.Groups
            .Include(g => g.Department)!
                .ThenInclude(d => d!.College)!
                    .ThenInclude(c => c!.University)
            .Include(g => g.Stage)
            .Where(g => g.Department!.College!.University!.Name == universityName &&
                        g.Department.College.Name == collegeName &&
                        g.Department.Name == departmentName &&
                        g.Name == groupName &&
                        g.Kind == kind);

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

    private Task<Course?> FindCourseAsync(string universityName, string collegeName, string departmentName, string courseName)
    {
        return _dbContext.Courses
            .Include(c => c.Department)!
                .ThenInclude(d => d!.College)!
                    .ThenInclude(c => c!.University)
            .FirstOrDefaultAsync(c => c.Department!.College!.University!.Name == universityName &&
                                      c.Department.College.Name == collegeName &&
                                      c.Department.Name == departmentName &&
                                      c.Name == courseName);
    }

    private Task<Instructor?> FindInstructorAsync(string universityName, string collegeName, string departmentName, string instructorName)
    {
        return _dbContext.Instructors
            .Include(i => i.Department)!
                .ThenInclude(d => d!.College)!
                    .ThenInclude(c => c!.University)
            .FirstOrDefaultAsync(i => i.Department!.College!.University!.Name == universityName &&
                                      i.Department.College.Name == collegeName &&
                                      i.Department.Name == departmentName &&
                                      i.FullName == instructorName);
    }

    private Task<Room?> FindRoomAsync(string universityName, string collegeName, string departmentName, string roomName)
    {
        return _dbContext.Rooms
            .Include(r => r.Department)!
                .ThenInclude(d => d!.College)!
                    .ThenInclude(c => c!.University)
            .FirstOrDefaultAsync(r => r.Department!.College!.University!.Name == universityName &&
                                      r.Department.College.Name == collegeName &&
                                      r.Department.Name == departmentName &&
                                      r.Name == roomName);
    }
}

public record CreateScheduleEntryRequest(string UniversityName, string CollegeName, string DepartmentName, string? StageName, string GroupName, GroupKind GroupKind, string CourseName, string InstructorName, string RoomName, DayOfWeek Day, TimeOnly Start, TimeOnly End);
public record CreateScheduleOverrideRequest(string UniversityName, string CollegeName, string DepartmentName, string? StageName, string GroupName, GroupKind GroupKind, string CourseName, string InstructorName, string RoomName, DateOnly Date, TimeOnly Start, TimeOnly End);
