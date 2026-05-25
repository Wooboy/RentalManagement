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
    private async Task<string?> ValidateAsync(ExpenseRecord model)
    {
        if (model.PropertyUnitId <= 0) return "請選擇房源";
        if (!await db.PropertyUnits.AnyAsync(x => x.Id == model.PropertyUnitId)) return "房源不存在";

        if (model.PropertyRoomId.HasValue)
        {
            var room = await db.PropertyRooms.FirstOrDefaultAsync(x => x.Id == model.PropertyRoomId.Value);
            if (room is null) return "房間不存在";
            if (room.PropertyUnitId != model.PropertyUnitId) return "房間不屬於所選房源";
        }

        if (model.Amount < 0) return "金額不可小於 0";
        if (model.UsageUnits.HasValue && model.UsageUnits.Value < 0) return "度數不可小於 0";
        if (model.BillingEndUtc < model.BillingStartUtc) return "帳期結束日不可早於開始日";
        if (!Enum.IsDefined(typeof(ExpenseSplitStatus), model.SplitStatus)) return "支出狀態不正確";
        return null;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetAll([FromQuery] DateTime? startDateUtc, [FromQuery] DateTime? endDateUtc)
    {
        var query = db.ExpenseRecords.Include(x => x.PropertyUnit).Include(x => x.PropertyRoom).AsQueryable();
        if (startDateUtc.HasValue)
        {
            query = query.Where(x => x.BillingStartUtc >= startDateUtc.Value);
        }
        if (endDateUtc.HasValue)
        {
            query = query.Where(x => x.BillingStartUtc <= endDateUtc.Value);
        }

        var rows = await query.OrderByDescending(x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.PropertyUnitId,
                PropertyUnitName = x.PropertyUnit != null ? x.PropertyUnit.Name : null,
                x.PropertyRoomId,
                PropertyRoomName = x.PropertyRoom != null ? x.PropertyRoom.Name : null,
                x.Category,
                x.BillingStartUtc,
                x.BillingEndUtc,
                x.Amount,
                x.UsageUnits,
                x.SplitStatus,
                x.Notes,
                x.OccurredAtUtc,
                x.CreatedAtUtc
            }).ToListAsync();
        return Ok(rows);
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
        var error = await ValidateAsync(model);
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
        var error = await ValidateAsync(model);
        if (error is not null) return BadRequest(error);

        item.PropertyUnitId = model.PropertyUnitId;
        item.PropertyRoomId = model.PropertyRoomId;
        item.Category = model.Category;
        item.BillingStartUtc = model.BillingStartUtc;
        item.BillingEndUtc = model.BillingEndUtc;
        item.Amount = model.Amount;
        item.UsageUnits = model.UsageUnits;
        item.SplitStatus = model.SplitStatus;
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
