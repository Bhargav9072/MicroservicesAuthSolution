namespace UserService.Domain.Entities;

/// <summary>
/// Application-level profile data for a user. "AuthUserId" maps back to the
/// user's Id in AuthService's AspNetUsers table (the source of truth for
/// credentials). Role is denormalized here from the JWT for quick queries.
/// </summary>
public class UserProfile
{
    public int Id { get; set; }
    public int AuthUserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "Employee";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
