namespace RentalManager.Api.Dtos;

public record ElectricityExpensePreviewRequest(List<int> ExpenseBillIds);
public record ElectricityTenantInput(decimal TenantUnits, int OccupantCount, int OccupancyDays);
public record ElectricityCalculateRequest(int RuleType, decimal? UnitPrice, decimal? TenantUnits, decimal? BillAmount, decimal? TotalUnits, List<ElectricityTenantInput>? Tenants);
public record ElectricityCalculateResponse(decimal UnitPrice, decimal PrivateElectricityAmount, decimal PublicElectricityAmount, decimal PayableAmount, List<decimal>? TenantPayables);
public record ElectricityAllocationSaveInput(int TargetType, int? ContractId, int PropertyRoomId, int? TenantId, DateTime OccupancyStartUtc, DateTime OccupancyEndUtc, decimal? MeterStart, decimal? MeterEnd, decimal TenantUnits, int OccupantCount, int OccupancyDays);
public record ElectricityBillSaveRequest(int? ContractId, int RuleType, DateTime BillingStartUtc, DateTime BillingEndUtc, decimal TotalAmount, decimal TotalUnits, decimal UnitPrice, List<ElectricityAllocationSaveInput> Allocations);
public record ElectricityCreateChargesRequest(List<int>? ExpenseBillIds);

public record ElectricityPreviewBillItem(
    int Id,
    int PropertyUnitId,
    string? PropertyUnitName,
    int? PropertyRoomId,
    string? PropertyRoomName,
    DateTime BillingStartUtc,
    DateTime BillingEndUtc,
    decimal Amount,
    decimal UsageUnits
);

public record ElectricityPreviewAllocationItem(
    int TargetType,
    string TargetName,
    int ExpenseBillId,
    decimal ExpenseBillAmount,
    decimal ExpenseBillUnits,
    int PropertyUnitId,
    string? PropertyUnitName,
    int PropertyRoomId,
    string PropertyRoomName,
    int? ContractId,
    string? ContractNo,
    int? TenantId,
    string? TenantName,
    int OccupantCount,
    DateTime OccupancyStartUtc,
    DateTime OccupancyEndUtc,
    int OccupancyDays,
    decimal TenantUnits,
    decimal? MeterStart,
    decimal? MeterEnd,
    DateTime? MeterStartDateUtc,
    DateTime? MeterEndDateUtc
);

public record ElectricityPreviewResponse(
    List<ElectricityPreviewBillItem> Bills,
    List<ElectricityPreviewAllocationItem> Allocations,
    List<string> Warnings
);

public record ElectricityBillSaveResponse(int BillId, decimal PayableTotalAmount);

public record ElectricityBillListItemResponse(
    int Id,
    int? ContractId,
    string? ContractNo,
    DateTime BillingStartUtc,
    DateTime BillingEndUtc,
    decimal TotalAmount,
    decimal TotalUnits,
    decimal UnitPrice,
    decimal PayableTotalAmount,
    bool ChargesCreated
);

public record ElectricityBillAllocationDetail(
    int Id,
    int TargetType,
    int? ContractId,
    string? ContractNo,
    int PropertyRoomId,
    string? PropertyRoomName,
    int? TenantId,
    string? TenantName,
    DateTime OccupancyStartUtc,
    DateTime OccupancyEndUtc,
    decimal? MeterStart,
    decimal? MeterEnd,
    decimal TenantUnits,
    int OccupantCount,
    int OccupancyDays,
    decimal PrivateAmount,
    decimal PublicAmount,
    decimal PayableAmount
);

public record ElectricityBillDetailResponse(
    int Id,
    int? ContractId,
    string? ContractNo,
    DateTime BillingStartUtc,
    DateTime BillingEndUtc,
    decimal TotalAmount,
    decimal TotalUnits,
    decimal UnitPrice,
    decimal PrivateTotalAmount,
    decimal PublicTotalAmount,
    decimal PayableTotalAmount,
    List<ElectricityBillAllocationDetail> Allocations
);

public record ElectricityCreateChargesResponse(int CreatedCount);
