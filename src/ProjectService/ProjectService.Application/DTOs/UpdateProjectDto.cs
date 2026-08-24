using System.ComponentModel.DataAnnotations;

namespace ProjectService.Application.DTOs;

public class UpdateProjectDto
{
    [Required]
    public string Name { get; set; } = string.Empty;
}
