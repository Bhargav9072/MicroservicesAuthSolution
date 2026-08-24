using System.Security.Claims;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserService.Application.DTOs;
using UserService.Domain.Entities;
using UserService.Infrastructure.Data;

namespace UserService.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly UserDbContext _dbContext;
    private readonly IHttpClientFactory _httpClientFactory;

    public UsersController(UserDbContext dbContext, IHttpClientFactory httpClientFactory)
    {
        _dbContext = dbContext;
        _httpClientFactory = httpClientFactory;
    }

    private int CurrentAuthUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue("sub")
                  ?? throw new UnauthorizedAccessException());

    private string CurrentEmail => User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
    private string CurrentFullName => User.FindFirstValue("fullName") ?? string.Empty;
    private string CurrentRole => User.FindFirstValue(ClaimTypes.Role) ?? "Employee";

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterUserDto dto)
    {
        if (dto.Role is not ("Admin" or "Manager" or "Employee"))
        {
            return BadRequest(new { message = "Role must be 'Admin', 'Manager', or 'Employee'." });
        }

        if (await _dbContext.UserProfiles.AnyAsync(p => p.Email == dto.Email))
        {
            return Conflict(new { message = "A user with this email already exists." });
        }

        var authClient = _httpClientFactory.CreateClient("AuthService");

        var authResponse = await authClient.PostAsJsonAsync("api/auth/register", dto);
        if (authResponse.StatusCode == HttpStatusCode.Conflict)
        {
            return Conflict(new { message = "A user with this email already exists." });
        }

        if (!authResponse.IsSuccessStatusCode)
        {
            var errorBody = await authResponse.Content.ReadAsStringAsync();
            return StatusCode((int)authResponse.StatusCode, new
            {
                message = "Failed to register user in AuthService.",
                details = errorBody
            });
        }

        var authPayload = await authResponse.Content.ReadFromJsonAsync<AuthRegisterResponseDto>();
        if (authPayload is null)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                message = "AuthService returned an invalid registration payload."
            });
        }

        var profile = new UserProfile
        {
            AuthUserId = authPayload.UserId,
            Email = authPayload.Email,
            FullName = authPayload.FullName,
            Role = authPayload.Role
        };

        _dbContext.UserProfiles.Add(profile);
        await _dbContext.SaveChangesAsync();

        return Ok(ToDto(profile));
    }

    /// <summary>
    /// Ensures a UserProfile row exists for the currently authenticated JWT
    /// subject. This "just-in-time" provisioning keeps UserService decoupled
    /// from AuthService (no synchronous call needed on registration) — in a
    /// production system this would instead be driven by a "UserRegistered"
    /// event from a message broker.
    /// </summary>
    private async Task<UserProfile> GetOrProvisionCurrentProfileAsync()
    {
        var profile = await _dbContext.UserProfiles
            .FirstOrDefaultAsync(p => p.AuthUserId == CurrentAuthUserId);

        if (profile is null)
        {
            profile = new UserProfile
            {
                AuthUserId = CurrentAuthUserId,
                Email = CurrentEmail,
                FullName = CurrentFullName,
                Role = CurrentRole
            };
            _dbContext.UserProfiles.Add(profile);
            await _dbContext.SaveChangesAsync();
        }

        return profile;
    }

    private static UserProfileDto ToDto(UserProfile p) => new()
    {
        Id = p.Id,
        AuthUserId = p.AuthUserId,
        FullName = p.FullName,
        Email = p.Email,
        Role = p.Role,
        CreatedAt = p.CreatedAt
    };

    /// <summary>Get the authenticated user's own profile.</summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var profile = await GetOrProvisionCurrentProfileAsync();
        return Ok(ToDto(profile));
    }

    /// <summary>Update the authenticated user's own profile.</summary>
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileDto dto)
    {
        var profile = await GetOrProvisionCurrentProfileAsync();
        profile.FullName = dto.FullName;
        profile.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
        return Ok(ToDto(profile));
    }

    /// <summary>Admin only: list every user profile.</summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll()
    {
        var profiles = await _dbContext.UserProfiles
            .OrderBy(p => p.CreatedAt)
            .ToListAsync();

        return Ok(profiles.Select(ToDto));
    }

    /// <summary>Admin only: get a specific user's profile by id.</summary>
    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetById(int id)
    {
        var profile = await _dbContext.UserProfiles.FirstOrDefaultAsync(p => p.Id == id);
        if (profile is null) return NotFound(new { message = "User not found." });
        return Ok(ToDto(profile));
    }

    /// <summary>Admin only: delete a user's profile record.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var profile = await _dbContext.UserProfiles.FirstOrDefaultAsync(p => p.Id == id);
        if (profile is null) return NotFound(new { message = "User not found." });

        _dbContext.UserProfiles.Remove(profile);
        await _dbContext.SaveChangesAsync();
        return Ok(new { message = "User profile deleted." });
    }

    /// <summary>
    /// Admin only: change a user's denormalized role in UserService.
    /// Note: this does NOT change the role in AuthService's Identity store —
    /// use AuthService's admin endpoints (or extend this with an inter-service
    /// call) to keep both in sync in a production system.
    /// </summary>
    [HttpPatch("{id:int}/role")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ChangeRole(int id, [FromBody] ChangeRoleDto dto)
    {
        if (dto.Role is not ("Admin" or "Manager" or "Employee"))
        {
            return BadRequest(new { message = "Role must be 'Admin', 'Manager', or 'Employee'." });
        }

        var profile = await _dbContext.UserProfiles.FirstOrDefaultAsync(p => p.Id == id);
        if (profile is null) return NotFound(new { message = "User not found." });

        profile.Role = dto.Role;
        profile.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        return Ok(ToDto(profile));
    }
}
