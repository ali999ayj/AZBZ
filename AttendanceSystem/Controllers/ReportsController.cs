using AttendanceSystem.Data;
using AttendanceSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AttendanceSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public ReportsController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("schedule-today")]
    public async Task<ActionResult<ScheduleReportResponse>> GetScheduleTodayAsync([FromQuery] string universityName, [FromQuery] string collegeName, [FromQuery] string departmentName, [FromQuery] DayOfWeek? day)
    {
        var targetDay = day ?? DateTime.Today.DayOfWeek;
        var entries = await _dbContext.ScheduleEntries
            .Include(e => e.Group)!
                .ThenInclude(g => g!.Stage)
            .Include(e => e.Course)
            .Include(e => e.Instructor)
            .Include(e => e.Room)
            .Where(e => e.Group!.Department!.College!.University!.Name == universityName &&
                        e.Group.Department.College.Name == collegeName &&
                        e.Group.Department.Name == departmentName &&
                        e.DayOfWeek == targetDay)
            .OrderBy(e => e.Group!.Name)
            .ThenBy(e => e.Start)
            .ToListAsync();

        var overrides = await _dbContext.ScheduleOverrides
            .Include(o => o.Group)!
                .ThenInclude(g => g!.Stage)
            .Include(o => o.Course)
            .Include(o => o.Instructor)
            .Include(o => o.Room)
            .Where(o => o.Group!.Department!.College!.University!.Name == universityName &&
                        o.Group.Department.College.Name == collegeName &&
                        o.Group.Department.Name == departmentName &&
                        o.Date == DateOnly.FromDateTime(DateTime.Today))
            .OrderBy(o => o.Group!.Name)
            .ThenBy(o => o.Start)
            .ToListAsync();

        return Ok(new ScheduleReportResponse(entries, overrides));
    }

    [HttpGet("attendance-summary")]
    public async Task<ActionResult<IEnumerable<AttendanceSummaryRow>>> GetAttendanceSummaryAsync([FromQuery] string universityName, [FromQuery] string collegeName, [FromQuery] string departmentName, [FromQuery] DateOnly? startDate, [FromQuery] DateOnly? endDate)
    {
        var query = _dbContext.AttendanceRecords
            .Include(r => r.Student)!
                .ThenInclude(s => s!.Group)!
                    .ThenInclude(g => g!.Department)!
                        .ThenInclude(d => d!.College)!
                            .ThenInclude(c => c!.University)
            .AsQueryable();

        query = query.Where(r => r.Student!.Group!.Department!.College!.University!.Name == universityName &&
                                 r.Student.Group.Department.College.Name == collegeName &&
                                 r.Student.Group.Department.Name == departmentName);

        if (startDate.HasValue)
        {
            query = query.Where(r => r.Date >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(r => r.Date <= endDate.Value);
        }

        var summary = await query
            .GroupBy(r => new { r.Student!.UniversityId, r.Student.FirstName, r.Student.LastName })
            .Select(g => new AttendanceSummaryRow(
                g.Key.UniversityId,
                g.Key.FirstName,
                g.Key.LastName,
                g.Count(r => r.Status == AttendanceStatus.Present),
                g.Count(r => r.Status == AttendanceStatus.Late),
                g.Count(r => r.Status == AttendanceStatus.OutOfWindow)))
            .OrderBy(r => r.UniversityId)
            .ToListAsync();

        return Ok(summary);
    }
}

public record ScheduleReportResponse(IList<ScheduleEntry> Entries, IList<ScheduleOverride> Overrides);
public record AttendanceSummaryRow(string UniversityId, string FirstName, string LastName, int PresentCount, int LateCount, int OutOfWindowCount);
