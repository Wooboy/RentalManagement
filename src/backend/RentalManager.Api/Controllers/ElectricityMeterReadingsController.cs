using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalManager.Api.Data;
using RentalManager.Api.Models;

namespace RentalManager.Api.Controllers;

public record ElectricityMeterReadingUpsertRequest(
    int PropertyUnitId,
    int PropertyRoomId,
    DateTime ReadingDateUtc,
    decimal ReadingValue,
    string? Notes
);

[ApiController]
[Authorize]
[Route("api/electricity-meter-readings")]
public class ElectricityMeterReadingsController(AppDbContext db) : ControllerBase
{
    [HttpGet("latest-by-room")]
    public async Task<ActionResult<IEnumerable<object>>> GetLatestByRoom([FromQuery] int? propertyUnitId)
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

        var rows = rooms.Select(r =>
        {
            map.TryGetValue(r.Id, out var m);
            return new
            {
                PropertyUnitId = r.PropertyUnitId,
                PropertyUnitName = r.PropertyUnit != null ? r.PropertyUnit.Name : null,
                PropertyRoomId = r.Id,
                PropertyRoomName = r.Name,
                LastReadingDateUtc = m?.ReadingDateUtc,
                LastReadingValue = m?.ReadingValue
            };
        });

        return Ok(rows);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetAll([FromQuery] int? propertyUnitId, [FromQuery] int? propertyRoomId)
    {
        var query = db.ElectricityMeterReadings
            .Include(x => x.PropertyUnit)
            .Include(x => x.PropertyRoom)
            .AsQueryable();
        if (propertyUnitId.HasValue) query = query.Where(x => x.PropertyUnitId == propertyUnitId.Value);
        if (propertyRoomId.HasValue) query = query.Where(x => x.PropertyRoomId == propertyRoomId.Value);

        var rows = await query
            .OrderByDescending(x => x.ReadingDateUtc)
            .ThenByDescending(x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.PropertyUnitId,
                PropertyUnitName = x.PropertyUnit != null ? x.PropertyUnit.Name : null,
                x.PropertyRoomId,
                PropertyRoomName = x.PropertyRoom != null ? x.PropertyRoom.Name : null,
                x.ReadingDateUtc,
                x.ReadingValue,
                x.Notes,
                x.CreatedAtUtc
            })
            .ToListAsync();
        return Ok(rows);
    }

    [HttpPost]
    public async Task<ActionResult<object>> Create(ElectricityMeterReadingUpsertRequest model)
    {
        var validationError = await ValidateAsync(model);
        if (validationError is not null) return BadRequest(validationError);

        var exists = await db.ElectricityMeterReadings.AnyAsync(x =>
            x.PropertyRoomId == model.PropertyRoomId &&
            x.ReadingDateUtc.Date == model.ReadingDateUtc.Date);
        if (exists) return BadRequest("同一房間在同一天已有抄表記錄，請改用編輯");

        var item = new ElectricityMeterReading
        {
            PropertyUnitId = model.PropertyUnitId,
            PropertyRoomId = model.PropertyRoomId,
            ReadingDateUtc = model.ReadingDateUtc,
            ReadingValue = model.ReadingValue,
            Notes = NormalizeNotes(model.Notes),
            CreatedAtUtc = DateTime.UtcNow
        };
        db.ElectricityMeterReadings.Add(item);
        await db.SaveChangesAsync();
        return Ok(item);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<object>> Update(int id, ElectricityMeterReadingUpsertRequest model)
    {
        var item = await db.ElectricityMeterReadings.FindAsync(id);
        if (item is null) return NotFound("抄表記錄不存在");

        var validationError = await ValidateAsync(model);
        if (validationError is not null) return BadRequest(validationError);

        var duplicate = await db.ElectricityMeterReadings.AnyAsync(x =>
            x.Id != id &&
            x.PropertyRoomId == model.PropertyRoomId &&
            x.ReadingDateUtc.Date == model.ReadingDateUtc.Date);
        if (duplicate) return BadRequest("同一房間在同一天已有其他抄表記錄");

        item.PropertyUnitId = model.PropertyUnitId;
        item.PropertyRoomId = model.PropertyRoomId;
        item.ReadingDateUtc = model.ReadingDateUtc;
        item.ReadingValue = model.ReadingValue;
        item.Notes = NormalizeNotes(model.Notes);

        await db.SaveChangesAsync();
        return Ok(item);
    }

    private async Task<string?> ValidateAsync(ElectricityMeterReadingUpsertRequest model)
    {
        if (model.PropertyUnitId <= 0) return "請選擇房源";
        if (model.PropertyRoomId <= 0) return "請選擇房間";
        if (model.ReadingValue < 0) return "抄表度數不可小於 0";

        var room = await db.PropertyRooms.FirstOrDefaultAsync(x => x.Id == model.PropertyRoomId);
        if (room is null) return "房間不存在";
        if (room.PropertyUnitId != model.PropertyUnitId) return "所選房間不屬於指定房源";

        return null;
    }

    private static string? NormalizeNotes(string? notes)
        => string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
}
