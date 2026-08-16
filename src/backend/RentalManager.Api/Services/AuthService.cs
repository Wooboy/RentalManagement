using Microsoft.EntityFrameworkCore;
using RentalManager.Api.Common;
using RentalManager.Api.Data;
using RentalManager.Api.Dtos;

namespace RentalManager.Api.Services;

public class AuthService(AppDbContext db, IJwtService jwtService)
{
    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Username == request.Username);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new DomainUnauthorizedException("帳號或密碼錯誤");
        }
        if (!user.IsActive)
        {
            throw new DomainUnauthorizedException("帳號已停用");
        }

        var token = jwtService.GenerateToken(user);
        return new LoginResponse(token, user.Id, user.Username, (int)user.Role, user.TenantId, user.DisplayName);
    }

    public async Task ChangePasswordAsync(int currentUserId, ChangePasswordRequest request)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == currentUserId)
            ?? throw new DomainUnauthorizedException();

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new DomainValidationException("目前密碼錯誤");
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            throw new DomainValidationException("請輸入新密碼");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await db.SaveChangesAsync();
    }
}
