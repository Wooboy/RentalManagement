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
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ExpenseRecord>>> GetAll() => Ok(await db.ExpenseRecords.OrderByDescending(x => x.Id).ToListAsync());

    [HttpPost]
    public async Task<ActionResult<ExpenseRecord>> Create(ExpenseRecord model)
    {
        model.CreatedAtUtc = DateTime.UtcNow;
        db.ExpenseRecords.Add(model);
        await db.SaveChangesAsync();
        return Ok(model);
    }
}
