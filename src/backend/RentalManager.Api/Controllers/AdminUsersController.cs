using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalManager.Api.Dtos;
using RentalManager.Api.Services;

namespace RentalManager.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin-users")]
public class AdminUsersController(AdminUserService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AdminUserResponse>>> GetAll([FromQuery] string? keyword)
        => Ok(await service.GetAllAsync(keyword));

    [HttpPost]
    public async Task<ActionResult<AdminUserResponse>> Create(AdminUserUpsertRequest request)
        => Ok(await service.CreateAsync(request));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AdminUserResponse>> Update(int id, AdminUserUpsertRequest request)
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
