using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalManager.Api.Data;
using RentalManager.Api.Models;

namespace RentalManager.Api.Controllers;

[ApiController]
[Route("api/tenants")]
[Authorize]
public class TenantsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Tenant>>> GetAll([FromQuery] string? keyword)
    {
        var query = db.Tenants.AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(x => x.Name.Contains(keyword) || (x.Phone != null && x.Phone.Contains(keyword)));
        }

        return Ok(await query.OrderBy(x => x.Name).ThenBy(x => x.Id).ToListAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Tenant>> GetById(int id)
    {
        var item = await db.Tenants.FindAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<Tenant>> Create(Tenant model)
    {
        model.CreatedAtUtc = DateTime.UtcNow;
        model.UpdatedAtUtc = DateTime.UtcNow;
        db.Tenants.Add(model);
        await db.SaveChangesAsync();
        return Ok(model);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Tenant>> Update(int id, Tenant model)
    {
        var item = await db.Tenants.FindAsync(id);
        if (item is null) return NotFound();

        item.Type = model.Type;
        item.Name = model.Name;
        item.TaxId = model.TaxId;
        item.PersonalId = model.PersonalId;
        item.Phone = model.Phone;
        item.Email = model.Email;
        item.Address = model.Address;
        item.EmergencyContactName = model.EmergencyContactName;
        item.EmergencyContactPhone = model.EmergencyContactPhone;
        item.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Ok(item);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.Tenants.FindAsync(id);
        if (item is null) return NotFound();

        db.Tenants.Remove(item);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
