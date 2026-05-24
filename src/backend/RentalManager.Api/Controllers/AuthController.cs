using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalManager.Api.Data;
using RentalManager.Api.Dtos;
using RentalManager.Api.Services;

namespace RentalManager.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AppDbContext db, IJwtService jwtService) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var user = await db.AdminUsers.FirstOrDefaultAsync(x => x.Username == request.Username);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized("帳號或密碼錯誤");
        }

        var token = jwtService.GenerateToken(user.Id, user.Username, (int)user.Role);
        return Ok(new LoginResponse(token, user.Id, user.Username, (int)user.Role));
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        var currentUserIdText = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!int.TryParse(currentUserIdText, out var currentUserId)) return Unauthorized();

        var user = await db.AdminUsers.FirstOrDefaultAsync(x => x.Id == currentUserId);
        if (user is null) return Unauthorized();
        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
        {
            return BadRequest("目前密碼錯誤");
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return BadRequest("請輸入新密碼");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
