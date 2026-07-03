using Microsoft.EntityFrameworkCore;
using RentalManager.Api.Common;
using RentalManager.Api.Data;
using RentalManager.Api.Dtos;
using RentalManager.Api.Models;

namespace RentalManager.Api.Services;

public class TenantService(AppDbContext db)
{
    public async Task<List<TenantResponse>> GetAllAsync(string? keyword)
    {
        var query = db.Tenants.AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(x => x.Name.Contains(keyword) || (x.Phone != null && x.Phone.Contains(keyword)));
        }

        var items = await query.OrderBy(x => x.Name).ThenBy(x => x.Id).ToListAsync();
        return items.Select(TenantResponse.From).ToList();
    }

    public async Task<TenantResponse> GetByIdAsync(int id)
    {
        var item = await db.Tenants.FindAsync(id) ?? throw new DomainNotFoundException();
        return TenantResponse.From(item);
    }

    public async Task<TenantResponse> CreateAsync(TenantUpsertRequest request)
    {
        var item = new Tenant
        {
            Type = request.Type,
            Name = request.Name,
            BirthdayUtc = request.BirthdayUtc,
            TaxId = request.TaxId,
            PersonalId = request.PersonalId,
            Phone = request.Phone,
            Email = request.Email,
            Address = request.Address,
            EmergencyContactName = request.EmergencyContactName,
            EmergencyContactPhone = request.EmergencyContactPhone,
            Notes = request.Notes,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        db.Tenants.Add(item);
        await db.SaveChangesAsync();
        return TenantResponse.From(item);
    }

    public async Task<TenantResponse> UpdateAsync(int id, TenantUpsertRequest request)
    {
        var item = await db.Tenants.FindAsync(id) ?? throw new DomainNotFoundException();

        item.Type = request.Type;
        item.Name = request.Name;
        item.BirthdayUtc = request.BirthdayUtc;
        item.TaxId = request.TaxId;
        item.PersonalId = request.PersonalId;
        item.Phone = request.Phone;
        item.Email = request.Email;
        item.Address = request.Address;
        item.EmergencyContactName = request.EmergencyContactName;
        item.EmergencyContactPhone = request.EmergencyContactPhone;
        item.Notes = request.Notes;
        item.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return TenantResponse.From(item);
    }

    public async Task DeleteAsync(int id)
    {
        var item = await db.Tenants.FindAsync(id) ?? throw new DomainNotFoundException();
        db.Tenants.Remove(item);
        await db.SaveChangesAsync();
    }
}
