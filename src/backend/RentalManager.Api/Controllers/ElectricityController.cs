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
                x.PayableTotalAmount
            }).ToListAsync();

        return Ok(rows);
    }
}
