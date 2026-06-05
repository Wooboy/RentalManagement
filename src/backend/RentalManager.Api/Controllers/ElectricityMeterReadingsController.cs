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
        if (model.PropertyUnitId <= 0) return BadRequest("請選擇房源");
        if (model.PropertyRoomId <= 0) return BadRequest("請選擇房間");
        if (model.ReadingValue < 0) return BadRequest("度數不可小於 0");

        var room = await db.PropertyRooms.FirstOrDefaultAsync(x => x.Id == model.PropertyRoomId);
        if (room is null) return BadRequest("房間不存在");
        if (room.PropertyUnitId != model.PropertyUnitId) return BadRequest("房間不屬於所選房源");

        var item = new ElectricityMeterReading
        {
            PropertyUnitId = model.PropertyUnitId,
            PropertyRoomId = model.PropertyRoomId,
            ReadingDateUtc = model.ReadingDateUtc,
            ReadingValue = model.ReadingValue,
            Notes = model.Notes,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.ElectricityMeterReadings.Add(item);
        await db.SaveChangesAsync();
        return Ok(item);
    }
}
