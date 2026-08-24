using Microsoft.AspNetCore.Identity;

namespace AuthService.Domain.Entities;

/// <summary>
/// Extends the built-in Identity user with application-specific fields.
/// </summary>
public class ApplicationUser : IdentityUser<int>
{
    public string FullName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
