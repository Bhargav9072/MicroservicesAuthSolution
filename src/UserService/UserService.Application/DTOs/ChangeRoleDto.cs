using System.ComponentModel.DataAnnotations;

namespace UserService.Application.DTOs;

public class ChangeRoleDto
{
    [Required]
    public string Role { get; set; } = string.Empty; // "Admin" or "Manager" or "Employee"
}
