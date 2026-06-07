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
    int PaymentIntervalMonths,
    decimal Deposit,
    int OccupantCount,
    string? Notes,
    int ElectricityRuleType,
    int Status
);

public record ContractBatchChargeCreateRequest(
    List<int> ContractIds,
    DateTime? ReferenceDateUtc
);

[ApiController]
[Route("api/contracts")]
[Authorize]
public class ContractsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetAll([FromQuery] string? keyword, [FromQuery] int? propertyUnitId, [FromQuery] int? propertyRoomId, [FromQuery] int? status)
    {
        var query = db.Contracts.Include(x => x.Tenant).Include(x => x.PropertyUnit).AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(x => x.ContractNo.Contains(keyword) || x.PropertyName.Contains(keyword));
        if (status.HasValue) query = query.Where(x => (int)x.Status == status.Value);

        if (propertyUnitId.HasValue || propertyRoomId.HasValue)
        {
            var filteredContractIds = db.ContractRooms
                .Include(x => x.PropertyRoom)
                .Where(x =>
                    (!propertyUnitId.HasValue || x.PropertyRoom!.PropertyUnitId == propertyUnitId.Value) &&
                    (!propertyRoomId.HasValue || x.PropertyRoomId == propertyRoomId.Value))
                .Select(x => x.ContractId)
                .Distinct();

            query = query.Where(x => filteredContractIds.Contains(x.Id));
        }

        var contracts = await query.OrderByDescending(x => x.Id).ToListAsync();
        var ids = contracts.Select(x => x.Id).ToList();
        var roomQuery = db.ContractRooms
            .Include(x => x.PropertyRoom)
            .ThenInclude(x => x!.PropertyUnit)
            .Where(x => ids.Contains(x.ContractId));
        var roomMap = await roomQuery
            .GroupBy(x => x.ContractId)
            .ToDictionaryAsync(g => g.Key, g => g.Select(x => new
            {
                x.PropertyRoomId,
                x.PropertyRoom!.PropertyUnitId,
                PropertyUnitName = x.PropertyRoom.PropertyUnit != null ? x.PropertyRoom.PropertyUnit.Name : string.Empty,
                RoomCode = x.PropertyRoom.Code,
                RoomName = x.PropertyRoom.Name
            }).ToList());

        var rows = contracts.Select(c => new {
            c.Id, c.ContractNo, c.TenantId, c.Tenant, c.PropertyUnitId, c.PropertyName, c.PropertyAddress,
            c.StartDateUtc, c.EndDateUtc, c.MonthlyRent, c.PaymentIntervalMonths, c.PeriodPayableAmount, c.Deposit, c.OccupantCount, c.Notes, c.ElectricityRuleType, c.Status,
            c.CreatedAtUtc, c.UpdatedAtUtc,
            PropertyRoomIds = roomMap.ContainsKey(c.Id) ? roomMap[c.Id].Select(r => r.PropertyRoomId).ToList() : new List<int>(),
            Rooms = roomMap.ContainsKey(c.Id) ? roomMap[c.Id].Cast<object>().ToList() : new List<object>()
        }).ToList();

        return Ok(rows);
    }

    [HttpPost]
    public async Task<ActionResult> Create(ContractUpsertRequest model)
    {
        var valid = await ValidateAndResolve(model);
        if (!valid.ok) return BadRequest(valid.error);

        var contract = new Contract
        {
            ContractNo = string.IsNullOrWhiteSpace(model.ContractNo) ? $"AUTO-{DateTime.UtcNow:yyyyMMddHHmmss}" : model.ContractNo.Trim(),
            TenantId = model.TenantId,
            PropertyUnitId = valid.propertyUnitId,
            PropertyName = valid.propertyDisplay!,
            PropertyAddress = valid.propertyAddress!,
            StartDateUtc = model.StartDateUtc,
            EndDateUtc = model.EndDateUtc,
            MonthlyRent = model.MonthlyRent,
            PaymentIntervalMonths = model.PaymentIntervalMonths,
            PeriodPayableAmount = model.MonthlyRent * model.PaymentIntervalMonths,
            Deposit = model.Deposit,
            OccupantCount = model.OccupantCount,
            Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim(),
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

        if (!string.IsNullOrWhiteSpace(model.ContractNo))
        {
            item.ContractNo = model.ContractNo.Trim();
        }
        item.TenantId = model.TenantId;
        item.PropertyUnitId = valid.propertyUnitId;
        item.PropertyName = valid.propertyDisplay!;
        item.PropertyAddress = valid.propertyAddress!;
        item.StartDateUtc = model.StartDateUtc;
        item.EndDateUtc = model.EndDateUtc;
        item.MonthlyRent = model.MonthlyRent;
        item.PaymentIntervalMonths = model.PaymentIntervalMonths;
        item.PeriodPayableAmount = model.MonthlyRent * model.PaymentIntervalMonths;
        item.Deposit = model.Deposit;
        item.OccupantCount = model.OccupantCount;
        item.Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim();
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

    [HttpPost("batch-create-period-charges")]
    public async Task<ActionResult<object>> BatchCreatePeriodCharges(ContractBatchChargeCreateRequest request)
    {
        var contractIds = (request.ContractIds ?? []).Distinct().Where(x => x > 0).ToList();
        if (contractIds.Count == 0) return BadRequest("請至少選擇一份合約");

        var referenceDate = (request.ReferenceDateUtc ?? DateTime.UtcNow).Date;
        var contracts = await db.Contracts
            .Where(x => contractIds.Contains(x.Id))
            .OrderBy(x => x.Id)
            .ToListAsync();
        if (contracts.Count != contractIds.Count) return BadRequest("部分合約不存在");

        var existingCharges = await db.ChargeRecords
            .Where(x => contractIds.Contains(x.ContractId) && x.Category == ChargeCategory.Rent)
            .Select(x => new { x.ContractId, x.BillingStartUtc, x.BillingEndUtc })
            .ToListAsync();

        var created = new List<object>();
        var skipped = new List<object>();

        foreach (var contract in contracts)
        {
            if (contract.Status != ContractStatus.Active)
            {
                skipped.Add(new { contract.Id, contract.ContractNo, reason = "合約不是生效中" });
                continue;
            }

            var period = ResolveCurrentBillingPeriod(contract, referenceDate);

            var duplicated = existingCharges.Any(x =>
                x.ContractId == contract.Id &&
                x.BillingStartUtc.Date == period.start.Date &&
                x.BillingEndUtc.Date == period.end.Date);
            if (duplicated)
            {
                skipped.Add(new { contract.Id, contract.ContractNo, reason = "本期租金應收已存在" });
                continue;
            }

            var charge = new ChargeRecord
            {
                ContractId = contract.Id,
                Category = ChargeCategory.Rent,
                BillingStartUtc = period.start,
                BillingEndUtc = period.end,
                Amount = contract.PeriodPayableAmount,
                Notes = $"系統批次建立本期租金（{period.start:yyyy-MM-dd} ~ {period.end:yyyy-MM-dd}）",
                IsPaid = false,
                CreatedAtUtc = DateTime.UtcNow
            };

            db.ChargeRecords.Add(charge);
            created.Add(new { contract.Id, contract.ContractNo, charge.BillingStartUtc, charge.BillingEndUtc, charge.Amount });
        }

        if (created.Count > 0)
        {
            await db.SaveChangesAsync();
        }

        return Ok(new
        {
            referenceDateUtc = referenceDate,
            createdCount = created.Count,
            skippedCount = skipped.Count,
            created,
            skipped
        });
    }

    private async Task<(bool ok, string? error, int? propertyUnitId, string? propertyDisplay, string? propertyAddress)> ValidateAndResolve(ContractUpsertRequest model)
    {
        if (model.TenantId <= 0) return (false, "請選擇租客", null, null, null);
        if (model.PaymentIntervalMonths != 1 && model.PaymentIntervalMonths != 3 && model.PaymentIntervalMonths != 12) return (false, "付款間隔只允許每月、每季、每年", null, null, null);
        if (model.PropertyRoomIds is null || model.PropertyRoomIds.Count == 0) return (false, "請至少選擇一間房間", null, null, null);
        if (!await db.Tenants.AnyAsync(x => x.Id == model.TenantId)) return (false, "租客不存在", null, null, null);

        var roomIds = model.PropertyRoomIds.Distinct().ToList();
        var rooms = await db.PropertyRooms.Include(x => x.PropertyUnit).Where(x => roomIds.Contains(x.Id)).ToListAsync();
        if (rooms.Count != roomIds.Count || rooms.Any(x => x.PropertyUnit is null)) return (false, "房間資料不存在", null, null, null);

        var propertyGroups = rooms
            .GroupBy(x => x.PropertyUnitId)
            .Select(g => new
            {
                PropertyUnitId = g.Key,
                Property = g.First().PropertyUnit!,
                Rooms = g.OrderBy(x => x.Name).ThenBy(x => x.Id).ToList()
            })
            .OrderBy(x => x.Property.Name)
            .ThenBy(x => x.Property.Id)
            .ToList();

        var propertyDisplay = string.Join("；", propertyGroups.Select(g =>
            $"{g.Property.Name}-" + string.Join(",", g.Rooms.Select(r => string.IsNullOrWhiteSpace(r.Name) ? r.Code : r.Name))));
        var propertyAddress = string.Join("；", propertyGroups.Select(g => g.Property.Address).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct());
        int? propertyUnitId = propertyGroups.Count == 1 ? propertyGroups[0].PropertyUnitId : null;

        return (true, null, propertyUnitId, propertyDisplay, propertyAddress);
    }

    private static (DateTime start, DateTime end) ResolveCurrentBillingPeriod(Contract contract, DateTime referenceDate)
    {
        var startDate = contract.StartDateUtc.Date;
        var endDate = contract.EndDateUtc.Date;
        if (referenceDate < startDate) referenceDate = startDate;

        var intervalMonths = contract.PaymentIntervalMonths <= 0 ? 1 : contract.PaymentIntervalMonths;
        var periodStart = startDate;

        while (periodStart.AddMonths(intervalMonths) <= referenceDate)
        {
            periodStart = periodStart.AddMonths(intervalMonths);
        }

        var periodEnd = periodStart.AddMonths(intervalMonths).AddDays(-1);
        if (periodEnd > endDate) periodEnd = endDate;

        return (periodStart, periodEnd);
    }
}
