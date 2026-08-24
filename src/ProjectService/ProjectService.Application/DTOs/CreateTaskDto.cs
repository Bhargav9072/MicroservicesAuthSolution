using System.ComponentModel.DataAnnotations;

namespace ProjectService.Application.DTOs;

public class CreateTaskDto
{
    [Required]
    public int ProjectId { get; set; }

    [Required]
    public string Title { get; set; } = string.Empty;
}
