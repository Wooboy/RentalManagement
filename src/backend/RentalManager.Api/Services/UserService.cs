using Microsoft.EntityFrameworkCore;
using RentalManager.Api.Common;
using RentalManager.Api.Data;
using RentalManager.Api.Dtos;
using RentalManager.Api.Models;

namespace RentalManager.Api.Services;

public class UserService(AppDbContext db)
{
    public async Task<List<UserResponse>> GetAllAsync(string? keyword)
    {
        var query = db.Users.Include(x => x.Tenant).AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(x => x.Username.Contains(keyword) || (x.DisplayName != null && x.DisplayName.Contains(keyword)));
        }

        return await query
            .OrderBy(x => x.Username)
            .Select(x => new UserResponse(
                x.Id, x.Username, (int)x.Role, x.DisplayName, x.Email,
                x.TenantId, x.Tenant != null ? x.Tenant.Name : null,
                x.IsActive, x.CreatedAtUtc))
            .ToListAsync();
    }

    public async Task<UserResponse> CreateAsync(UserUpsertRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username)) throw new DomainValidationException("請輸入帳號");
        if (string.IsNullOrWhiteSpace(request.Password)) throw new DomainValidationException("請輸入密碼");

        var username = request.Username.Trim();
        if (await db.Users.AnyAsync(x => x.Username == username)) throw new DomainValidationException("帳號已存在");

        var role = ResolveRole(request.Role);
        var tenantId = await ResolveTenantIdAsync(role, request.TenantId);

        var item = new AppUser
        {
            Username = username,
            Role = role,
            DisplayName = Normalize(request.DisplayName),
            Email = Normalize(request.Email),
            TenantId = tenantId,
            IsActive = request.IsActive,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Users.Add(item);
        await db.SaveChangesAsync();

        return await ToResponseAsync(item);
    }

    public async Task<UserResponse> UpdateAsync(int id, UserUpsertRequest request)
    {
        var item = await db.Users.FindAsync(id) ?? throw new DomainNotFoundException();
        if (string.IsNullOrWhiteSpace(request.Username)) throw new DomainValidationException("請輸入帳號");

        var username = request.Username.Trim();
        var duplicated = await db.Users.AnyAsync(x => x.Id != id && x.Username == username);
        if (duplicated) throw new DomainValidationException("帳號已存在");

        var role = ResolveRole(request.Role);
        var tenantId = await ResolveTenantIdAsync(role, request.TenantId);

        item.Username = username;
        item.Role = role;
        item.DisplayName = Normalize(request.DisplayName);
        item.Email = Normalize(request.Email);
        item.TenantId = tenantId;
        item.IsActive = request.IsActive;
        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            item.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        }

        await db.SaveChangesAsync();
        return await ToResponseAsync(item);
    }

    public async Task DeleteAsync(int id, int currentUserId)
    {
        var item = await db.Users.FindAsync(id) ?? throw new DomainNotFoundException();

        if (currentUserId == id)
        {
            throw new DomainValidationException("不可刪除目前登入的帳號");
        }

        if (item.Role == UserRoleType.Admin)
        {
            var adminCount = await db.Users.CountAsync(x => x.Role == UserRoleType.Admin);
            if (adminCount <= 1) throw new DomainValidationException("至少需保留一位管理員");
        }

        db.Users.Remove(item);
        await db.SaveChangesAsync();
    }

    private static UserRoleType ResolveRole(int role)
        => Enum.IsDefined(typeof(UserRoleType), role) ? (UserRoleType)role : UserRoleType.Admin;

    private async Task<int?> ResolveTenantIdAsync(UserRoleType role, int? tenantId)
    {
        if (role != UserRoleType.Tenant) return null;
        if (!tenantId.HasValue) throw new DomainValidationException("租客帳號必須關聯租客");
        if (!await db.Tenants.AnyAsync(x => x.Id == tenantId.Value)) throw new DomainValidationException("租客不存在");
        return tenantId;
    }

    private async Task<UserResponse> ToResponseAsync(AppUser item)
    {
        var tenantName = item.TenantId.HasValue
            ? await db.Tenants.Where(x => x.Id == item.TenantId.Value).Select(x => x.Name).FirstOrDefaultAsync()
            : null;
        return new UserResponse(
            item.Id, item.Username, (int)item.Role, item.DisplayName, item.Email,
            item.TenantId, tenantName, item.IsActive, item.CreatedAtUtc);
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
