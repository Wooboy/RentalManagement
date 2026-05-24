using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalManager.Api.Data;
using RentalManager.Api.Models;

namespace RentalManager.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/charges")]
public class ChargesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ChargeRecord>>> GetAll([FromQuery] int? year, [FromQuery] int? month)
    {
        var query = db.ChargeRecords.AsQueryable();
        if (year.HasValue && month.HasValue)
        {
            var start = new DateTime(year.Value, month.Value, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = start.AddMonths(1);
            query = query.Where(x => x.BillingStartUtc >= start && x.BillingStartUtc < end);
        }

        return Ok(await query.OrderByDescending(x => x.Id).ToListAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ChargeRecord>> GetById(int id)
    {
        var item = await db.ChargeRecords.FindAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<ChargeRecord>> Create(ChargeRecord model)
    {
        model.CreatedAtUtc = DateTime.UtcNow;
        db.ChargeRecords.Add(model);
        await db.SaveChangesAsync();
        return Ok(model);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ChargeRecord>> Update(int id, ChargeRecord model)
    {
        var item = await db.ChargeRecords.FindAsync(id);
        if (item is null) return NotFound();

        item.ContractId = model.ContractId;
        item.Category = model.Category;
        item.BillingStartUtc = model.BillingStartUtc;
        item.BillingEndUtc = model.BillingEndUtc;
        item.MeterStart = model.MeterStart;
        item.MeterEnd = model.MeterEnd;
        item.UsageUnits = model.UsageUnits;
        item.Amount = model.Amount;
        item.Notes = model.Notes;
        item.IsPaid = model.IsPaid;
        item.PaidAtUtc = model.PaidAtUtc;

        await db.SaveChangesAsync();
        return Ok(item);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.ChargeRecords.FindAsync(id);
        if (item is null) return NotFound();

        db.ChargeRecords.Remove(item);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
