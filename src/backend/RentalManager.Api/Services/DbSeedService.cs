using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RentalManager.Api.Data;
using RentalManager.Api.Models;

namespace RentalManager.Api.Services;

public class AppSeedData
{
    public List<AdminUser> AdminUsers { get; set; } = [];
    public List<PropertyUnit> PropertyUnits { get; set; } = [];
    public List<PropertyRoom> PropertyRooms { get; set; } = [];
    public List<Tenant> Tenants { get; set; } = [];
    public List<Contract> Contracts { get; set; } = [];
    public List<ContractRoom> ContractRooms { get; set; } = [];
    public List<ChargeRecord> ChargeRecords { get; set; } = [];
    public List<ExpenseRecord> ExpenseRecords { get; set; } = [];
    public List<ElectricityBill> ElectricityBills { get; set; } = [];
    public List<ElectricityAllocation> ElectricityAllocations { get; set; } = [];
    public List<ElectricityMeterReading> ElectricityMeterReadings { get; set; } = [];
}

public class DbSeedService(AppDbContext db, IWebHostEnvironment env, IConfiguration configuration)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public async Task ExportAsync(string? templatePath = null)
    {
        var fullPath = ResolveTemplatePath(templatePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        var data = new AppSeedData
        {
            AdminUsers = await db.AdminUsers.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            PropertyUnits = await db.PropertyUnits.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            PropertyRooms = await db.PropertyRooms.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            Tenants = await db.Tenants.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            Contracts = await db.Contracts.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            ContractRooms = await db.ContractRooms.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            ChargeRecords = await db.ChargeRecords.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            ExpenseRecords = await db.ExpenseRecords.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            ElectricityBills = await db.ElectricityBills.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            ElectricityAllocations = await db.ElectricityAllocations.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            ElectricityMeterReadings = await db.ElectricityMeterReadings.AsNoTracking().OrderBy(x => x.Id).ToListAsync()
        };

        var json = JsonSerializer.Serialize(data, JsonOptions);
        await File.WriteAllTextAsync(fullPath, json);
    }

    public async Task ApplyTemplateIfEmptyAsync()
    {
        if (!IsApplyOnStartupEnabled()) return;
        if (!await IsBusinessDataEmptyAsync()) return;

        var fullPath = ResolveTemplatePath();
        if (!File.Exists(fullPath)) return;

        var json = await File.ReadAllTextAsync(fullPath);
        var data = JsonSerializer.Deserialize<AppSeedData>(json, JsonOptions);
        if (data is null) return;

        await ImportAsync(data);
    }

    private async Task ImportAsync(AppSeedData data)
    {
        if (data.AdminUsers.Count > 0)
        {
            var existingAdminIds = await db.AdminUsers.AsNoTracking().Select(x => x.Id).ToListAsync();
            db.AdminUsers.AddRange(data.AdminUsers.Where(x => !existingAdminIds.Contains(x.Id)));
        }

        if (data.PropertyUnits.Count > 0) db.PropertyUnits.AddRange(data.PropertyUnits);
        if (data.PropertyRooms.Count > 0) db.PropertyRooms.AddRange(data.PropertyRooms);
        if (data.Tenants.Count > 0) db.Tenants.AddRange(data.Tenants);
        if (data.Contracts.Count > 0) db.Contracts.AddRange(data.Contracts);
        if (data.ContractRooms.Count > 0) db.ContractRooms.AddRange(data.ContractRooms);
        if (data.ChargeRecords.Count > 0) db.ChargeRecords.AddRange(data.ChargeRecords);
        if (data.ExpenseRecords.Count > 0) db.ExpenseRecords.AddRange(data.ExpenseRecords);
        if (data.ElectricityBills.Count > 0) db.ElectricityBills.AddRange(data.ElectricityBills);
        if (data.ElectricityAllocations.Count > 0) db.ElectricityAllocations.AddRange(data.ElectricityAllocations);
        if (data.ElectricityMeterReadings.Count > 0) db.ElectricityMeterReadings.AddRange(data.ElectricityMeterReadings);

        await db.SaveChangesAsync();
        await ResetSequencesAsync();
    }

    private async Task<bool> IsBusinessDataEmptyAsync()
    {
        return !await db.PropertyUnits.AnyAsync()
            && !await db.PropertyRooms.AnyAsync()
            && !await db.Tenants.AnyAsync()
            && !await db.Contracts.AnyAsync()
            && !await db.ContractRooms.AnyAsync()
            && !await db.ChargeRecords.AnyAsync()
            && !await db.ExpenseRecords.AnyAsync()
            && !await db.ElectricityBills.AnyAsync()
            && !await db.ElectricityAllocations.AnyAsync()
            && !await db.ElectricityMeterReadings.AnyAsync();
    }

    private bool IsApplyOnStartupEnabled()
    {
        return configuration.GetValue<bool?>("SeedData:ApplyOnStartup") ?? true;
    }

    private string ResolveTemplatePath(string? configuredPath = null)
    {
        var path = configuredPath ?? configuration["SeedData:TemplatePath"] ?? "Seed/seed-template.json";
        if (Path.IsPathRooted(path)) return path;
        return Path.Combine(env.ContentRootPath, path);
    }

    private async Task ResetSequencesAsync()
    {
        foreach (var table in new[]
                 {
                     "AdminUsers",
                     "PropertyUnits",
                     "PropertyRooms",
                     "Tenants",
                     "Contracts",
                     "ContractRooms",
                     "ChargeRecords",
                     "ExpenseRecords",
                     "ElectricityBills",
                     "ElectricityAllocations",
                     "ElectricityMeterReadings"
                 })
        {
            var sql = $"""
                SELECT setval(
                    pg_get_serial_sequence('"{table}"', 'Id'),
                    COALESCE(MAX("Id"), 1),
                    MAX("Id") IS NOT NULL
                )
                FROM "{table}";
                """;
            await db.Database.ExecuteSqlRawAsync(sql);
        }
    }
}
