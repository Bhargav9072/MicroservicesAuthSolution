using System.ComponentModel.DataAnnotations;

namespace ProjectService.Application.DTOs;

public class CreateTimeEntryDto
{
    [Required]
    public int ProjectId { get; set; }

    [Required]
    public int TaskId { get; set; }

    [Required]
    public DateTime EntryDate { get; set; }

    [Range(0.25, 24)]
    public decimal Hours { get; set; }

    public string Notes { get; set; } = string.Empty;
}
