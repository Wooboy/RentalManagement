using RentalManager.Api.Common;
using RentalManager.Api.Dtos;

namespace RentalManager.Api.Services;

/// <summary>
/// 電費計算的純邏輯（不碰資料庫），供 API 與單元測試共用。
/// </summary>
public static class ElectricityCalculationService
{
    public static ElectricityCalculateResponse Calculate(ElectricityCalculateRequest request)
    {
        if (request.RuleType == 1)
        {
            var unitPrice = request.UnitPrice ?? 0;
            var units = request.TenantUnits ?? 0;
            var amount = units * unitPrice;
            return new ElectricityCalculateResponse(unitPrice, amount, 0, amount, null);
        }

        if (request.RuleType == 2)
        {
            var billAmount = request.BillAmount ?? 0;
            var totalUnits = request.TotalUnits ?? 1;
            var units = request.TenantUnits ?? 0;
            var unitPrice = totalUnits == 0 ? 0 : billAmount / totalUnits;
            var amount = units * unitPrice;
            return new ElectricityCalculateResponse(unitPrice, amount, 0, amount, null);
        }

        // 規則 3（多錶：各錶單價）與規則 4（多錶平均單價：多帳單加總後取單一平均單價）
        // 在此皆以「總額 ÷ 總度數」的合併平均單價計算私電，公電再依居住人天分攤；
        // 兩者差異（逐錶單價 vs 平均單價）由前端於帶入各帳單時決定總額與總度數。
        if (request.RuleType == 3 || request.RuleType == 4)
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

            return new ElectricityCalculateResponse(unitPrice, privateTotal, publicTotal, payables.Sum(), payables);
        }

        throw new DomainValidationException("未知規則");
    }

    /// <summary>依帳單總額/總度數與各戶分攤輸入，計算單價與公私電分攤金額。</summary>
    public static (decimal UnitPrice, decimal PrivateTotal, decimal PublicTotal, decimal AvgDailyPublic) ResolveAllocationRates(
        decimal totalAmount, decimal totalUnits, IReadOnlyList<ElectricityAllocationSaveInput> allocations)
    {
        var unitPrice = totalUnits == 0 ? 0 : totalAmount / totalUnits;
        var privateTotal = allocations.Sum(x => x.TenantUnits * unitPrice);
        var publicTotal = totalAmount - privateTotal;
        var divisor = allocations.Sum(x => x.OccupantCount * x.OccupancyDays);
        var avgDailyPublic = divisor == 0 ? 0 : publicTotal / divisor;
        return (unitPrice, privateTotal, publicTotal, avgDailyPublic);
    }

    public static int CalculateInclusiveDays(DateTime start, DateTime end)
        => end.Date < start.Date ? 0 : (end.Date - start.Date).Days + 1;

    public static decimal TruncateToInteger(decimal amount)
        => Math.Floor(amount);
}
