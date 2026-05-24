using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalManager.Api.Dtos;

namespace RentalManager.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/electricity")]
public class ElectricityController : ControllerBase
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
}
