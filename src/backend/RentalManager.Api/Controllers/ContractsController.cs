using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalManager.Api.Data;
using RentalManager.Api.Models;

namespace RentalManager.Api.Controllers;

public record ContractUpsertRequest(
    string ContractNo,
    int TenantId,
    List<int> PropertyRoomIds,
    DateTime StartDateUtc,
    DateTime EndDateUtc,
    decimal MonthlyRent,
    decimal Deposit,
    int OccupantCount,
    int ElectricityRuleType,
    int Status
);

[ApiController]
[Route("api/contracts")]
[Authorize]
public class ContractsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetAll([FromQuery] string? keyword, [FromQuery] int? propertyUnitId, [FromQuery] int? propertyRoomId)
    {
        var query = db.Contracts.Include(x => x.Tenant).Include(x => x.PropertyUnit).AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(x => x.ContractNo.Contains(keyword) || x.PropertyName.Contains(keyword));
        if (propertyUnitId.HasValue) query = query.Where(x => x.PropertyUnitId == propertyUnitId.Value);

        var contracts = await query.OrderByDescending(x => x.Id).ToListAsync();
        var ids = contracts.Select(x => x.Id).ToList();
        var roomQuery = db.ContractRooms.Include(x => x.PropertyRoom).Where(x => ids.Contains(x.ContractId));
        if (propertyRoomId.HasValue) roomQuery = roomQuery.Where(x => x.PropertyRoomId == propertyRoomId.Value);
        var roomMap = await roomQuery
            .GroupBy(x => x.ContractId)
            .ToDictionaryAsync(g => g.Key, g => g.Select(x => new { x.PropertyRoomId, RoomCode = x.PropertyRoom!.Code, RoomName = x.PropertyRoom!.Name }).ToList());

        var rows = contracts.Select(c => new {
            c.Id, c.ContractNo, c.TenantId, c.Tenant, c.PropertyUnitId, c.PropertyName, c.PropertyAddress,
            c.StartDateUtc, c.EndDateUtc, c.MonthlyRent, c.Deposit, c.OccupantCount, c.ElectricityRuleType, c.Status,
            c.CreatedAtUtc, c.UpdatedAtUtc,
            PropertyRoomIds = roomMap.ContainsKey(c.Id) ? roomMap[c.Id].Select(r => r.PropertyRoomId).ToList() : new List<int>(),
            Rooms = roomMap.ContainsKey(c.Id) ? roomMap[c.Id].Cast<object>().ToList() : new List<object>()
        }).ToList();

        if (propertyRoomId.HasValue)
        {
            rows = rows.Where(x => x.PropertyRoomIds.Contains(propertyRoomId.Value)).ToList();
        }

        return Ok(rows);
    }

    [HttpPost]
    public async Task<ActionResult> Create(ContractUpsertRequest model)
    {
        var valid = await ValidateAndResolve(model);
        if (!valid.ok) return BadRequest(valid.error);

        var contract = new Contract
        {
            ContractNo = model.ContractNo,
            TenantId = model.TenantId,
            PropertyUnitId = valid.propertyUnit!.Id,
            PropertyName = valid.propertyDisplay!,
            PropertyAddress = valid.propertyUnit.Address,
            StartDateUtc = model.StartDateUtc,
            EndDateUtc = model.EndDateUtc,
            MonthlyRent = model.MonthlyRent,
            Deposit = model.Deposit,
            OccupantCount = model.OccupantCount,
            ElectricityRuleType = (ElectricityRuleType)model.ElectricityRuleType,
            Status = (ContractStatus)model.Status,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        db.Contracts.Add(contract);
        await db.SaveChangesAsync();

        db.ContractRooms.AddRange(model.PropertyRoomIds.Distinct().Select(rid => new ContractRoom { ContractId = contract.Id, PropertyRoomId = rid }));
        await db.SaveChangesAsync();
        return Ok(contract);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> Update(int id, ContractUpsertRequest model)
    {
        var item = await db.Contracts.FindAsync(id);
        if (item is null) return NotFound();

        var valid = await ValidateAndResolve(model);
        if (!valid.ok) return BadRequest(valid.error);

        item.ContractNo = model.ContractNo;
        item.TenantId = model.TenantId;
        item.PropertyUnitId = valid.propertyUnit!.Id;
        item.PropertyName = valid.propertyDisplay!;
        item.PropertyAddress = valid.propertyUnit.Address;
        item.StartDateUtc = model.StartDateUtc;
        item.EndDateUtc = model.EndDateUtc;
        item.MonthlyRent = model.MonthlyRent;
        item.Deposit = model.Deposit;
        item.OccupantCount = model.OccupantCount;
        item.ElectricityRuleType = (ElectricityRuleType)model.ElectricityRuleType;
        item.Status = (ContractStatus)model.Status;
        item.UpdatedAtUtc = DateTime.UtcNow;

        var old = db.ContractRooms.Where(x => x.ContractId == id);
        db.ContractRooms.RemoveRange(old);
        db.ContractRooms.AddRange(model.PropertyRoomIds.Distinct().Select(rid => new ContractRoom { ContractId = id, PropertyRoomId = rid }));

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

    private async Task<(bool ok, string? error, PropertyUnit? propertyUnit, string? propertyDisplay)> ValidateAndResolve(ContractUpsertRequest model)
    {
        if (string.IsNullOrWhiteSpace(model.ContractNo)) return (false, "合約編號必填", null, null);
        if (model.TenantId <= 0) return (false, "請選擇租客", null, null);
        if (model.PropertyRoomIds is null || model.PropertyRoomIds.Count == 0) return (false, "請至少選擇一間房間", null, null);
        if (!await db.Tenants.AnyAsync(x => x.Id == model.TenantId)) return (false, "租客不存在", null, null);

        var roomIds = model.PropertyRoomIds.Distinct().ToList();
        var rooms = await db.PropertyRooms.Include(x => x.PropertyUnit).Where(x => roomIds.Contains(x.Id)).ToListAsync();
        if (rooms.Count != roomIds.Count || rooms.Any(x => x.PropertyUnit is null)) return (false, "房間資料不存在", null, null);

        var unitId = rooms.First().PropertyUnitId;
        if (rooms.Any(x => x.PropertyUnitId != unitId)) return (false, "所選房間必須屬於同一房源", null, null);

        var property = rooms.First().PropertyUnit!;
        var display = $"{property.Name}-" + string.Join(",", rooms.Select(r => string.IsNullOrWhiteSpace(r.Name) ? r.Code : r.Name));
        return (true, null, property, display);
    }
}
