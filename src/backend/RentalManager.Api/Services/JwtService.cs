using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using RentalManager.Api.Models;

namespace RentalManager.Api.Services;

public interface IJwtService
{
    string GenerateToken(AppUser user);
}

public class JwtService(IConfiguration configuration) : IJwtService
{
    public string GenerateToken(AppUser user)
    {
        var key = configuration["Jwt:Key"] ?? "super-secret-key-change-me";
        var issuer = configuration["Jwt:Issuer"] ?? "RentalManager";
        var audience = configuration["Jwt:Audience"] ?? "RentalManagerClient";

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new(ClaimTypes.Role, user.Role == UserRoleType.Admin ? "Admin" : "Tenant")
        };
        if (user.TenantId.HasValue)
        {
            claims.Add(new Claim("tenantId", user.TenantId.Value.ToString()));
        }

        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(issuer, audience, claims, expires: DateTime.UtcNow.AddHours(12), signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
