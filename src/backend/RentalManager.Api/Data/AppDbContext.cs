using Microsoft.EntityFrameworkCore;
using RentalManager.Api.Models;

namespace RentalManager.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<PropertyUnit> PropertyUnits => Set<PropertyUnit>();
    public DbSet<PropertyRoom> PropertyRooms => Set<PropertyRoom>();
    public DbSet<ContractRoom> ContractRooms => Set<ContractRoom>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<ChargeRecord> ChargeRecords => Set<ChargeRecord>();
    public DbSet<ExpenseRecord> ExpenseRecords => Set<ExpenseRecord>();
    public DbSet<ElectricityBill> ElectricityBills => Set<ElectricityBill>();
    public DbSet<ElectricityAllocation> ElectricityAllocations => Set<ElectricityAllocation>();
    public DbSet<ElectricityMeterReading> ElectricityMeterReadings => Set<ElectricityMeterReading>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Contract>()
            .HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Contract>()
            .HasOne(x => x.PropertyUnit)
            .WithMany()
            .HasForeignKey(x => x.PropertyUnitId)
            .OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<PropertyRoom>()
            .HasOne(x => x.PropertyUnit)
            .WithMany()
            .HasForeignKey(x => x.PropertyUnitId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ContractRoom>()
            .HasOne(x => x.Contract)
            .WithMany()
            .HasForeignKey(x => x.ContractId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ContractRoom>()
            .HasOne(x => x.PropertyRoom)
            .WithMany()
            .HasForeignKey(x => x.PropertyRoomId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ChargeRecord>()
            .HasOne(x => x.Contract)
            .WithMany()
            .HasForeignKey(x => x.ContractId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ExpenseRecord>()
            .HasOne(x => x.PropertyUnit)
            .WithMany()
            .HasForeignKey(x => x.PropertyUnitId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ExpenseRecord>()
            .HasOne(x => x.PropertyRoom)
            .WithMany()
            .HasForeignKey(x => x.PropertyRoomId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<AdminUser>().HasData(new AdminUser
        {
            Id = 1,
            Username = "admin",
            Role = UserRoleType.Admin,
            CreatedAtUtc = new DateTime(2026, 5, 24, 4, 11, 21, 638, DateTimeKind.Utc).AddTicks(8194),
            PasswordHash = "$2a$11$HlDgIuiyVxHd356KWpNR4OtSocmp5RQNYprtQXuKFl6jBlbaRkq3a"
        });

        modelBuilder.Entity<ElectricityBill>()
            .HasOne(x => x.Contract)
            .WithMany()
            .HasForeignKey(x => x.ContractId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ElectricityAllocation>()
            .HasOne(x => x.ElectricityBill)
            .WithMany()
            .HasForeignKey(x => x.ElectricityBillId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ElectricityAllocation>()
            .HasOne(x => x.Contract)
            .WithMany()
            .HasForeignKey(x => x.ContractId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ElectricityAllocation>()
            .HasOne(x => x.PropertyRoom)
            .WithMany()
            .HasForeignKey(x => x.PropertyRoomId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ElectricityAllocation>()
            .HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ElectricityMeterReading>()
            .HasOne(x => x.PropertyUnit)
            .WithMany()
            .HasForeignKey(x => x.PropertyUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ElectricityMeterReading>()
            .HasOne(x => x.PropertyRoom)
            .WithMany()
            .HasForeignKey(x => x.PropertyRoomId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ElectricityMeterReading>()
            .HasIndex(x => new { x.PropertyRoomId, x.ReadingDateUtc })
            .IsUnique();
    }
}
