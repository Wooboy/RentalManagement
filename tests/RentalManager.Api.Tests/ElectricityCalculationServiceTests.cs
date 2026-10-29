using RentalManager.Api.Common;
using RentalManager.Api.Dtos;
using RentalManager.Api.Services;
using Xunit;

namespace RentalManager.Api.Tests;

public class ElectricityCalculationServiceTests
{
    [Fact]
    public void Rule1_UnitPrice_MultipliesUnits()
    {
        var request = new ElectricityCalculateRequest(1, UnitPrice: 5m, TenantUnits: 100m, BillAmount: null, TotalUnits: null, Tenants: null);

        var result = ElectricityCalculationService.Calculate(request);

        Assert.Equal(5m, result.UnitPrice);
        Assert.Equal(500m, result.PrivateElectricityAmount);
        Assert.Equal(0m, result.PublicElectricityAmount);
        Assert.Equal(500m, result.PayableAmount);
        Assert.Null(result.TenantPayables);
    }

    [Fact]
    public void Rule2_Average_DerivesUnitPriceFromBill()
    {
        var request = new ElectricityCalculateRequest(2, UnitPrice: null, TenantUnits: 50m, BillAmount: 1000m, TotalUnits: 200m, Tenants: null);

        var result = ElectricityCalculationService.Calculate(request);

        Assert.Equal(5m, result.UnitPrice);
        Assert.Equal(250m, result.PayableAmount);
    }

    [Fact]
    public void Rule2_ZeroTotalUnits_YieldsZero()
    {
        var request = new ElectricityCalculateRequest(2, UnitPrice: null, TenantUnits: 50m, BillAmount: 1000m, TotalUnits: 0m, Tenants: null);

        var result = ElectricityCalculationService.Calculate(request);

        Assert.Equal(0m, result.UnitPrice);
        Assert.Equal(0m, result.PayableAmount);
    }

    [Fact]
    public void Rule3_MultiMeter_SplitsPublicByOccupantDays()
    {
        // 總額 1000、總度數 200 → 單價 5
        // 私電：A 100 度 = 500、B 60 度 = 300 → 公電 = 200
        // 分母：A 2人*10天=20、B 1人*20天=20 → 每人日公電 = 5
        var tenants = new List<ElectricityTenantInput>
        {
            new(TenantUnits: 100m, OccupantCount: 2, OccupancyDays: 10),
            new(TenantUnits: 60m, OccupantCount: 1, OccupancyDays: 20)
        };
        var request = new ElectricityCalculateRequest(3, UnitPrice: null, TenantUnits: null, BillAmount: 1000m, TotalUnits: 200m, Tenants: tenants);

        var result = ElectricityCalculationService.Calculate(request);

        Assert.Equal(5m, result.UnitPrice);
        Assert.Equal(800m, result.PrivateElectricityAmount);
        Assert.Equal(200m, result.PublicElectricityAmount);
        Assert.NotNull(result.TenantPayables);
        Assert.Equal(600m, result.TenantPayables![0]); // 500 私電 + 100 公電
        Assert.Equal(400m, result.TenantPayables![1]); // 300 私電 + 100 公電
        Assert.Equal(1000m, result.PayableAmount);
    }

    [Fact]
    public void Rule4_MultiMeterAverage_UsesBlendedUnitPrice()
    {
        // 多帳單加總後：總額 1000、總度數 200 → 平均單價 5
        // 私電：A 100 度 = 500、B 60 度 = 300 → 公電 = 200
        // 分母：A 2人*10天=20、B 1人*20天=20 → 每人日公電 = 5
        var tenants = new List<ElectricityTenantInput>
        {
            new(TenantUnits: 100m, OccupantCount: 2, OccupancyDays: 10),
            new(TenantUnits: 60m, OccupantCount: 1, OccupancyDays: 20)
        };
        var request = new ElectricityCalculateRequest(4, UnitPrice: null, TenantUnits: null, BillAmount: 1000m, TotalUnits: 200m, Tenants: tenants);

        var result = ElectricityCalculationService.Calculate(request);

        Assert.Equal(5m, result.UnitPrice);
        Assert.Equal(800m, result.PrivateElectricityAmount);
        Assert.Equal(200m, result.PublicElectricityAmount);
        Assert.NotNull(result.TenantPayables);
        Assert.Equal(600m, result.TenantPayables![0]);
        Assert.Equal(400m, result.TenantPayables![1]);
        Assert.Equal(1000m, result.PayableAmount);
    }

    [Fact]
    public void Rule3_NoTenants_PublicEqualsTotal()
    {
        var request = new ElectricityCalculateRequest(3, UnitPrice: null, TenantUnits: null, BillAmount: 1000m, TotalUnits: 200m, Tenants: []);

        var result = ElectricityCalculationService.Calculate(request);

        Assert.Equal(0m, result.PrivateElectricityAmount);
        Assert.Equal(1000m, result.PublicElectricityAmount);
        Assert.Equal(0m, result.PayableAmount);
    }

    [Fact]
    public void UnknownRule_Throws()
    {
        var request = new ElectricityCalculateRequest(99, null, null, null, null, null);

        Assert.Throws<DomainValidationException>(() => ElectricityCalculationService.Calculate(request));
    }

    [Fact]
    public void ResolveAllocationRates_ComputesRatesFromAllocations()
    {
        var allocations = new List<ElectricityAllocationSaveInput>
        {
            new(1, 1, 1, 1, DateTime.UtcNow, DateTime.UtcNow, null, null, TenantUnits: 100m, OccupantCount: 2, OccupancyDays: 10),
            new(1, 2, 2, 2, DateTime.UtcNow, DateTime.UtcNow, null, null, TenantUnits: 60m, OccupantCount: 1, OccupancyDays: 20)
        };

        var (unitPrice, privateTotal, publicTotal, avgDailyPublic) =
            ElectricityCalculationService.ResolveAllocationRates(1000m, 200m, allocations);

        Assert.Equal(5m, unitPrice);
        Assert.Equal(800m, privateTotal);
        Assert.Equal(200m, publicTotal);
        Assert.Equal(5m, avgDailyPublic);
    }

    [Theory]
    [InlineData("2026-01-01", "2026-01-31", 31)]
    [InlineData("2026-01-01", "2026-01-01", 1)]
    [InlineData("2026-01-02", "2026-01-01", 0)]
    public void CalculateInclusiveDays_CountsBothEnds(string start, string end, int expected)
    {
        Assert.Equal(expected, ElectricityCalculationService.CalculateInclusiveDays(DateTime.Parse(start), DateTime.Parse(end)));
    }

    [Theory]
    [InlineData("123.99", "123")]
    [InlineData("123.01", "123")]
    [InlineData("123", "123")]
    public void TruncateToInteger_FloorsAmount(string input, string expected)
    {
        Assert.Equal(decimal.Parse(expected), ElectricityCalculationService.TruncateToInteger(decimal.Parse(input)));
    }
}
