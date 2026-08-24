using AuthService.Application.DTOs;
using AuthService.Application.Services;
using AuthService.Domain.Entities;
using AuthService.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly AuthDbContext _dbContext;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        AuthDbContext dbContext)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Public registration endpoint. Assigns the role provided by the caller.
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (dto.Role is not ("Admin" or "Manager" or "Employee"))
        {
            return BadRequest(new { message = "Role must be 'Admin', 'Manager', or 'Employee'." });
        }

        var existing = await _userManager.FindByEmailAsync(dto.Email);
        if (existing is not null)
        {
            return Conflict(new { message = "A user with this email already exists." });
        }

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FullName = dto.FullName
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        await _userManager.AddToRoleAsync(user, dto.Role);

        return Ok(new
        {
            message = "Registration successful. You can now log in.",
            userId = user.Id,
            email = user.Email,
            fullName = user.FullName,
            role = dto.Role
        });
    }

    /// <summary>
    /// Admin-only endpoint to create additional Admin accounts.
    /// Requires a valid Admin JWT.
    /// </summary>
    [HttpPost("register-admin")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RegisterAdmin([FromBody] RegisterDto dto)
    {
        var existing = await _userManager.FindByEmailAsync(dto.Email);
        if (existing is not null)
        {
            return Conflict(new { message = "A user with this email already exists." });
        }

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FullName = dto.FullName
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        await _userManager.AddToRoleAsync(user, "Admin");

        return Ok(new { message = "Admin account created successfully." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user is null || !user.IsActive)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        var validPassword = await _userManager.CheckPasswordAsync(user, dto.Password);
        if (!validPassword)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var accessTokenExpiresAt = _tokenService.GetTokenExpiry(accessToken) ?? _tokenService.GetAccessTokenExpiry();
        var refreshTokenValue = _tokenService.GenerateRefreshToken();

        _dbContext.AccessTokenAudits.Add(new AccessTokenAudit
        {
            UserId = user.Id,
            Token = accessToken,
            ExpiresAt = accessTokenExpiresAt,
            IssuedAt = DateTime.UtcNow
        });

        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAt = _tokenService.GetRefreshTokenExpiry()
        });
        await _dbContext.SaveChangesAsync();

        return Ok(new TokenResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenValue,
            AccessTokenExpiresAt = accessTokenExpiresAt,
            Role = roles.FirstOrDefault() ?? "Employee",
            Email = user.Email ?? string.Empty,
            FullName = user.FullName
        });
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto dto)
    {
        var existingToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == dto.RefreshToken);

        if (existingToken is null || !existingToken.IsActive)
        {
            return Unauthorized(new { message = "Invalid or expired refresh token." });
        }

        var user = await _userManager.FindByIdAsync(existingToken.UserId.ToString());
        if (user is null || !user.IsActive)
        {
            return Unauthorized(new { message = "User no longer exists or is inactive." });
        }

        // Revoke the old refresh token (rotation) and issue new tokens.
        existingToken.IsRevoked = true;

        var roles = await _userManager.GetRolesAsync(user);
        var newAccessToken = _tokenService.GenerateAccessToken(user, roles);
        var newAccessTokenExpiresAt = _tokenService.GetTokenExpiry(newAccessToken) ?? _tokenService.GetAccessTokenExpiry();
        var newRefreshTokenValue = _tokenService.GenerateRefreshToken();

        _dbContext.AccessTokenAudits.Add(new AccessTokenAudit
        {
            UserId = user.Id,
            Token = newAccessToken,
            ExpiresAt = newAccessTokenExpiresAt,
            IssuedAt = DateTime.UtcNow
        });

        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = newRefreshTokenValue,
            ExpiresAt = _tokenService.GetRefreshTokenExpiry()
        });

        await _dbContext.SaveChangesAsync();

        return Ok(new TokenResponseDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshTokenValue,
            AccessTokenExpiresAt = newAccessTokenExpiresAt,
            Role = roles.FirstOrDefault() ?? "Employee",
            Email = user.Email ?? string.Empty,
            FullName = user.FullName
        });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto dto)
    {
        var hasChanges = false;

        var bearer = Request.Headers.Authorization.ToString();
        var accessToken = bearer.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? bearer[7..].Trim()
            : string.Empty;

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            var auditToken = await _dbContext.AccessTokenAudits
                .FirstOrDefaultAsync(at => at.Token == accessToken);

            if (auditToken is not null && !auditToken.IsRevoked)
            {
                auditToken.IsRevoked = true;
                auditToken.RevokedAt = DateTime.UtcNow;
                hasChanges = true;
            }
        }

        var existingToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == dto.RefreshToken);

        if (existingToken is not null)
        {
            existingToken.IsRevoked = true;
            hasChanges = true;
        }

        if (hasChanges)
        {
            await _dbContext.SaveChangesAsync();
        }

        return Ok(new { message = "Logged out successfully." });
    }
}
