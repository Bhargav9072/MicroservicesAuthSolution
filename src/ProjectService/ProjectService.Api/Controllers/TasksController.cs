using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectService.Application.DTOs;
using ProjectService.Domain.Entities;
using ProjectService.Infrastructure.Data;

namespace ProjectService.Api.Controllers;

[ApiController]
[Route("api/tasks")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly ProjectDbContext _dbContext;

    public TasksController(ProjectDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private static TaskDto ToDto(ProjectTaskItem t) => new()
    {
        Id = t.Id,
        ProjectId = t.ProjectId,
        ProjectName = t.Project?.Name ?? string.Empty,
        Title = t.Title,
        IsActive = t.IsActive,
        CreatedAt = t.CreatedAt,
        UpdatedAt = t.UpdatedAt
    };

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? projectId)
    {
        var query = _dbContext.Tasks.Where(t => t.IsActive).AsQueryable();
        if (projectId.HasValue)
        {
            query = query.Where(t => t.ProjectId == projectId.Value);
        }

        var tasks = await query
            .Include(t => t.Project)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return Ok(tasks.Select(ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var task = await _dbContext.Tasks
            .Include(t => t.Project)
            .FirstOrDefaultAsync(t => t.Id == id && t.IsActive);
        if (task is null) return NotFound(new { message = "Task not found." });

        return Ok(ToDto(task));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateTaskDto dto)
    {
        var projectExists = await _dbContext.Projects.AnyAsync(p => p.Id == dto.ProjectId);
        if (!projectExists)
        {
            return BadRequest(new { message = "Invalid ProjectId. Project does not exist." });
        }

        var task = new ProjectTaskItem
        {
            ProjectId = dto.ProjectId,
            Title = dto.Title,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Tasks.Add(task);
        await _dbContext.SaveChangesAsync();

        task.Project = await _dbContext.Projects.FirstOrDefaultAsync(p => p.Id == task.ProjectId);

        return CreatedAtAction(nameof(GetById), new { id = task.Id }, ToDto(task));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTaskDto dto)
    {
        var projectExists = await _dbContext.Projects.AnyAsync(p => p.Id == dto.ProjectId && p.IsActive);
        if (!projectExists)
        {
            return BadRequest(new { message = "Invalid ProjectId. Active project does not exist." });
        }

        var task = await _dbContext.Tasks.FirstOrDefaultAsync(t => t.Id == id);
        if (task is null) return NotFound(new { message = "Task not found." });

        task.ProjectId = dto.ProjectId;
        task.Title = dto.Title;
        task.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        task.Project = await _dbContext.Projects.FirstOrDefaultAsync(p => p.Id == task.ProjectId);
        return Ok(ToDto(task));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var task = await _dbContext.Tasks.FirstOrDefaultAsync(t => t.Id == id);
        if (task is null) return NotFound(new { message = "Task not found." });

        task.IsActive = false;
        task.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        return Ok(new { message = "Task deleted successfully." });
    }
}
                