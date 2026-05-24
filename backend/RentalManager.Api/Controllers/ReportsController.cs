using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalManager.Api.Data;

namespace RentalManager.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/reports")]
public class ReportsController(AppDbContext db) : ControllerBase
{
    [HttpGet("monthly")]
    public async Task<ActionResult<object>> Monthly([FromQuery] int year, [FromQuery] int month)
    {
        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);

        var income = await db.ChargeRecords
            .Where(x => x.BillingStartUtc >= start && x.BillingStartUtc < end)
            .SumAsync(x => (decimal?)x.Amount) ?? 0;

        var expense = await db.ExpenseRecords
            .Where(x => x.BillingStartUtc >= start && x.BillingStartUtc < end)
            .SumAsync(x => (decimal?)x.Amount) ?? 0;

        return Ok(new
        {
            year,
            month,
            income,
            expense,
            profit = income - expense
        });
    }
}
