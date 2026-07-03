using RentalManager.Api.Models;

namespace RentalManager.Api.Dtos;

public record ContractUpsertRequest(
    string ContractNo,
    string ContractName,
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
    DateTime? ReferenceDateUtc,
    DateTime? TargetMonthUtc
);

public record ContractRoomInfo(
    int PropertyRoomId,
    int PropertyUnitId,
    string PropertyUnitName,
    string RoomCode,
    string RoomName
);

public record ContractResponse(
    int Id,
    string ContractNo,
    string ContractName,
    int TenantId,
    TenantResponse? Tenant,
    int? PropertyUnitId,
    string PropertyName,
    string PropertyAddress,
    DateTime StartDateUtc,
    DateTime EndDateUtc,
    decimal MonthlyRent,
    int PaymentIntervalMonths,
    decimal PeriodPayableAmount,
    decimal Deposit,
    int OccupantCount,
    string? Notes,
    ElectricityRuleType ElectricityRuleType,
    ContractStatus Status,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    List<int> PropertyRoomIds,
    List<ContractRoomInfo> Rooms
)
{
    public static ContractResponse From(Contract c, List<ContractRoomInfo>? rooms = null) => new(
        c.Id, c.ContractNo, c.ContractName, c.TenantId,
        c.Tenant is null ? null : TenantResponse.From(c.Tenant),
        c.PropertyUnitId, c.PropertyName, c.PropertyAddress,
        c.StartDateUtc, c.EndDateUtc, c.MonthlyRent, c.PaymentIntervalMonths, c.PeriodPayableAmount,
        c.Deposit, c.OccupantCount, c.Notes, c.ElectricityRuleType, c.Status,
        c.CreatedAtUtc, c.UpdatedAtUtc,
        rooms?.Select(r => r.PropertyRoomId).ToList() ?? [],
        rooms ?? []);
}

public record ContractBatchChargeCreatedItem(int Id, string ContractNo, DateTime BillingStartUtc, DateTime BillingEndUtc, decimal Amount);
public record ContractBatchChargeSkippedItem(int Id, string ContractNo, string Reason);

public record ContractBatchChargeCreateResponse(
    DateTime TargetMonthUtc,
    int CreatedCount,
    int SkippedCount,
    List<ContractBatchChargeCreatedItem> Created,
    List<ContractBatchChargeSkippedItem> Skipped
);
