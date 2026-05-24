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

        var token = jwtService.GenerateToken(user.Id, user.Username);
        return Ok(new LoginResponse(token, user.Username));
    }
}
