using AttendanceSystem.Services;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImportController : ControllerBase
{
    private readonly ExcelImportService _importService;

    public ImportController(ExcelImportService importService)
    {
        _importService = importService;
    }

    [HttpPost("students-excel")]
    public async Task<ActionResult<ImportResult>> ImportStudentsAsync([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("File is required");
        }

        var result = await _importService.ImportStudentsAsync(file);
        return Ok(result);
    }

    [HttpPost("attendance-excel")]
    public async Task<ActionResult<ImportResult>> ImportAttendanceAsync([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("File is required");
        }

        var result = await _importService.ImportAttendanceAsync(file);
        return Ok(result);
    }
}
