using Microsoft.EntityFrameworkCore;
using RentalManager.Api.Common;
using RentalManager.Api.Data;
using RentalManager.Api.Dtos;
using RentalManager.Api.Models;

namespace RentalManager.Api.Services;

public class MeterReadingService(AppDbContext db)
{
    public async Task<List<MeterReadingLatestByRoomResponse>> GetLatestByRoomAsync(int? propertyUnitId)
    {
        var roomsQuery = db.PropertyRooms.Include(x => x.PropertyUnit).AsQueryable();
        if (propertyUnitId.HasValue) roomsQuery = roomsQuery.Where(x => x.PropertyUnitId == propertyUnitId.Value);

        var rooms = await roomsQuery.OrderBy(x => x.PropertyUnitId).ThenBy(x => x.Name).ThenBy(x => x.Id).ToListAsync();
        var roomIds = rooms.Select(x => x.Id).ToList();

        var latestReadings = await db.ElectricityMeterReadings
            .Where(x => roomIds.Contains(x.PropertyRoomId))
            .GroupBy(x => x.PropertyRoomId)
            .Select(g => g.OrderByDescending(x => x.ReadingDateUtc).ThenByDescending(x => x.Id).First())
            .ToListAsync();
        var map = latestReadings.ToDictionary(x => x.PropertyRoomId, x => x);

        return rooms.Select(r =>
        {
            map.TryGetValue(r.Id, out var m);
            return new MeterReadingLatestByRoomResponse(
                r.PropertyUnitId,
                r.PropertyUnit?.Name,
                r.Id,
                r.Name,
                m?.ReadingDateUtc,
                m?.ReadingValue);
        }).ToList();
    }

    public async Task<List<MeterReadingListItemResponse>> GetAllAsync(int? propertyUnitId, int? propertyRoomId)
    {
        var query = db.ElectricityMeterReadings
            .Include(x => x.PropertyUnit)
            .Include(x => x.PropertyRoom)
            .AsQueryable();
        if (propertyUnitId.HasValue) query = query.Where(x => x.PropertyUnitId == propertyUnitId.Value);
        if (propertyRoomId.HasValue) query = query.Where(x => x.PropertyRoomId == propertyRoomId.Value);

        return await query
            .OrderByDescending(x => x.ReadingDateUtc)
            .ThenByDescending(x => x.Id)
            .Select(x => new MeterReadingListItemResponse(
                x.Id,
                x.PropertyUnitId,
                x.PropertyUnit != null ? x.PropertyUnit.Name : null,
                x.PropertyRoomId,
                x.PropertyRoom != null ? x.PropertyRoom.Name : null,
                x.ReadingDateUtc,
                x.ReadingValue,
                x.Notes,
                x.CreatedAtUtc))
            .ToListAsync();
    }

    public async Task<MeterReadingResponse> CreateAsync(ElectricityMeterReadingUpsertRequest request)
    {
        await ValidateAsync(request);

        var exists = await db.ElectricityMeterReadings.AnyAsync(x =>
            x.PropertyRoomId == request.PropertyRoomId &&
            x.ReadingDateUtc.Date == request.ReadingDateUtc.Date);
        if (exists) throw new DomainValidationException("同一房間在同一天已有抄表記錄，請改用編輯");

        var item = new ElectricityMeterReading
        {
            PropertyUnitId = request.PropertyUnitId,
            PropertyRoomId = request.PropertyRoomId,
            ReadingDateUtc = request.ReadingDateUtc,
            ReadingValue = request.ReadingValue,
            Notes = NormalizeNotes(request.Notes),
            CreatedAtUtc = DateTime.UtcNow
        };
        db.ElectricityMeterReadings.Add(item);
        await db.SaveChangesAsync();
        return MeterReadingResponse.From(item);
    }

    public async Task<MeterReadingResponse> UpdateAsync(int id, ElectricityMeterReadingUpsertRequest request)
    {
        var item = await db.ElectricityMeterReadings.FindAsync(id)
            ?? throw new DomainNotFoundException("抄表記錄不存在");

        await ValidateAsync(request);

        var duplicate = await db.ElectricityMeterReadings.AnyAsync(x =>
            x.Id != id &&
            x.PropertyRoomId == request.PropertyRoomId &&
            x.ReadingDateUtc.Date == request.ReadingDateUtc.Date);
        if (duplicate) throw new DomainValidationException("同一房間在同一天已有其他抄表記錄");

        item.PropertyUnitId = request.PropertyUnitId;
        item.PropertyRoomId = request.PropertyRoomId;
        item.ReadingDateUtc = request.ReadingDateUtc;
        item.ReadingValue = request.ReadingValue;
        item.Notes = NormalizeNotes(request.Notes);

        await db.SaveChangesAsync();
        return MeterReadingResponse.From(item);
    }

    private async Task ValidateAsync(ElectricityMeterReadingUpsertRequest request)
    {
        if (request.PropertyUnitId <= 0) throw new DomainValidationException("請選擇房源");
        if (request.PropertyRoomId <= 0) throw new DomainValidationException("請選擇房間");
        if (request.ReadingValue < 0) throw new DomainValidationException("抄表度數不可小於 0");

        var room = await db.PropertyRooms.FirstOrDefaultAsync(x => x.Id == request.PropertyRoomId)
            ?? throw new DomainValidationException("房間不存在");
        if (room.PropertyUnitId != request.PropertyUnitId) throw new DomainValidationException("所選房間不屬於指定房源");
    }

    private static string? NormalizeNotes(string? notes)
        => string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
}
