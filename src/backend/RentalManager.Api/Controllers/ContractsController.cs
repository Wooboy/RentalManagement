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
    public async Task<ActionResult<IEnumerable<Contract>>> GetAll([FromQuery] string? keyword)
    {
        var query = db.Contracts.Include(x => x.Tenant).Include(x => x.PropertyUnit).AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(x => x.ContractNo.Contains(keyword) || x.PropertyName.Contains(keyword));
        }

        return Ok(await query.OrderByDescending(x => x.Id).ToListAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Contract>> GetById(int id)
    {
        var item = await db.Contracts.Include(x => x.Tenant).Include(x => x.PropertyUnit).FirstOrDefaultAsync(x => x.Id == id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<Contract>> Create(Contract model)
    {
        if (string.IsNullOrWhiteSpace(model.ContractNo)) return BadRequest("合約編號必填");
        if (model.TenantId <= 0) return BadRequest("請選擇租客");
        if (model.PropertyUnitId is null or <= 0) return BadRequest("請選擇房源");

        var tenantExists = await db.Tenants.AnyAsync(x => x.Id == model.TenantId);
        if (!tenantExists) return BadRequest("租客不存在");
        var property = await db.PropertyUnits.FirstOrDefaultAsync(x => x.Id == model.PropertyUnitId.Value);
        if (property is null) return BadRequest("房源不存在");

        model.PropertyName = property.Name;
        model.PropertyAddress = property.Address;
        model.CreatedAtUtc = DateTime.UtcNow;
        model.UpdatedAtUtc = DateTime.UtcNow;
        db.Contracts.Add(model);
        await db.SaveChangesAsync();
        return Ok(model);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Contract>> Update(int id, Contract model)
    {
        var item = await db.Contracts.FindAsync(id);
        if (item is null) return NotFound();
        if (string.IsNullOrWhiteSpace(model.ContractNo)) return BadRequest("合約編號必填");
        if (model.TenantId <= 0) return BadRequest("請選擇租客");
        if (model.PropertyUnitId is null or <= 0) return BadRequest("請選擇房源");

        var tenantExists = await db.Tenants.AnyAsync(x => x.Id == model.TenantId);
        if (!tenantExists) return BadRequest("租客不存在");
        var property = await db.PropertyUnits.FirstOrDefaultAsync(x => x.Id == model.PropertyUnitId.Value);
        if (property is null) return BadRequest("房源不存在");

        item.ContractNo = model.ContractNo;
        item.TenantId = model.TenantId;
        item.PropertyUnitId = model.PropertyUnitId;
        item.PropertyName = property.Name;
        item.PropertyAddress = property.Address;
        item.StartDateUtc = model.StartDateUtc;
        item.EndDateUtc = model.EndDateUtc;
        item.MonthlyRent = model.MonthlyRent;
        item.Deposit = model.Deposit;
        item.OccupantCount = model.OccupantCount;
        item.ElectricityRuleType = model.ElectricityRuleType;
        item.Status = model.Status;
        item.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Ok(item);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.Contracts.FindAsync(id);
        if (item is null) return NotFound();

        db.Contracts.Remove(item);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
