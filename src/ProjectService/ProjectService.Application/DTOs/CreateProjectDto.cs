using System.ComponentModel.DataAnnotations;

namespace ProjectService.Application.DTOs;

public class CreateProjectDto
{
    [Required]
    public string Name { get; set; } = string.Empty;
}
