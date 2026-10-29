using RentalManager.Api.Models;
using RentalManager.Api.Services;
using Xunit;

namespace RentalManager.Api.Tests;

public class BillReadingSeriesTests
{
    private static ElectricityMeterReading Reading(int id, string date, decimal value) => new()
    {
        Id = id,
        PropertyRoomId = 1,
        ReadingDateUtc = DateTime.Parse(date),
        ReadingValue = value
    };

    [Fact]
    public void MissingStartBoundary_ReturnsEmpty()
    {
        var readings = new List<ElectricityMeterReading>
        {
            Reading(1, "2026-01-15", 100m),
            Reading(2, "2026-02-05", 200m)
        };

        var series = ElectricityBillingService.BuildBillReadingSeries(readings, DateTime.Parse("2026-01-01"), DateTime.Parse("2026-01-31"));

        Assert.Empty(series);
    }

    [Fact]
    public void MissingEndBoundary_ReturnsEmpty()
    {
        var readings = new List<ElectricityMeterReading>
        {
            Reading(1, "2025-12-31", 100m),
            Reading(2, "2026-01-15", 150m)
        };

        var series = ElectricityBillingService.BuildBillReadingSeries(readings, DateTime.Parse("2026-01-01"), DateTime.Parse("2026-01-31"));

        Assert.Empty(series);
    }

    [Fact]
    public void BoundariesAndInPeriodReadings_AreIncludedInOrder()
    {
        var readings = new List<ElectricityMeterReading>
        {
            Reading(1, "2025-12-20", 90m),   // 更早的讀數，不應入選
            Reading(2, "2025-12-31", 100m),  // 起始邊界（帳期開始前最後一筆）
            Reading(3, "2026-01-15", 150m),  // 帳期內
            Reading(4, "2026-02-05", 200m),  // 結束邊界（帳期結束後第一筆）
            Reading(5, "2026-03-01", 300m)   // 更晚的讀數，不應入選
        };

        var series = ElectricityBillingService.BuildBillReadingSeries(readings, DateTime.Parse("2026-01-01"), DateTime.Parse("2026-01-31"));

        Assert.Equal([2, 3, 4], series.Select(x => x.Id));
    }

    [Fact]
    public void ReadingOnBillingStartDate_IsUsedAsStartBoundary()
    {
        var readings = new List<ElectricityMeterReading>
        {
            Reading(1, "2026-01-01", 100m),
            Reading(2, "2026-02-01", 200m)
        };

        var series = ElectricityBillingService.BuildBillReadingSeries(readings, DateTime.Parse("2026-01-01"), DateTime.Parse("2026-01-31"));

        Assert.Equal([1, 2], series.Select(x => x.Id));
    }

    [Fact]
    public void ReadingOnBillingEndDate_IsUsedAsEndBoundary()
    {
        // 每期於週期兩端各抄一次，抄表日剛好對齊帳期起訖日（起 06-02、迄 08-03）。
        var readings = new List<ElectricityMeterReading>
        {
            Reading(1, "2026-06-02", 15384m),
            Reading(2, "2026-08-03", 15679m)
        };

        var series = ElectricityBillingService.BuildBillReadingSeries(readings, DateTime.Parse("2026-06-02"), DateTime.Parse("2026-08-03"));

        Assert.Equal([1, 2], series.Select(x => x.Id));
    }
}
