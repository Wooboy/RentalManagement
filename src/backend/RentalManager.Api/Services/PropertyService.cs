using Microsoft.EntityFrameworkCore;
using RentalManager.Api.Common;
using RentalManager.Api.Data;
using RentalManager.Api.Dtos;
using RentalManager.Api.Models;

namespace RentalManager.Api.Services;

public class PropertyService(AppDbContext db)
{
    public async Task<List<PropertyResponse>> GetAllAsync(string? keyword)
    {
        var query = db.PropertyUnits.AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(x => x.Name.Contains(keyword) || x.Address.Contains(keyword));
        }

        var items = await query.OrderBy(x => x.Name).ThenBy(x => x.Id).ToListAsync();
        return items.Select(PropertyResponse.From).ToList();
    }

    public async Task<PropertyResponse> GetByIdAsync(int id)
    {
        var item = await db.PropertyUnits.FindAsync(id) ?? throw new DomainNotFoundException();
        return PropertyResponse.From(item);
    }

    public async Task<PropertyResponse> CreateAsync(PropertyUpsertRequest request)
    {
        var item = new PropertyUnit
        {
            Code = string.IsNullOrWhiteSpace(request.Code) ? $"P{DateTime.UtcNow:yyyyMMddHHmmss}" : request.Code.Trim(),
            Name = request.Name,
            Address = request.Address,
            Notes = request.Notes,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        db.PropertyUnits.Add(item);
        await db.SaveChangesAsync();
        return PropertyResponse.From(item);
    }

    public async Task<PropertyResponse> UpdateAsync(int id, PropertyUpsertRequest request)
    {
        var item = await db.PropertyUnits.FindAsync(id) ?? throw new DomainNotFoundException();

        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            item.Code = request.Code.Trim();
        }
        item.Name = request.Name;
        item.Address = request.Address;
        item.Notes = request.Notes;
        item.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return PropertyResponse.From(item);
    }

    public async Task DeleteAsync(int id)
    {
        var item = await db.PropertyUnits.FindAsync(id) ?? throw new DomainNotFoundException();
        db.PropertyUnits.Remove(item);
        await db.SaveChangesAsync();
    }
}
