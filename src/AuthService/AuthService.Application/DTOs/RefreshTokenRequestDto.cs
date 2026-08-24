using System.ComponentModel.DataAnnotations;

namespace AuthService.Application.DTOs;

public class RefreshTokenRequestDto
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
