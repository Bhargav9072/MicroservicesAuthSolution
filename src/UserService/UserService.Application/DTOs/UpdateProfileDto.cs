using System.ComponentModel.DataAnnotations;

namespace UserService.Application.DTOs;

public class UpdateProfileDto
{
    [Required]
    public string FullName { get; set; } = string.Empty;
}
