namespace RentalManager.Api.Models;

public enum TenantType { Person = 1, Company = 2 }
public enum UserRoleType { Admin = 1, Tenant = 2 }
public enum ContractStatus { Active = 1, Expired = 2, Terminated = 3 }
public enum PaymentIntervalType { Monthly = 1, Quarterly = 3, Yearly = 12 }
public enum ElectricityRuleType { Unit = 1, Average = 2, MultiMeter = 3 }
public enum ElectricityAllocationTargetType { Tenant = 1, Landlord = 2 }
public enum ChargeCategory { Rent = 1, Water = 2, Electricity = 3, Other = 99 }
public enum ExpenseCategory { Water = 1, Electricity = 2, Gas = 3, Internet = 4, Television = 5, Management = 6, Parking = 7, Tax = 8, Repair = 9, Other = 99 }
public enum ExpenseSplitStatus { Unsplit = 1, Split = 2, NoNeed = 3 }
public enum AttachmentEntityType { Contract = 1, ChargeRecord = 2, ExpenseRecord = 3, RepairTicket = 4 }

public class AppUser
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRoleType Role { get; set; } = UserRoleType.Admin;
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
    /// <summary>租客帳號對應的租客；管理員帳號為 null。</summary>
    public int? TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class Tenant
{
    public int Id { get; set; }
    public TenantType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime? BirthdayUtc { get; set; }
    public string? TaxId { get; set; }
    public string? PersonalId { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class PropertyUnit
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class PropertyRoom
{
    public int Id { get; set; }
    public int PropertyUnitId { get; set; }
    public PropertyUnit? PropertyUnit { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class Contract
{
    public int Id { get; set; }
    public string ContractNo { get; set; } = string.Empty;
    public string ContractName { get; set; } = string.Empty;
    public int TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public int? PropertyUnitId { get; set; }
    public PropertyUnit? PropertyUnit { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public string PropertyAddress { get; set; } = string.Empty;
    public DateTime StartDateUtc { get; set; }
    public DateTime EndDateUtc { get; set; }
    public decimal MonthlyRent { get; set; }
    public int PaymentIntervalMonths { get; set; } = 1;
    public decimal PeriodPayableAmount { get; set; }
    public decimal Deposit { get; set; }
    public int OccupantCount { get; set; }
    public string? Notes { get; set; }
    public ElectricityRuleType ElectricityRuleType { get; set; }
    public ContractStatus Status { get; set; } = ContractStatus.Active;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class ContractRoom
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public Contract? Contract { get; set; }
    public int PropertyRoomId { get; set; }
    public PropertyRoom? PropertyRoom { get; set; }
}

public class ChargeRecord
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public Contract? Contract { get; set; }
    public ChargeCategory Category { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime BillingStartUtc { get; set; }
    public DateTime BillingEndUtc { get; set; }
    public decimal? MeterStart { get; set; }
    public decimal? MeterEnd { get; set; }
    public decimal? UsageUnits { get; set; }
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
    public bool IsPaid { get; set; }
    public DateTime? PaidAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class ExpenseRecord
{
    public int Id { get; set; }
    public int PropertyUnitId { get; set; }
    public PropertyUnit? PropertyUnit { get; set; }
    public int? PropertyRoomId { get; set; }
    public PropertyRoom? PropertyRoom { get; set; }
    public ExpenseCategory Category { get; set; }
    public DateTime BillingStartUtc { get; set; }
    public DateTime BillingEndUtc { get; set; }
    public decimal Amount { get; set; }
    public decimal? UsageUnits { get; set; }
    public ExpenseSplitStatus SplitStatus { get; set; } = ExpenseSplitStatus.Unsplit;
    public string? Notes { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class ElectricityBill
{
    public int Id { get; set; }
    public int? ContractId { get; set; }
    public Contract? Contract { get; set; }
    public ElectricityRuleType RuleType { get; set; }
    public DateTime BillingStartUtc { get; set; }
    public DateTime BillingEndUtc { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalUnits { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal PrivateTotalAmount { get; set; }
    public decimal PublicTotalAmount { get; set; }
    public decimal PayableTotalAmount { get; set; }
    public bool ChargesCreated { get; set; }
    public DateTime? ChargesCreatedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class ElectricityAllocation
{
    public int Id { get; set; }
    public int ElectricityBillId { get; set; }
    public ElectricityBill? ElectricityBill { get; set; }
    public ElectricityAllocationTargetType TargetType { get; set; } = ElectricityAllocationTargetType.Tenant;
    public int? ContractId { get; set; }
    public Contract? Contract { get; set; }
    public int PropertyRoomId { get; set; }
    public PropertyRoom? PropertyRoom { get; set; }
    public int? TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public DateTime OccupancyStartUtc { get; set; }
    public DateTime OccupancyEndUtc { get; set; }
    public decimal? MeterStart { get; set; }
    public decimal? MeterEnd { get; set; }
    public decimal TenantUnits { get; set; }
    public int OccupantCount { get; set; }
    public int OccupancyDays { get; set; }
    public decimal PrivateAmount { get; set; }
    public decimal PublicAmount { get; set; }
    public decimal PayableAmount { get; set; }
}

public class Attachment
{
    public int Id { get; set; }
    public AttachmentEntityType EntityType { get; set; }
    public int EntityId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    /// <summary>相對於儲存根目錄的檔案路徑。</summary>
    public string StoragePath { get; set; } = string.Empty;
    public int? UploadedByUserId { get; set; }
    public AppUser? UploadedBy { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class ElectricityMeterReading
{
    public int Id { get; set; }
    public int PropertyUnitId { get; set; }
    public PropertyUnit? PropertyUnit { get; set; }
    public int PropertyRoomId { get; set; }
    public PropertyRoom? PropertyRoom { get; set; }
    public DateTime ReadingDateUtc { get; set; }
    public decimal ReadingValue { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
