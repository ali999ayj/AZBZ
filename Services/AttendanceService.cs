using AttendanceSystem.Data;
using AttendanceSystem.Models;
using AttendanceSystem.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AttendanceSystem.Services;

public record AttendanceResolution(AttendanceStatus Status, int? CourseId, string? CourseName);

public class AttendanceService
{
    private readonly AppDbContext _dbContext;
    private readonly AttendanceOptions _options;

    public AttendanceService(AppDbContext dbContext, IOptions<AttendanceOptions> options)
    {
        _dbContext = dbContext;
        _options = options.Value;
    }

    public async Task<AttendanceResolution> ResolveAttendanceAsync(int studentId, DateTime timestamp, CancellationToken cancellationToken = default)
    {
        var student = await _dbContext.Students
            .Include(s => s.Group)!
                .ThenInclude(g => g!.Department)
            .FirstOrDefaultAsync(s => s.Id == studentId, cancellationToken);

        if (student is null)
        {
            return new AttendanceResolution(AttendanceStatus.OutOfWindow, null, null);
        }

        var groupId = student.GroupId;
        var date = DateOnly.FromDateTime(timestamp);
        var time = TimeOnly.FromDateTime(timestamp);

        var overrideEntry = await _dbContext.ScheduleOverrides
            .Include(o => o.Course)
            .Where(o => o.GroupId == groupId && o.Date == date && time >= o.Start && time <= o.End)
            .OrderBy(o => o.Start)
            .FirstOrDefaultAsync(cancellationToken);

        if (overrideEntry != null)
        {
            var status = ResolveStatus(time, overrideEntry.Start, overrideEntry.End);
            return new AttendanceResolution(status, overrideEntry.CourseId, overrideEntry.Course?.Name);
        }

        var day = timestamp.DayOfWeek;
        var schedule = await _dbContext.ScheduleEntries
            .Include(e => e.Course)
            .Where(e => e.GroupId == groupId && e.DayOfWeek == day && time >= e.Start && time <= e.End)
            .OrderBy(e => e.Start)
            .FirstOrDefaultAsync(cancellationToken);

        if (schedule != null)
        {
            var status = ResolveStatus(time, schedule.Start, schedule.End);
            return new AttendanceResolution(status, schedule.CourseId, schedule.Course?.Name);
        }

        return new AttendanceResolution(AttendanceStatus.OutOfWindow, null, null);
    }

    private AttendanceStatus ResolveStatus(TimeOnly actual, TimeOnly start, TimeOnly end)
    {
        if (actual < start || actual > end)
        {
            return AttendanceStatus.OutOfWindow;
        }

        if (actual > start.AddMinutes(_options.LateMinutes))
        {
            return AttendanceStatus.Late;
        }

        return AttendanceStatus.Present;
    }
}
