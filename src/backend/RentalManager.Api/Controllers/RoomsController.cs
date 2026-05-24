using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalManager.Api.Data;
using RentalManager.Api.Models;

namespace RentalManager.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/rooms")]
public class RoomsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PropertyRoom>>> GetAll([FromQuery] int? propertyUnitId)
    {
        var query = db.PropertyRooms.Include(x => x.PropertyUnit).AsQueryable();
        if (propertyUnitId.HasValue) query = query.Where(x => x.PropertyUnitId == propertyUnitId.Value);
        return Ok(await query.OrderBy(x => x.PropertyUnitId).ThenBy(x => x.Name).ThenBy(x => x.Id).ToListAsync());
    }

    [HttpPost]
    public async Task<ActionResult<PropertyRoom>> Create(PropertyRoom model)
    {
        if (model.PropertyUnitId <= 0) return BadRequest("請選擇房源");
        if (!await db.PropertyUnits.AnyAsync(x => x.Id == model.PropertyUnitId)) return BadRequest("房源不存在");
        if (string.IsNullOrWhiteSpace(model.Name)) return BadRequest("請輸入房間名稱");

        model.Code = string.IsNullOrWhiteSpace(model.Code) ? $"R{DateTime.UtcNow:yyyyMMddHHmmss}" : model.Code.Trim();

        model.CreatedAtUtc = DateTime.UtcNow;
        model.UpdatedAtUtc = DateTime.UtcNow;
        db.PropertyRooms.Add(model);
        await db.SaveChangesAsync();
        return Ok(model);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<PropertyRoom>> Update(int id, PropertyRoom model)
    {
        var item = await db.PropertyRooms.FindAsync(id);
        if (item is null) return NotFound();
        if (model.PropertyUnitId <= 0) return BadRequest("請選擇房源");
        if (!await db.PropertyUnits.AnyAsync(x => x.Id == model.PropertyUnitId)) return BadRequest("房源不存在");
        if (string.IsNullOrWhiteSpace(model.Name)) return BadRequest("請輸入房間名稱");

        item.PropertyUnitId = model.PropertyUnitId;
        if (!string.IsNullOrWhiteSpace(model.Code))
        {
            item.Code = model.Code.Trim();
        }
        item.Name = model.Name;
        item.Notes = model.Notes;
        item.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(item);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.PropertyRooms.FindAsync(id);
        if (item is null) return NotFound();
        db.PropertyRooms.Remove(item);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
