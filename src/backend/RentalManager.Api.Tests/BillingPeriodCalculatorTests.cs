using RentalManager.Api.Models;
using RentalManager.Api.Services;
using Xunit;

namespace RentalManager.Api.Tests;

public class BillingPeriodCalculatorTests
{
    private static Contract MakeContract(string startDate, int intervalMonths) => new()
    {
        StartDateUtc = DateTime.Parse(startDate),
        EndDateUtc = DateTime.Parse(startDate).AddYears(1),
        PaymentIntervalMonths = intervalMonths
    };

    [Fact]
    public void Monthly_TargetSameMonth_AnchorsToContractStartDay()
    {
        var contract = MakeContract("2026-01-15", 1);

        var (start, end) = BillingPeriodCalculator.ResolveBillingPeriodForMonth(contract, new DateTime(2026, 1, 1));

        Assert.Equal(new DateTime(2026, 1, 15), start);
        Assert.Equal(new DateTime(2026, 2, 14), end);
    }

    [Fact]
    public void Monthly_TargetLaterMonth_ShiftsPeriod()
    {
        var contract = MakeContract("2026-01-15", 1);

        var (start, end) = BillingPeriodCalculator.ResolveBillingPeriodForMonth(contract, new DateTime(2026, 3, 20));

        Assert.Equal(new DateTime(2026, 3, 15), start);
        Assert.Equal(new DateTime(2026, 4, 14), end);
    }

    [Fact]
    public void Quarterly_PeriodSpansThreeMonths()
    {
        var contract = MakeContract("2026-01-01", 3);

        var (start, end) = BillingPeriodCalculator.ResolveBillingPeriodForMonth(contract, new DateTime(2026, 1, 1));

        Assert.Equal(new DateTime(2026, 1, 1), start);
        Assert.Equal(new DateTime(2026, 3, 31), end);
    }

    [Fact]
    public void Quarterly_MidQuarterMonth_AlignsToPeriodBoundary()
    {
        var contract = MakeContract("2026-01-01", 3);

        // 選第二個月（2 月）應仍落在同一季 1/1~3/31，而非偏移出重疊的 2/1~4/30
        var (start, end) = BillingPeriodCalculator.ResolveBillingPeriodForMonth(contract, new DateTime(2026, 2, 10));

        Assert.Equal(new DateTime(2026, 1, 1), start);
        Assert.Equal(new DateTime(2026, 3, 31), end);
    }

    [Fact]
    public void Quarterly_SecondQuarterMonth_ShiftsToNextPeriod()
    {
        var contract = MakeContract("2026-01-01", 3);

        var (start, end) = BillingPeriodCalculator.ResolveBillingPeriodForMonth(contract, new DateTime(2026, 5, 1));

        Assert.Equal(new DateTime(2026, 4, 1), start);
        Assert.Equal(new DateTime(2026, 6, 30), end);
    }

    [Fact]
    public void StartDay31_FallsBackToMonthEnd()
    {
        var contract = MakeContract("2026-01-31", 1);

        var (start, end) = BillingPeriodCalculator.ResolveBillingPeriodForMonth(contract, new DateTime(2026, 2, 1));

        // 2 月沒有 31 日，AddMonths 會落在該月最後一天
        Assert.Equal(new DateTime(2026, 2, 28), start);
        Assert.Equal(new DateTime(2026, 3, 27), end);
    }

    [Fact]
    public void ZeroInterval_TreatedAsMonthly()
    {
        var contract = MakeContract("2026-01-01", 0);

        var (start, end) = BillingPeriodCalculator.ResolveBillingPeriodForMonth(contract, new DateTime(2026, 1, 1));

        Assert.Equal(new DateTime(2026, 1, 1), start);
        Assert.Equal(new DateTime(2026, 1, 31), end);
    }
}
