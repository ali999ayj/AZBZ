using System.Data;
using AttendanceSystem.Data;
using AttendanceSystem.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace AttendanceSystem.Services;

public class ExcelImportService
{
    private readonly AppDbContext _dbContext;
    private readonly AttendanceService _attendanceService;

    public ExcelImportService(AppDbContext dbContext, AttendanceService attendanceService)
    {
        _dbContext = dbContext;
        _attendanceService = attendanceService;
    }

    public async Task<ImportResult> ImportStudentsAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        using var stream = file.OpenReadStream();
        using var reader = ExcelDataReader.ExcelReaderFactory.CreateReader(stream);
        var dataSet = reader.AsDataSet();
        var table = dataSet.Tables[0];
        var created = 0;
        var updated = 0;

        for (var i = 1; i < table.Rows.Count; i++)
        {
            var row = table.Rows[i];
            var universityId = row[0]?.ToString()?.Trim();
            var firstName = row[1]?.ToString()?.Trim();
            var lastName = row[2]?.ToString()?.Trim();
            var stageName = row[3]?.ToString()?.Trim();
            var groupName = row[4]?.ToString()?.Trim();
            var departmentName = row[5]?.ToString()?.Trim();
            var collegeName = row[6]?.ToString()?.Trim();
            var universityName = row[7]?.ToString()?.Trim();

            if (string.IsNullOrWhiteSpace(universityId) || string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(groupName) || string.IsNullOrWhiteSpace(departmentName) ||
                string.IsNullOrWhiteSpace(collegeName) || string.IsNullOrWhiteSpace(universityName))
            {
                continue;
            }

            var university = await FindOrCreateUniversityAsync(universityName, cancellationToken);
            var college = await FindOrCreateCollegeAsync(university, collegeName, cancellationToken);
            var department = await FindOrCreateDepartmentAsync(college, departmentName, cancellationToken);
            Stage? stage = null;
            if (!string.IsNullOrWhiteSpace(stageName))
            {
                stage = await FindOrCreateStageAsync(department, stageName, cancellationToken);
            }

            var group = await FindOrCreateGroupAsync(department, stage, groupName, GroupKind.Theory, cancellationToken);

            var student = await _dbContext.Students.FirstOrDefaultAsync(s => s.UniversityId == universityId, cancellationToken);
            if (student == null)
            {
                student = new Student
                {
                    UniversityId = universityId,
                    FirstName = firstName!,
                    LastName = lastName!,
                    Group = group,
                    IsActive = true
                };
                await _dbContext.Students.AddAsync(student, cancellationToken);
                created++;
            }
            else
            {
                student.FirstName = firstName!;
                student.LastName = lastName!;
                student.Group = group;
                updated++;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new ImportResult(created, updated);
    }

    public async Task<ImportResult> ImportAttendanceAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        using var stream = file.OpenReadStream();
        using var reader = ExcelDataReader.ExcelReaderFactory.CreateReader(stream);
        var table = reader.AsDataSet().Tables[0];
        var created = 0;
        for (var i = 1; i < table.Rows.Count; i++)
        {
            var row = table.Rows[i];
            var universityId = row[0]?.ToString()?.Trim();
            var timestampString = row[1]?.ToString()?.Trim();

            if (string.IsNullOrWhiteSpace(universityId) || string.IsNullOrWhiteSpace(timestampString))
            {
                continue;
            }

            if (!DateTime.TryParse(timestampString, out var timestamp))
            {
                continue;
            }

            var student = await _dbContext.Students.FirstOrDefaultAsync(s => s.UniversityId == universityId, cancellationToken);
            if (student == null)
            {
                continue;
            }

            var resolution = await _attendanceService.ResolveAttendanceAsync(student.Id, timestamp, cancellationToken);
            var record = new AttendanceRecord
            {
                StudentId = student.Id,
                Date = DateOnly.FromDateTime(timestamp),
                Time = TimeOnly.FromDateTime(timestamp),
                CourseId = resolution.CourseId,
                CourseName = resolution.CourseName,
                Status = resolution.Status,
                Source = "Excel"
            };
            await _dbContext.AttendanceRecords.AddAsync(record, cancellationToken);
            created++;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new ImportResult(created, 0);
    }

    private async Task<University> FindOrCreateUniversityAsync(string universityName, CancellationToken ct)
    {
        var entity = await _dbContext.Universities.FirstOrDefaultAsync(u => u.Name == universityName, ct);
        if (entity != null)
        {
            return entity;
        }

        entity = new University { Name = universityName };
        await _dbContext.Universities.AddAsync(entity, ct);
        await _dbContext.SaveChangesAsync(ct);
        return entity;
    }

    private async Task<College> FindOrCreateCollegeAsync(University university, string collegeName, CancellationToken ct)
    {
        var entity = await _dbContext.Colleges.FirstOrDefaultAsync(c => c.UniversityId == university.Id && c.Name == collegeName, ct);
        if (entity != null)
        {
            return entity;
        }

        entity = new College { Name = collegeName, University = university };
        await _dbContext.Colleges.AddAsync(entity, ct);
        await _dbContext.SaveChangesAsync(ct);
        return entity;
    }

    private async Task<Department> FindOrCreateDepartmentAsync(College college, string departmentName, CancellationToken ct)
    {
        var entity = await _dbContext.Departments.FirstOrDefaultAsync(d => d.CollegeId == college.Id && d.Name == departmentName, ct);
        if (entity != null)
        {
            return entity;
        }

        entity = new Department { Name = departmentName, College = college };
        await _dbContext.Departments.AddAsync(entity, ct);
        await _dbContext.SaveChangesAsync(ct);
        return entity;
    }

    private async Task<Stage> FindOrCreateStageAsync(Department department, string stageName, CancellationToken ct)
    {
        var entity = await _dbContext.Stages.FirstOrDefaultAsync(s => s.DepartmentId == department.Id && s.Name == stageName, ct);
        if (entity != null)
        {
            return entity;
        }

        entity = new Stage { Name = stageName, Department = department };
        await _dbContext.Stages.AddAsync(entity, ct);
        await _dbContext.SaveChangesAsync(ct);
        return entity;
    }

    private async Task<Group> FindOrCreateGroupAsync(Department department, Stage? stage, string groupName, GroupKind kind, CancellationToken ct)
    {
        var query = _dbContext.Groups.Where(g => g.DepartmentId == department.Id && g.Name == groupName && g.Kind == kind);
        if (stage != null)
        {
            query = query.Where(g => g.StageId == stage.Id);
        }
        else
        {
            query = query.Where(g => g.StageId == null);
        }

        var entity = await query.FirstOrDefaultAsync(ct);
        if (entity != null)
        {
            return entity;
        }

        entity = new Group
        {
            Name = groupName,
            Department = department,
            Stage = stage,
            Kind = kind
        };
        await _dbContext.Groups.AddAsync(entity, ct);
        await _dbContext.SaveChangesAsync(ct);
        return entity;
    }
}

public record ImportResult(int Created, int Updated);
