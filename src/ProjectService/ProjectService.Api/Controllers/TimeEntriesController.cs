using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectService.Application.DTOs;
using ProjectService.Domain.Entities;
using ProjectService.Infrastructure.Data;

namespace ProjectService.Api.Controllers;

[ApiController]
[Route("api/time-entries")]
[Authorize(Roles = "Employee")]
public class TimeEntriesController : ControllerBase
{
    private readonly ProjectDbContext _dbContext;

    public TimeEntriesController(ProjectDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue("sub")
                  ?? throw new UnauthorizedAccessException());

    private static TimeEntryDto ToDto(TimeEntry t) => new()
    {
        Id = t.Id,
        UserId = t.UserId,
        ProjectId = t.ProjectId,
        ProjectName = t.Project?.Name ?? string.Empty,
        TaskId = t.TaskId,
        TaskTitle = t.Task?.Title ?? string.Empty,
        EntryDate = t.EntryDate,
        Hours = t.Hours,
        Notes = t.Notes,
        IsActive = t.IsActive,
        CreatedAt = t.CreatedAt,
        UpdatedAt = t.UpdatedAt
    };

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, [FromQuery] int? projectId, [FromQuery] int? taskId)
    {
        var query = _dbContext.TimeEntries
            .Where(t => t.IsActive && t.UserId == CurrentUserId)
            .Include(t => t.Project)
            .Include(t => t.Task)
            .AsQueryable();

        if (fromDate.HasValue)
        {
            query = query.Where(t => t.EntryDate.Date >= fromDate.Value.Date);
        }

        if (toDate.HasValue)
        {
            query = query.Where(t => t.EntryDate.Date <= toDate.Value.Date);
        }

        if (projectId.HasValue)
        {
            query = query.Where(t => t.ProjectId == projectId.Value);
        }

        if (taskId.HasValue)
        {
            query = query.Where(t => t.TaskId == taskId.Value);
        }

        var entries = await query
            .OrderByDescending(t => t.EntryDate)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync();

        return Ok(entries.Select(ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var entry = await _dbContext.TimeEntries
            .Include(t => t.Project)
            .Include(t => t.Task)
            .FirstOrDefaultAsync(t => t.Id == id && t.IsActive && t.UserId == CurrentUserId);

        if (entry is null) return NotFound(new { message = "Time entry not found." });

        return Ok(ToDto(entry));
    }

    [HttpGet("by-date-user")]
    public async Task<IActionResult> GetByDateAndUser([FromQuery] DateTime entryDate, [FromQuery] int userId)
    {
        if (userId != CurrentUserId)
        {
            return Forbid();
        }

        var entries = await _dbContext.TimeEntries
            .Where(t => t.IsActive && t.UserId == userId && t.EntryDate.Date == entryDate.Date)
            .Include(t => t.Project)
            .Include(t => t.Task)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return Ok(entries.Select(ToDto));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTimeEntryDto dto)
    {
        var project = await _dbContext.Projects.FirstOrDefaultAsync(p => p.Id == dto.ProjectId && p.IsActive);
        if (project is null)
        {
            return BadRequest(new { message = "Invalid ProjectId. Active project does not exist." });
        }

        var task = await _dbContext.Tasks.FirstOrDefaultAsync(t => t.Id == dto.TaskId && t.IsActive);
        if (task is null)
        {
            return BadRequest(new { message = "Invalid TaskId. Active task does not exist." });
        }

        if (task.ProjectId != dto.ProjectId)
        {
            return BadRequest(new { message = "Task does not belong to the selected project." });
        }

        var entry = new TimeEntry
        {
            UserId = CurrentUserId,
            ProjectId = dto.ProjectId,
            TaskId = dto.TaskId,
            EntryDate = dto.EntryDate.Date,
            Hours = dto.Hours,
            Notes = dto.Notes,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.TimeEntries.Add(entry);
        await _dbContext.SaveChangesAsync();

        entry.Project = project;
        entry.Task = task;

        return CreatedAtAction(nameof(GetById), new { id = entry.Id }, ToDto(entry));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTimeEntryDto dto)
    {
        var entry = await _dbContext.TimeEntries
            .FirstOrDefaultAsync(t => t.Id == id && t.IsActive && t.UserId == CurrentUserId);

        if (entry is null) return NotFound(new { message = "Time entry not found." });

        var project = await _dbContext.Projects.FirstOrDefaultAsync(p => p.Id == dto.ProjectId && p.IsActive);
        if (project is null)
        {
            return BadRequest(new { message = "Invalid ProjectId. Active project does not exist." });
        }

        var task = await _dbContext.Tasks.FirstOrDefaultAsync(t => t.Id == dto.TaskId && t.IsActive);
        if (task is null)
        {
            return BadRequest(new { message = "Invalid TaskId. Active task does not exist." });
        }

        if (task.ProjectId != dto.ProjectId)
        {
            return BadRequest(new { message = "Task does not belong to the selected project." });
        }

        entry.ProjectId = dto.ProjectId;
        entry.TaskId = dto.TaskId;
        entry.EntryDate = dto.EntryDate.Date;
        entry.Hours = dto.Hours;
        entry.Notes = dto.Notes;
        entry.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        entry.Project = project;
        entry.Task = task;

        return Ok(ToDto(entry));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var entry = await _dbContext.TimeEntries
            .FirstOrDefaultAsync(t => t.Id == id && t.IsActive && t.UserId == CurrentUserId);

        if (entry is null) return NotFound(new { message = "Time entry not found." });

        entry.IsActive = false;
        entry.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        return Ok(new { message = "Time entry deleted successfully." });
    }
}
