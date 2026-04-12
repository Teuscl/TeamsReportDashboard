using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TeamsReportDashboard.Backend.Models.Configuration;
using TeamsReportDashboard.Entities;
using TeamsReportDashboard.Interfaces;

namespace TeamsReportDashboard.Services;

public class TokenService : ITokenService
{
    private readonly JwtSettings _jwtSettings;

    public TokenService(IOptions<JwtSettings> jwtOptions)
    {
        _jwtSettings = jwtOptions.Value;
    }

    public string GenerateToken(Entities.User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = _jwtSettings.Key;
        
        var claims = new List<Claim>()
        {
            new Claim("id", user.Id.ToString()),
            new Claim("name", user.Name),
            new Claim("role", user.Role.ToString()),
            new Claim("email", user.Email)
        };
        
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "TeamsReportDashboard",
            audience: "TeamsReportDashboard",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: credentials
        );

        return tokenHandler.WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}