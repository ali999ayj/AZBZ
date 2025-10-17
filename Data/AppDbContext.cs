using AttendanceSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace AttendanceSystem.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<University> Universities => Set<University>();
    public DbSet<College> Colleges => Set<College>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Stage> Stages => Set<Stage>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Instructor> Instructors => Set<Instructor>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<ScheduleEntry> ScheduleEntries => Set<ScheduleEntry>();
    public DbSet<ScheduleOverride> ScheduleOverrides => Set<ScheduleOverride>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<University>().HasIndex(u => u.Name).IsUnique();
        modelBuilder.Entity<College>().HasIndex(c => new { c.UniversityId, c.Name }).IsUnique();
        modelBuilder.Entity<Department>().HasIndex(d => new { d.CollegeId, d.Name }).IsUnique();
        modelBuilder.Entity<Stage>().HasIndex(s => new { s.DepartmentId, s.Name }).IsUnique();
        modelBuilder.Entity<Group>().HasIndex(g => new { g.DepartmentId, g.StageId, g.Name, g.Kind }).IsUnique();
        modelBuilder.Entity<Course>().HasIndex(c => new { c.DepartmentId, c.Name }).IsUnique();
        modelBuilder.Entity<Instructor>().HasIndex(i => new { i.DepartmentId, i.FullName }).IsUnique();
        modelBuilder.Entity<Room>().HasIndex(r => new { r.DepartmentId, r.Name }).IsUnique();
        modelBuilder.Entity<Student>().HasIndex(s => s.UniversityId).IsUnique();

        modelBuilder.Entity<ScheduleEntry>()
            .HasIndex(e => new { e.GroupId, e.DayOfWeek, e.Start })
            .IsUnique();

        modelBuilder.Entity<ScheduleOverride>()
            .HasIndex(o => new { o.GroupId, o.Date, o.Start })
            .IsUnique();
    }
}
