using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalManager.Api.Data;
using RentalManager.Api.Models;

namespace RentalManager.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/properties")]
public class PropertiesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PropertyUnit>>> GetAll([FromQuery] string? keyword)
    {
        var query = db.PropertyUnits.AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(x => x.Code.Contains(keyword) || x.Name.Contains(keyword) || x.Address.Contains(keyword));
        }

        return Ok(await query.OrderByDescending(x => x.Id).ToListAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PropertyUnit>> GetById(int id)
    {
        var item = await db.PropertyUnits.FindAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<PropertyUnit>> Create(PropertyUnit model)
    {
        model.CreatedAtUtc = DateTime.UtcNow;
        model.UpdatedAtUtc = DateTime.UtcNow;
        db.PropertyUnits.Add(model);
        await db.SaveChangesAsync();
        return Ok(model);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<PropertyUnit>> Update(int id, PropertyUnit model)
    {
        var item = await db.PropertyUnits.FindAsync(id);
        if (item is null) return NotFound();

        item.Code = model.Code;
        item.Name = model.Name;
        item.Address = model.Address;
        item.Notes = model.Notes;
        item.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Ok(item);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.PropertyUnits.FindAsync(id);
        if (item is null) return NotFound();

        db.PropertyUnits.Remove(item);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
