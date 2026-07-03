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

        // Always use the selected month and keep the day anchored to the contract start day,
        // falling back to the month's last day when needed.
        var periodStart = startDate.AddMonths(monthDiff);
        var periodEnd = periodStart.AddMonths(intervalMonths).AddDays(-1);
        return (periodStart, periodEnd);
    }
}
