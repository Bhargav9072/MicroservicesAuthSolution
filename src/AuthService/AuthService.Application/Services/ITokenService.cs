using System.Security.Claims;
using AuthService.Domain.Entities;

namespace AuthService.Application.Services;

public interface ITokenService
{
    string GenerateAccessToken(ApplicationUser user, IList<string> roles);
    string GetTokenJti(string accessToken);
    DateTime? GetTokenExpiry(string accessToken);
    string GenerateRefreshToken();
    DateTime GetAccessTokenExpiry();
    DateTime GetRefreshTokenExpiry();
}
