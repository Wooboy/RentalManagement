using Microsoft.EntityFrameworkCore;
using RentalManager.Api.Common;
using RentalManager.Api.Data;
using RentalManager.Api.Dtos;
using RentalManager.Api.Models;

namespace RentalManager.Api.Services;

public class RoomService(AppDbContext db)
{
    public async Task<List<RoomResponse>> GetAllAsync(int? propertyUnitId)
    {
        var query = db.PropertyRooms.Include(x => x.PropertyUnit).AsQueryable();
        if (propertyUnitId.HasValue) query = query.Where(x => x.PropertyUnitId == propertyUnitId.Value);
        var items = await query.OrderBy(x => x.PropertyUnitId).ThenBy(x => x.Name).ThenBy(x => x.Id).ToListAsync();
        return items.Select(RoomResponse.From).ToList();
    }

    public async Task<RoomResponse> CreateAsync(RoomUpsertRequest request)
    {
        await ValidateAsync(request);

        var item = new PropertyRoom
        {
            PropertyUnitId = request.PropertyUnitId,
            Code = string.IsNullOrWhiteSpace(request.Code) ? $"R{DateTime.UtcNow:yyyyMMddHHmmss}" : request.Code.Trim(),
            Name = request.Name,
            Notes = request.Notes,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        db.PropertyRooms.Add(item);
        await db.SaveChangesAsync();
        return RoomResponse.From(item);
    }

    public async Task<RoomResponse> UpdateAsync(int id, RoomUpsertRequest request)
    {
        var item = await db.PropertyRooms.FindAsync(id) ?? throw new DomainNotFoundException();
        await ValidateAsync(request);

        item.PropertyUnitId = request.PropertyUnitId;
        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            item.Code = request.Code.Trim();
        }
        item.Name = request.Name;
        item.Notes = request.Notes;
        item.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return RoomResponse.From(item);
    }

    public async Task DeleteAsync(int id)
    {
        var item = await db.PropertyRooms.FindAsync(id) ?? throw new DomainNotFoundException();
        db.PropertyRooms.Remove(item);
        await db.SaveChangesAsync();
    }

    private async Task ValidateAsync(RoomUpsertRequest request)
    {
        if (request.PropertyUnitId <= 0) throw new DomainValidationException("請選擇房源");
        if (!await db.PropertyUnits.AnyAsync(x => x.Id == request.PropertyUnitId)) throw new DomainValidationException("房源不存在");
        if (string.IsNullOrWhiteSpace(request.Name)) throw new DomainValidationException("請輸入房間名稱");
    }
}
