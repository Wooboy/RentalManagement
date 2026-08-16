using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalManager.Api.Dtos;
using RentalManager.Api.Services;

namespace RentalManager.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AuthService service) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
        => Ok(await service.LoginAsync(request));

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        var currentUserIdText = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!int.TryParse(currentUserIdText, out var currentUserId)) return Unauthorized();

        await service.ChangePasswordAsync(currentUserId, request);
        return NoContent();
    }
}
