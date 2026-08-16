using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalManager.Api.Dtos;
using RentalManager.Api.Services;

namespace RentalManager.Api.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/users")]
public class UsersController(UserService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserResponse>>> GetAll([FromQuery] string? keyword)
        => Ok(await service.GetAllAsync(keyword));

    [HttpPost]
    public async Task<ActionResult<UserResponse>> Create(UserUpsertRequest request)
        => Ok(await service.CreateAsync(request));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserResponse>> Update(int id, UserUpsertRequest request)
        => Ok(await service.UpdateAsync(id, request));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var currentUserIdText = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        int.TryParse(currentUserIdText, out var currentUserId);

        await service.DeleteAsync(id, currentUserId);
        return NoContent();
    }
}
