using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace RentalManager.Api.Services;

public interface IJwtService
{
    string GenerateToken(int userId, string username);
}

public class JwtService(IConfiguration configuration) : IJwtService
{
    public string GenerateToken(int userId, string username)
    {
        var key = configuration["Jwt:Key"] ?? "super-secret-key-change-me";
        var issuer = configuration["Jwt:Issuer"] ?? "RentalManager";
        var audience = configuration["Jwt:Audience"] ?? "RentalManagerClient";

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, username)
        };

        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(issuer, audience, claims, expires: DateTime.UtcNow.AddHours(12), signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
