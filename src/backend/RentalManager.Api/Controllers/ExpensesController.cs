using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalManager.Api.Data;
using RentalManager.Api.Models;

namespace RentalManager.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/expenses")]
public class ExpensesController(AppDbContext db) : ControllerBase
{
    private static string? Validate(ExpenseRecord model)
    {
        if (model.Amount < 0) return "金額不可小於 0";
        if (model.BillingEndUtc < model.BillingStartUtc) return "帳期結束日不可早於開始日";
        return null;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ExpenseRecord>>> GetAll([FromQuery] int? year, [FromQuery] int? month)
    {
        var query = db.ExpenseRecords.AsQueryable();
        if (year.HasValue && month.HasValue)
        {
            var start = new DateTime(year.Value, month.Value, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = start.AddMonths(1);
            query = query.Where(x => x.BillingStartUtc >= start && x.BillingStartUtc < end);
        }

        return Ok(await query.OrderByDescending(x => x.Id).ToListAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ExpenseRecord>> GetById(int id)
    {
        var item = await db.ExpenseRecords.FindAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<ExpenseRecord>> Create(ExpenseRecord model)
    {
        var error = Validate(model);
        if (error is not null) return BadRequest(error);

        model.CreatedAtUtc = DateTime.UtcNow;
        db.ExpenseRecords.Add(model);
        await db.SaveChangesAsync();
        return Ok(model);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ExpenseRecord>> Update(int id, ExpenseRecord model)
    {
        var item = await db.ExpenseRecords.FindAsync(id);
        if (item is null) return NotFound();
        var error = Validate(model);
        if (error is not null) return BadRequest(error);

        item.Category = model.Category;
        item.BillingStartUtc = model.BillingStartUtc;
        item.BillingEndUtc = model.BillingEndUtc;
        item.Amount = model.Amount;
        item.Notes = model.Notes;
        item.OccurredAtUtc = model.OccurredAtUtc;

        await db.SaveChangesAsync();
        return Ok(item);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.ExpenseRecords.FindAsync(id);
        if (item is null) return NotFound();

        db.ExpenseRecords.Remove(item);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
