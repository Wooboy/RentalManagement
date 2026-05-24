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
    public async Task<ActionResult<IEnumerable<Tenant>>> GetAll() => Ok(await db.Tenants.OrderByDescending(x => x.Id).ToListAsync());

    [HttpPost]
    public async Task<ActionResult<Tenant>> Create(Tenant model)
    {
        model.CreatedAtUtc = DateTime.UtcNow;
        model.UpdatedAtUtc = DateTime.UtcNow;
        db.Tenants.Add(model);
        await db.SaveChangesAsync();
        return Ok(model);
    }
}
