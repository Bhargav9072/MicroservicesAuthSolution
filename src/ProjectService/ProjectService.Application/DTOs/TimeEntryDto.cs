namespace ProjectService.Application.DTOs;

public class TimeEntryDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public int TaskId { get; set; }
    public string TaskTitle { get; set; } = string.Empty;
    public DateTime EntryDate { get; set; }
    public decimal Hours { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
