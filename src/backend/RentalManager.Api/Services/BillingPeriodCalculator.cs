using RentalManager.Api.Models;

namespace RentalManager.Api.Services;

/// <summary>合約租金帳期計算（純邏輯，供批次建立應收與單元測試使用）。</summary>
public static class BillingPeriodCalculator
{
    public static (DateTime Start, DateTime End) ResolveBillingPeriodForMonth(Contract contract, DateTime targetMonth)
    {
        var startDate = contract.StartDateUtc.Date;
        var normalizedTargetMonth = new DateTime(targetMonth.Year, targetMonth.Month, 1);
        var intervalMonths = contract.PaymentIntervalMonths <= 0 ? 1 : contract.PaymentIntervalMonths;
        var monthDiff = ((normalizedTargetMonth.Year - startDate.Year) * 12) + normalizedTargetMonth.Month - startDate.Month;

        // 將目標月對齊付款間隔的邊界，避免季繳/年繳在非整期月份被選到時產生偏移一個月的重疊帳期。
        // 例：合約 1/1 起、季繳，選 2 月時 monthDiff=1，對齊後仍落在 1/1~3/31 這一期。
        if (monthDiff < 0) monthDiff = 0;
        var alignedMonthDiff = monthDiff - (monthDiff % intervalMonths);

        // 期初鎖定合約起租日，往後推整數個付款間隔。
        var periodStart = startDate.AddMonths(alignedMonthDiff);
        var periodEnd = periodStart.AddMonths(intervalMonths).AddDays(-1);
        return (periodStart, periodEnd);
    }
}
