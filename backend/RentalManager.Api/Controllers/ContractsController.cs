using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalManager.Api.Data;
using RentalManager.Api.Models;

namespace RentalManager.Api.Controllers;

[ApiController]
[Route("api/contracts")]
[Authorize]
public class ContractsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Contract>>> GetAll() => Ok(await db.Contracts.Include(x => x.Tenant).OrderByDescending(x => x.Id).ToListAsync());

    [HttpPost]
    public async Task<ActionResult<Contract>> Create(Contract model)
    {
        model.CreatedAtUtc = DateTime.UtcNow;
        model.UpdatedAtUtc = DateTime.UtcNow;
        db.Contracts.Add(model);
        await db.SaveChangesAsync();
        return Ok(model);
    }
}
