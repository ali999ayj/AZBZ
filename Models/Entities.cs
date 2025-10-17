using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AttendanceSystem.Models;

public class University
{
    public int Id { get; set; }
    [MaxLength(200)]
    public required string Name { get; set; }
    public ICollection<College> Colleges { get; set; } = new List<College>();
}

public class College
{
    public int Id { get; set; }
    [MaxLength(200)]
    public required string Name { get; set; }

    public int UniversityId { get; set; }
    public University? University { get; set; }

    public ICollection<Department> Departments { get; set; } = new List<Department>();
}

public class Department
{
    public int Id { get; set; }
    [MaxLength(200)]
    public required string Name { get; set; }

    public int CollegeId { get; set; }
    public College? College { get; set; }

    public ICollection<Stage> Stages { get; set; } = new List<Stage>();
    public ICollection<Group> Groups { get; set; } = new List<Group>();
}

public class Stage
{
    public int Id { get; set; }
    [MaxLength(200)]
    public required string Name { get; set; }

    public int DepartmentId { get; set; }
    public Department? Department { get; set; }

    public ICollection<Group> Groups { get; set; } = new List<Group>();
}

public enum GroupKind
{
    Theory,
    Practical
}

public class Group
{
    public int Id { get; set; }
    [MaxLength(100)]
    public required string Name { get; set; }

    public GroupKind Kind { get; set; }

    public int DepartmentId { get; set; }
    public Department? Department { get; set; }

    public int? StageId { get; set; }
    public Stage? Stage { get; set; }

    public ICollection<Student> Students { get; set; } = new List<Student>();
}

public class Course
{
    public int Id { get; set; }
    [MaxLength(200)]
    public required string Name { get; set; }

    public int DepartmentId { get; set; }
    public Department? Department { get; set; }
}

public class Instructor
{
    public int Id { get; set; }
    [MaxLength(200)]
    public required string FullName { get; set; }

    public int DepartmentId { get; set; }
    public Department? Department { get; set; }
}

public class Room
{
    public int Id { get; set; }
    [MaxLength(200)]
    public required string Name { get; set; }

    public int DepartmentId { get; set; }
    public Department? Department { get; set; }
}

public class ScheduleEntry
{
    public int Id { get; set; }
    public required int GroupId { get; set; }
    public Group? Group { get; set; }

    public required int CourseId { get; set; }
    public Course? Course { get; set; }

    public required int InstructorId { get; set; }
    public Instructor? Instructor { get; set; }

    public required int RoomId { get; set; }
    public Room? Room { get; set; }

    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly Start { get; set; }
    public TimeOnly End { get; set; }
}

public class ScheduleOverride
{
    public int Id { get; set; }
    public required int GroupId { get; set; }
    public Group? Group { get; set; }

    public required int CourseId { get; set; }
    public Course? Course { get; set; }

    public required int InstructorId { get; set; }
    public Instructor? Instructor { get; set; }

    public required int RoomId { get; set; }
    public Room? Room { get; set; }

    public DateOnly Date { get; set; }
    public TimeOnly Start { get; set; }
    public TimeOnly End { get; set; }
}

public class Student
{
    public int Id { get; set; }
    [MaxLength(50)]
    public required string UniversityId { get; set; }
    [MaxLength(200)]
    public required string FirstName { get; set; }
    [MaxLength(200)]
    public required string LastName { get; set; }

    public bool IsActive { get; set; } = true;

    public int GroupId { get; set; }
    public Group? Group { get; set; }
}

public enum AttendanceStatus
{
    Present,
    Late,
    OutOfWindow,
    Absent
}

public class AttendanceRecord
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public Student? Student { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly Time { get; set; }
    public int? CourseId { get; set; }
    public string? CourseName { get; set; }
    public AttendanceStatus Status { get; set; }
    public string Source { get; set; } = "Excel";
}
