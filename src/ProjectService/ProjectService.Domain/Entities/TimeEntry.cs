namespace ProjectService.Domain.Entities;

public class TimeEntry
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int ProjectId { get; set; }
    public int TaskId { get; set; }
    public DateTime EntryDate { get; set; }
    public decimal Hours { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ProjectItem? Project { get; set; }
    public ProjectTaskItem? Task { get; set; }
}
