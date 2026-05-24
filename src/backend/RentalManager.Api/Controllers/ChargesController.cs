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
    private async Task<string?> ValidateAsync(ChargeRecord model)
    {
        if (model.ContractId <= 0) return "請選擇合約";
        if (!await db.Contracts.AnyAsync(x => x.Id == model.ContractId)) return "合約不存在";
        if (model.Amount < 0) return "金額不可小於 0";
        if (model.BillingEndUtc < model.BillingStartUtc) return "帳期結束日不可早於開始日";
        if ((model.Category == ChargeCategory.Water || model.Category == ChargeCategory.Electricity) &&
            model.MeterStart.HasValue && model.MeterEnd.HasValue &&
            model.MeterEnd.Value < model.MeterStart.Value)
        {
            return "錶末讀數不可小於錶初讀數";
        }
        return null;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ChargeRecord>>> GetAll([FromQuery] DateTime? startDateUtc, [FromQuery] DateTime? endDateUtc)
    {
        var query = db.ChargeRecords.AsQueryable();
        if (startDateUtc.HasValue)
        {
            query = query.Where(x => x.BillingStartUtc >= startDateUtc.Value);
        }
        if (endDateUtc.HasValue)
        {
            query = query.Where(x => x.BillingStartUtc <= endDateUtc.Value);
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
        var error = await ValidateAsync(model);
        if (error is not null) return BadRequest(error);

        if (model.MeterStart.HasValue && model.MeterEnd.HasValue)
        {
            model.UsageUnits = model.MeterEnd.Value - model.MeterStart.Value;
        }
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
        var error = await ValidateAsync(model);
        if (error is not null) return BadRequest(error);

        item.ContractId = model.ContractId;
        item.Category = model.Category;
        item.BillingStartUtc = model.BillingStartUtc;
        item.BillingEndUtc = model.BillingEndUtc;
        item.MeterStart = model.MeterStart;
        item.MeterEnd = model.MeterEnd;
        item.UsageUnits = model.MeterStart.HasValue && model.MeterEnd.HasValue
            ? model.MeterEnd.Value - model.MeterStart.Value
            : model.UsageUnits;
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
