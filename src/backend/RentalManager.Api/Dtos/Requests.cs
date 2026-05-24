namespace RentalManager.Api.Dtos;

public record LoginRequest(string Username, string Password);
public record LoginResponse(string Token, int UserId, string Username, int Role);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public record ElectricityTenantInput(decimal TenantUnits, int OccupantCount, int OccupancyDays);
public record MultiMeterInput(decimal TotalAmount, decimal TotalUnits, List<ElectricityTenantInput> Tenants);
public record ElectricityCalculateRequest(int RuleType, decimal? UnitPrice, decimal? TenantUnits, decimal? BillAmount, decimal? TotalUnits, List<ElectricityTenantInput>? Tenants);
public record ElectricityCalculateResponse(decimal UnitPrice, decimal PrivateElectricityAmount, decimal PublicElectricityAmount, decimal PayableAmount, List<decimal>? TenantPayables);
public record ElectricityAllocationSaveInput(int? TenantId, decimal TenantUnits, int OccupantCount, int OccupancyDays);
public record ElectricityBillSaveRequest(int ContractId, int RuleType, DateTime BillingStartUtc, DateTime BillingEndUtc, decimal TotalAmount, decimal TotalUnits, decimal UnitPrice, List<ElectricityAllocationSaveInput> Allocations);
