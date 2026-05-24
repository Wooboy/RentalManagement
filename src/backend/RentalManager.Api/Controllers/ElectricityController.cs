using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalManager.Api.Data;
using RentalManager.Api.Dtos;
using RentalManager.Api.Models;

namespace RentalManager.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/electricity")]
public class ElectricityController(AppDbContext db) : ControllerBase
{
    [HttpPost("calculate")]
    public ActionResult<ElectricityCalculateResponse> Calculate(ElectricityCalculateRequest request)
    {
        if (request.RuleType == 1)
        {
            var unitPrice = request.UnitPrice ?? 0;
            var units = request.TenantUnits ?? 0;
            var amount = units * unitPrice;
            return Ok(new ElectricityCalculateResponse(unitPrice, amount, 0, amount, null));
        }

        if (request.RuleType == 2)
        {
            var billAmount = request.BillAmount ?? 0;
            var totalUnits = request.TotalUnits ?? 1;
            var units = request.TenantUnits ?? 0;
            var unitPrice = totalUnits == 0 ? 0 : billAmount / totalUnits;
            var amount = units * unitPrice;
            return Ok(new ElectricityCalculateResponse(unitPrice, amount, 0, amount, null));
        }

        if (request.RuleType == 3)
        {
            var tenants = request.Tenants ?? [];
            var totalAmount = request.BillAmount ?? 0;
            var totalUnits = request.TotalUnits ?? 1;
            var unitPrice = totalUnits == 0 ? 0 : totalAmount / totalUnits;

            var privateAmounts = tenants.Select(t => t.TenantUnits * unitPrice).ToList();
            var privateTotal = privateAmounts.Sum();
            var publicTotal = totalAmount - privateTotal;
            var divisor = tenants.Sum(x => x.OccupantCount * x.OccupancyDays);
            var avgDailyPublic = divisor == 0 ? 0 : publicTotal / divisor;

            var payables = tenants
                .Select((t, i) => privateAmounts[i] + (t.OccupantCount * t.OccupancyDays * avgDailyPublic))
                .ToList();

            return Ok(new ElectricityCalculateResponse(unitPrice, privateTotal, publicTotal, payables.Sum(), payables));
        }

        return BadRequest("未知規則");
    }

    [HttpPost("bills")]
    public async Task<ActionResult<object>> SaveBill(ElectricityBillSaveRequest request)
    {
        if (!await db.Contracts.AnyAsync(x => x.Id == request.ContractId)) return BadRequest("合約不存在");
        if (request.BillingEndUtc < request.BillingStartUtc) return BadRequest("帳期結束不可早於開始");

        var unitPrice = request.TotalUnits == 0 ? 0 : request.TotalAmount / request.TotalUnits;
        var privateTotal = request.Allocations.Sum(x => x.TenantUnits * unitPrice);
        var publicTotal = request.TotalAmount - privateTotal;
        var divisor = request.Allocations.Sum(x => x.OccupantCount * x.OccupancyDays);
        var avgPublic = divisor == 0 ? 0 : publicTotal / divisor;

        var bill = new ElectricityBill
        {
            ContractId = request.ContractId,
            RuleType = (ElectricityRuleType)request.RuleType,
            BillingStartUtc = request.BillingStartUtc,
            BillingEndUtc = request.BillingEndUtc,
            TotalAmount = request.TotalAmount,
            TotalUnits = request.TotalUnits,
            UnitPrice = unitPrice,
            PrivateTotalAmount = privateTotal,
            PublicTotalAmount = publicTotal,
            PayableTotalAmount = 0,
            ChargesCreated = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        db.ElectricityBills.Add(bill);
        await db.SaveChangesAsync();

        var allocations = request.Allocations.Select(x =>
        {
            var privateAmount = x.TenantUnits * unitPrice;
            var publicAmount = x.OccupantCount * x.OccupancyDays * avgPublic;
            var payable = privateAmount + publicAmount;
            return new ElectricityAllocation
            {
                ElectricityBillId = bill.Id,
                TenantId = x.TenantId,
                TenantUnits = x.TenantUnits,
                OccupantCount = x.OccupantCount,
                OccupancyDays = x.OccupancyDays,
                PrivateAmount = privateAmount,
                PublicAmount = publicAmount,
                PayableAmount = payable
            };
        }).ToList();

        bill.PayableTotalAmount = allocations.Sum(x => x.PayableAmount);
        db.ElectricityAllocations.AddRange(allocations);
        await db.SaveChangesAsync();

        return Ok(new { billId = bill.Id, bill.PayableTotalAmount });
    }

    [HttpGet("bills")]
    public async Task<ActionResult<IEnumerable<object>>> GetBills([FromQuery] int? contractId)
    {
        var query = db.ElectricityBills.Include(x => x.Contract).AsQueryable();
        if (contractId.HasValue) query = query.Where(x => x.ContractId == contractId.Value);

        var rows = await query.OrderByDescending(x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.ContractId,
                ContractNo = x.Contract!.ContractNo,
                x.BillingStartUtc,
                x.BillingEndUtc,
                x.TotalAmount,
                x.TotalUnits,
                x.UnitPrice,
                x.PayableTotalAmount,
                x.ChargesCreated
            }).ToListAsync();

        return Ok(rows);
    }

    [HttpGet("bills/{billId:int}")]
    public async Task<ActionResult<object>> GetBillDetail(int billId)
    {
        var bill = await db.ElectricityBills
            .Include(x => x.Contract)
            .FirstOrDefaultAsync(x => x.Id == billId);
        if (bill is null) return NotFound();

        var allocations = await db.ElectricityAllocations
            .Include(x => x.Tenant)
            .Where(x => x.ElectricityBillId == billId)
            .OrderBy(x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.TenantId,
                TenantName = x.Tenant != null ? x.Tenant.Name : null,
                x.TenantUnits,
                x.OccupantCount,
                x.OccupancyDays,
                x.PrivateAmount,
                x.PublicAmount,
                x.PayableAmount
            }).ToListAsync();

        return Ok(new
        {
            bill.Id,
            bill.ContractId,
            ContractNo = bill.Contract!.ContractNo,
            bill.BillingStartUtc,
            bill.BillingEndUtc,
            bill.TotalAmount,
            bill.TotalUnits,
            bill.UnitPrice,
            bill.PrivateTotalAmount,
            bill.PublicTotalAmount,
            bill.PayableTotalAmount,
            allocations
        });
    }

    [HttpPost("bills/{billId:int}/create-charges")]
    public async Task<ActionResult<object>> CreateChargesFromBill(int billId)
    {
        var bill = await db.ElectricityBills.FirstOrDefaultAsync(x => x.Id == billId);
        if (bill is null) return NotFound("帳單不存在");
        if (bill.ChargesCreated) return BadRequest("此帳單已轉入應收，不能重複轉入");

        var contract = await db.Contracts
            .Include(x => x.Tenant)
            .FirstOrDefaultAsync(x => x.Id == bill.ContractId);
        if (contract is null) return BadRequest("關聯合約不存在");

        var allocations = await db.ElectricityAllocations
            .Where(x => x.ElectricityBillId == billId)
            .ToListAsync();
        if (allocations.Count == 0) return BadRequest("無分攤資料");

        var created = 0;
        foreach (var a in allocations)
        {
            var charge = new ChargeRecord
            {
                ContractId = bill.ContractId,
                Category = ChargeCategory.Electricity,
                BillingStartUtc = bill.BillingStartUtc,
                BillingEndUtc = bill.BillingEndUtc,
                UsageUnits = a.TenantUnits,
                Amount = a.PayableAmount,
                Notes = $"電費帳單#{billId} 分攤；租客ID={a.TenantId?.ToString() ?? "N/A"}",
                IsPaid = false,
                CreatedAtUtc = DateTime.UtcNow
            };
            db.ChargeRecords.Add(charge);
            created++;
        }

        await db.SaveChangesAsync();
        bill.ChargesCreated = true;
        bill.ChargesCreatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(new { createdCount = created });
    }
}
