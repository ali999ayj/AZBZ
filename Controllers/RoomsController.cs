using AttendanceSystem.Data;
using AttendanceSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AttendanceSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoomsController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public RoomsController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Room>>> GetAsync([FromQuery] string universityName, [FromQuery] string collegeName, [FromQuery] string departmentName)
    {
        var rooms = await _dbContext.Rooms
            .Include(r => r.Department)!
                .ThenInclude(d => d!.College)!
                    .ThenInclude(c => c!.University)
            .Where(r => r.Department!.College!.University!.Name == universityName &&
                        r.Department.College.Name == collegeName &&
                        r.Department.Name == departmentName)
            .OrderBy(r => r.Name)
            .ToListAsync();
        return Ok(rooms);
    }

    [HttpPost("create-by-name")]
    public async Task<ActionResult<Room>> CreateAsync(CreateRoomRequest request)
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

        var exists = await _dbContext.Rooms.AnyAsync(r => r.DepartmentId == department.Id && r.Name == request.RoomName);
        if (exists)
        {
            return Conflict("Room already exists");
        }

        var room = new Room { Name = request.RoomName, Department = department };
        _dbContext.Rooms.Add(room);
        await _dbContext.SaveChangesAsync();
        return Ok(room);
    }

    [HttpPut("rename")]
    public async Task<IActionResult> RenameAsync(RenameRoomRequest request)
    {
        var room = await _dbContext.Rooms
            .Include(r => r.Department)!
                .ThenInclude(d => d!.College)!
                    .ThenInclude(c => c!.University)
            .FirstOrDefaultAsync(r => r.Department!.College!.University!.Name == request.UniversityName &&
                                      r.Department!.College!.Name == request.CollegeName &&
                                      r.Department!.Name == request.DepartmentName &&
                                      r.Name == request.CurrentName);
        if (room == null)
        {
            return NotFound();
        }

        room.Name = request.NewName;
        await _dbContext.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("by-name")]
    public async Task<IActionResult> DeleteAsync([FromQuery] string universityName, [FromQuery] string collegeName, [FromQuery] string departmentName, [FromQuery] string roomName)
    {
        var room = await _dbContext.Rooms
            .Include(r => r.Department)!
                .ThenInclude(d => d!.College)!
                    .ThenInclude(c => c!.University)
            .FirstOrDefaultAsync(r => r.Department!.College!.University!.Name == universityName &&
                                      r.Department!.College!.Name == collegeName &&
                                      r.Department!.Name == departmentName &&
                                      r.Name == roomName);
        if (room == null)
        {
            return NotFound();
        }

        _dbContext.Rooms.Remove(room);
        await _dbContext.SaveChangesAsync();
        return NoContent();
    }
}

public record CreateRoomRequest(string UniversityName, string CollegeName, string DepartmentName, string RoomName);
public record RenameRoomRequest(string UniversityName, string CollegeName, string DepartmentName, string CurrentName, string NewName);
