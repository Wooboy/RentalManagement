using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalManager.Api.Data;
using RentalManager.Api.Models;

namespace RentalManager.Api.Controllers;

public record AdminUserUpsertRequest(string Username, string? Password, int Role);

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin-users")]
public class AdminUsersController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetAll([FromQuery] string? keyword)
    {
        var query = db.AdminUsers.AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(x => x.Username.Contains(keyword));
        }

        var rows = await query
            .OrderBy(x => x.Username)
            .Select(x => new { x.Id, x.Username, Role = (int)x.Role, x.CreatedAtUtc })
            .ToListAsync();
        return Ok(rows);
    }

    [HttpPost]
    public async Task<ActionResult<object>> Create(AdminUserUpsertRequest model)
    {
        if (string.IsNullOrWhiteSpace(model.Username)) return BadRequest("請輸入管理員帳號");
        if (string.IsNullOrWhiteSpace(model.Password)) return BadRequest("請輸入管理員密碼");

        var username = model.Username.Trim();
        if (await db.AdminUsers.AnyAsync(x => x.Username == username)) return BadRequest("帳號已存在");

        var item = new AdminUser
        {
            Username = username,
            Role = Enum.IsDefined(typeof(UserRoleType), model.Role) ? (UserRoleType)model.Role : UserRoleType.Admin,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
            CreatedAtUtc = DateTime.UtcNow
        };
        db.AdminUsers.Add(item);
        await db.SaveChangesAsync();

        return Ok(new { item.Id, item.Username, Role = (int)item.Role, item.CreatedAtUtc });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<object>> Update(int id, AdminUserUpsertRequest model)
    {
        var item = await db.AdminUsers.FindAsync(id);
        if (item is null) return NotFound();
        if (string.IsNullOrWhiteSpace(model.Username)) return BadRequest("請輸入管理員帳號");

        var username = model.Username.Trim();
        var duplicated = await db.AdminUsers.AnyAsync(x => x.Id != id && x.Username == username);
        if (duplicated) return BadRequest("帳號已存在");

        item.Username = username;
        item.Role = Enum.IsDefined(typeof(UserRoleType), model.Role) ? (UserRoleType)model.Role : UserRoleType.Admin;
        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            item.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
        }

        await db.SaveChangesAsync();
        return Ok(new { item.Id, item.Username, Role = (int)item.Role, item.CreatedAtUtc });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.AdminUsers.FindAsync(id);
        if (item is null) return NotFound();

        var currentUserIdText = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (int.TryParse(currentUserIdText, out var currentUserId) && currentUserId == id)
        {
            return BadRequest("不可刪除目前登入的管理員帳號");
        }

        var total = await db.AdminUsers.CountAsync();
        if (total <= 1) return BadRequest("至少需保留一位管理員");

        db.AdminUsers.Remove(item);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
