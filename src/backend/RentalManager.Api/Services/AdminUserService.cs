using Microsoft.EntityFrameworkCore;
using RentalManager.Api.Common;
using RentalManager.Api.Data;
using RentalManager.Api.Dtos;
using RentalManager.Api.Models;

namespace RentalManager.Api.Services;

public class AdminUserService(AppDbContext db)
{
    public async Task<List<AdminUserResponse>> GetAllAsync(string? keyword)
    {
        var query = db.AdminUsers.AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(x => x.Username.Contains(keyword));
        }

        return await query
            .OrderBy(x => x.Username)
            .Select(x => new AdminUserResponse(x.Id, x.Username, (int)x.Role, x.CreatedAtUtc))
            .ToListAsync();
    }

    public async Task<AdminUserResponse> CreateAsync(AdminUserUpsertRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username)) throw new DomainValidationException("請輸入管理員帳號");
        if (string.IsNullOrWhiteSpace(request.Password)) throw new DomainValidationException("請輸入管理員密碼");

        var username = request.Username.Trim();
        if (await db.AdminUsers.AnyAsync(x => x.Username == username)) throw new DomainValidationException("帳號已存在");

        var item = new AdminUser
        {
            Username = username,
            Role = Enum.IsDefined(typeof(UserRoleType), request.Role) ? (UserRoleType)request.Role : UserRoleType.Admin,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            CreatedAtUtc = DateTime.UtcNow
        };
        db.AdminUsers.Add(item);
        await db.SaveChangesAsync();

        return new AdminUserResponse(item.Id, item.Username, (int)item.Role, item.CreatedAtUtc);
    }

    public async Task<AdminUserResponse> UpdateAsync(int id, AdminUserUpsertRequest request)
    {
        var item = await db.AdminUsers.FindAsync(id) ?? throw new DomainNotFoundException();
        if (string.IsNullOrWhiteSpace(request.Username)) throw new DomainValidationException("請輸入管理員帳號");

        var username = request.Username.Trim();
        var duplicated = await db.AdminUsers.AnyAsync(x => x.Id != id && x.Username == username);
        if (duplicated) throw new DomainValidationException("帳號已存在");

        item.Username = username;
        item.Role = Enum.IsDefined(typeof(UserRoleType), request.Role) ? (UserRoleType)request.Role : UserRoleType.Admin;
        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            item.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        }

        await db.SaveChangesAsync();
        return new AdminUserResponse(item.Id, item.Username, (int)item.Role, item.CreatedAtUtc);
    }

    public async Task DeleteAsync(int id, int currentUserId)
    {
        var item = await db.AdminUsers.FindAsync(id) ?? throw new DomainNotFoundException();

        if (currentUserId == id)
        {
            throw new DomainValidationException("不可刪除目前登入的管理員帳號");
        }

        var total = await db.AdminUsers.CountAsync();
        if (total <= 1) throw new DomainValidationException("至少需保留一位管理員");

        db.AdminUsers.Remove(item);
        await db.SaveChangesAsync();
    }
}
