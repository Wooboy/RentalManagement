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
    public async Task<ActionResult<IEnumerable<ChargeRecord>>> GetAll() => Ok(await db.ChargeRecords.OrderByDescending(x => x.Id).ToListAsync());

    [HttpPost]
    public async Task<ActionResult<ChargeRecord>> Create(ChargeRecord model)
    {
        model.CreatedAtUtc = DateTime.UtcNow;
        db.ChargeRecords.Add(model);
        await db.SaveChangesAsync();
        return Ok(model);
    }
}
