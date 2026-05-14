using Mesa_Mohloane_Backend.Models.Entities;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AppClaimTypes = Mesa_Mohloane_Backend.Helpers.ClaimTypes;

namespace Mesa_Mohloane_Backend.Helpers;

public class JwtHelper(IConfiguration config)
{
    private readonly IConfiguration _config = config;

    public DateTime GetExpiry()
    {
        var expiryHours = _config.GetValue<int?>("Jwt:ExpiryHours") ?? 60;
        return DateTime.UtcNow.AddHours(expiryHours);
    }

    public string GenerateToken(User user)
    {
        var roleName = user.Role?.Name ?? string.Empty;
        var claims = new[]
        {
            new Claim(AppClaimTypes.UserId, user.Id.ToString()),
            new Claim(AppClaimTypes.Role, roleName),
            new Claim(AppClaimTypes.Email, user.Email),
            new Claim(AppClaimTypes.FullName, $"{user.FirstName} {user.LastName}"),
            new Claim(System.Security.Claims.ClaimTypes.Role, roleName)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        string? jwtIssuer = _config["Jwt:Issuer"];
        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: GetExpiry(),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}