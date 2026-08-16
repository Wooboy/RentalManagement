using Microsoft.EntityFrameworkCore;
using RentalManager.Api.Common;
using RentalManager.Api.Data;
using RentalManager.Api.Dtos;
using RentalManager.Api.Models;

namespace RentalManager.Api.Services;

/// <summary>電費帳單的分帳預覽、儲存與轉入應收流程。</summary>
public class ElectricityBillingService(AppDbContext db)
{
    private sealed class PreviewAllocationRow
    {
        public required ExpenseRecord ExpenseBill { get; init; }
        public required PropertyRoom Room { get; init; }
        public ElectricityAllocationTargetType TargetType { get; init; }
        public Contract? Contract { get; init; }
        public decimal TenantUnits { get; set; }
        public decimal? MeterStart { get; set; }
        public decimal? MeterEnd { get; set; }
        public DateTime? MeterStartDateUtc { get; set; }
        public DateTime? MeterEndDateUtc { get; set; }
        public int OccupantCount { get; set; }
        public DateTime OccupancyStartUtc { get; set; }
        public DateTime OccupancyEndUtc { get; set; }
    }

    public async Task<ElectricityPreviewResponse> PreviewFromExpensesAsync(ElectricityExpensePreviewRequest request)
    {
        var expenseBillIds = (request.ExpenseBillIds ?? []).Distinct().Where(x => x > 0).ToList();
        if (expenseBillIds.Count == 0) throw new DomainValidationException("請至少選擇一張電費帳單");

        var expenseBills = await db.ExpenseRecords
            .Include(x => x.PropertyUnit)
            .Include(x => x.PropertyRoom)
            .Where(x => expenseBillIds.Contains(x.Id) && x.Category == ExpenseCategory.Electricity)
            .OrderBy(x => x.BillingStartUtc)
            .ThenBy(x => x.Id)
            .ToListAsync();
        if (expenseBills.Count != expenseBillIds.Count) throw new DomainValidationException("部分電費帳單不存在");

        var billRoomsMap = await ResolveExpenseBillRoomsAsync(expenseBills);
        var roomIds = billRoomsMap.Values.SelectMany(x => x).Select(x => x.Id).Distinct().ToList();
        if (roomIds.Count == 0) throw new DomainValidationException("找不到可分帳的房間");

        var contracts = await db.ContractRooms
            .Include(x => x.Contract)!.ThenInclude(x => x!.Tenant)
            .Include(x => x.PropertyRoom)
            .Where(x => roomIds.Contains(x.PropertyRoomId) && x.Contract != null)
            .ToListAsync();

        var readings = await db.ElectricityMeterReadings
            .Where(x => roomIds.Contains(x.PropertyRoomId))
            .OrderBy(x => x.ReadingDateUtc)
            .ThenBy(x => x.Id)
            .ToListAsync();

        var rows = new List<PreviewAllocationRow>();
        var warnings = new List<string>();

        foreach (var bill in expenseBills)
        {
            if (!billRoomsMap.TryGetValue(bill.Id, out var billRooms) || billRooms.Count == 0)
            {
                warnings.Add($"帳單#{bill.Id} 找不到可分帳房間");
                continue;
            }

            foreach (var billRoom in billRooms)
            {
                var roomId = billRoom.Id;
                var roomContracts = contracts
                    .Where(x => x.PropertyRoomId == roomId && x.Contract != null && HasOverlap(x.Contract.StartDateUtc, x.Contract.EndDateUtc, bill.BillingStartUtc, bill.BillingEndUtc))
                    .Select(x => new { x.PropertyRoom, Contract = x.Contract! })
                    .OrderBy(x => x.Contract.StartDateUtc)
                    .ThenBy(x => x.Contract.Id)
                    .ToList();

                if (roomContracts.Count == 0)
                {
                    warnings.Add($"帳單#{bill.Id} 房間 {billRoom.Name} 找不到帳期內有效合約");
                    continue;
                }

                var roomReadings = BuildBillReadingSeries(
                    readings.Where(x => x.PropertyRoomId == roomId).ToList(),
                    bill.BillingStartUtc,
                    bill.BillingEndUtc);

                if (roomReadings.Count < 2)
                {
                    warnings.Add($"帳單#{bill.Id} 房間 {billRoom.Name} 抄表資料不足，至少需要帳期起訖邊界各一筆讀數");
                }

                foreach (var contractLink in roomContracts)
                {
                    rows.Add(new PreviewAllocationRow
                    {
                        ExpenseBill = bill,
                        Contract = contractLink.Contract,
                        Room = contractLink.PropertyRoom!,
                        TargetType = ElectricityAllocationTargetType.Tenant,
                        TenantUnits = 0,
                        MeterStart = null,
                        MeterEnd = null,
                        OccupantCount = contractLink.Contract.OccupantCount,
                        OccupancyStartUtc = MaxDate(contractLink.Contract.StartDateUtc, bill.BillingStartUtc),
                        OccupancyEndUtc = MinDate(contractLink.Contract.EndDateUtc, bill.BillingEndUtc)
                    });
                }

                for (var index = 0; index < roomReadings.Count - 1; index++)
                {
                    var startReading = roomReadings[index];
                    var endReading = roomReadings[index + 1];
                    var units = endReading.ReadingValue - startReading.ReadingValue;
                    if (units < 0)
                    {
                        warnings.Add($"帳單#{bill.Id} 房間 {billRoom.Name} 在 {startReading.ReadingDateUtc:yyyy-MM-dd} 到 {endReading.ReadingDateUtc:yyyy-MM-dd} 出現倒退讀數");
                        continue;
                    }

                    var owner = roomContracts.FirstOrDefault(x =>
                        x.Contract.StartDateUtc.Date <= startReading.ReadingDateUtc.Date &&
                        x.Contract.EndDateUtc.Date >= startReading.ReadingDateUtc.Date);
                    if (owner is null)
                    {
                        var landlordRow = rows.FirstOrDefault(x =>
                            x.ExpenseBill.Id == bill.Id &&
                            x.TargetType == ElectricityAllocationTargetType.Landlord &&
                            x.Room.Id == roomId);
                        if (landlordRow is null)
                        {
                            landlordRow = new PreviewAllocationRow
                            {
                                ExpenseBill = bill,
                                Contract = null,
                                Room = billRoom,
                                TargetType = ElectricityAllocationTargetType.Landlord,
                                TenantUnits = 0,
                                MeterStart = null,
                                MeterEnd = null,
                                MeterStartDateUtc = null,
                                MeterEndDateUtc = null,
                                OccupantCount = 0,
                                OccupancyStartUtc = startReading.ReadingDateUtc.Date,
                                OccupancyEndUtc = endReading.ReadingDateUtc.Date.AddDays(-1)
                            };
                            rows.Add(landlordRow);
                        }
                        else
                        {
                            landlordRow.OccupancyStartUtc = MinDate(landlordRow.OccupancyStartUtc, startReading.ReadingDateUtc);
                            landlordRow.OccupancyEndUtc = MaxDate(landlordRow.OccupancyEndUtc, endReading.ReadingDateUtc.Date.AddDays(-1));
                        }

                        landlordRow.TenantUnits += units;
                        landlordRow.MeterStart ??= startReading.ReadingValue;
                        landlordRow.MeterStartDateUtc ??= startReading.ReadingDateUtc;
                        landlordRow.MeterEnd = endReading.ReadingValue;
                        landlordRow.MeterEndDateUtc = endReading.ReadingDateUtc;
                        continue;
                    }

                    var row = rows.First(x => x.ExpenseBill.Id == bill.Id && x.TargetType == ElectricityAllocationTargetType.Tenant && x.Contract!.Id == owner.Contract.Id && x.Room.Id == roomId);
                    row.TenantUnits += units;
                    row.MeterStart ??= startReading.ReadingValue;
                    row.MeterStartDateUtc ??= startReading.ReadingDateUtc;
                    row.MeterEnd = endReading.ReadingValue;
                    row.MeterEndDateUtc = endReading.ReadingDateUtc;
                }
            }
        }

        var allocationRows = rows
            .Select(x => new ElectricityPreviewAllocationItem(
                (int)x.TargetType,
                x.TargetType == ElectricityAllocationTargetType.Landlord ? "房東自付" : "租客",
                x.ExpenseBill.Id,
                x.ExpenseBill.Amount,
                x.ExpenseBill.UsageUnits ?? 0,
                x.ExpenseBill.PropertyUnitId,
                x.ExpenseBill.PropertyUnit?.Name,
                x.Room.Id,
                x.Room.Name,
                x.Contract?.Id,
                x.Contract?.ContractNo,
                x.Contract?.TenantId,
                x.Contract?.Tenant?.Name,
                x.OccupantCount,
                x.OccupancyStartUtc,
                x.OccupancyEndUtc,
                ElectricityCalculationService.CalculateInclusiveDays(x.OccupancyStartUtc, x.OccupancyEndUtc),
                x.TenantUnits,
                x.MeterStart,
                x.MeterEnd,
                x.MeterStartDateUtc,
                x.MeterEndDateUtc))
            .OrderBy(x => x.ExpenseBillId)
            .ThenBy(x => x.PropertyRoomId)
            .ThenBy(x => x.OccupancyStartUtc)
            .ThenBy(x => x.ContractId)
            .ToList();

        var billItems = expenseBills.Select(x => new ElectricityPreviewBillItem(
            x.Id,
            x.PropertyUnitId,
            x.PropertyUnit?.Name,
            x.PropertyRoomId,
            x.PropertyRoom?.Name,
            x.BillingStartUtc,
            x.BillingEndUtc,
            x.Amount,
            x.UsageUnits ?? 0)).ToList();

        return new ElectricityPreviewResponse(billItems, allocationRows, warnings.Distinct().ToList());
    }

    public async Task<ElectricityBillSaveResponse> SaveBillAsync(ElectricityBillSaveRequest request)
    {
        if (request.BillingEndUtc < request.BillingStartUtc) throw new DomainValidationException("帳期結束不可早於開始");
        if (request.Allocations is null || request.Allocations.Count == 0) throw new DomainValidationException("無分攤資料");

        var contractIds = request.Allocations.Where(x => x.ContractId.HasValue).Select(x => x.ContractId!.Value).Distinct().ToList();
        var roomIds = request.Allocations.Select(x => x.PropertyRoomId).Distinct().ToList();
        if (request.ContractId.HasValue && !await db.Contracts.AnyAsync(x => x.Id == request.ContractId.Value)) throw new DomainValidationException("主合約不存在");
        if (await db.Contracts.CountAsync(x => contractIds.Contains(x.Id)) != contractIds.Count) throw new DomainValidationException("部分分攤合約不存在");
        if (await db.PropertyRooms.CountAsync(x => roomIds.Contains(x.Id)) != roomIds.Count) throw new DomainValidationException("部分分攤房間不存在");

        var (unitPrice, privateTotal, publicTotal, avgPublic) =
            ElectricityCalculationService.ResolveAllocationRates(request.TotalAmount, request.TotalUnits, request.Allocations);

        var bill = new ElectricityBill
        {
            ContractId = request.ContractId,
            RuleType = (ElectricityRuleType)request.RuleType,
            BillingStartUtc = request.BillingStartUtc,
            BillingEndUtc = request.BillingEndUtc,
            TotalAmount = request.TotalAmount,
            TotalUnits = request.TotalUnits,
            UnitPrice = unitPrice,
            PrivateTotalAmount = privateTotal,
            PublicTotalAmount = publicTotal,
            PayableTotalAmount = 0,
            ChargesCreated = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        db.ElectricityBills.Add(bill);
        await db.SaveChangesAsync();

        var allocations = request.Allocations.Select(x =>
        {
            var privateAmount = x.TenantUnits * unitPrice;
            var publicAmount = x.OccupantCount * x.OccupancyDays * avgPublic;
            var payable = privateAmount + publicAmount;
            return new ElectricityAllocation
            {
                ElectricityBillId = bill.Id,
                TargetType = (ElectricityAllocationTargetType)x.TargetType,
                ContractId = x.ContractId,
                PropertyRoomId = x.PropertyRoomId,
                TenantId = x.TenantId,
                OccupancyStartUtc = x.OccupancyStartUtc,
                OccupancyEndUtc = x.OccupancyEndUtc,
                MeterStart = x.MeterStart,
                MeterEnd = x.MeterEnd,
                TenantUnits = x.TenantUnits,
                OccupantCount = x.OccupantCount,
                OccupancyDays = x.OccupancyDays,
                PrivateAmount = privateAmount,
                PublicAmount = publicAmount,
                PayableAmount = payable
            };
        }).ToList();

        bill.PayableTotalAmount = allocations.Sum(x => x.PayableAmount);
        db.ElectricityAllocations.AddRange(allocations);
        await db.SaveChangesAsync();

        return new ElectricityBillSaveResponse(bill.Id, bill.PayableTotalAmount);
    }

    public async Task<List<ElectricityBillListItemResponse>> GetBillsAsync(int? contractId)
    {
        var query = db.ElectricityBills.Include(x => x.Contract).AsQueryable();
        if (contractId.HasValue) query = query.Where(x => x.ContractId == contractId.Value);

        return await query.OrderByDescending(x => x.Id)
            .Select(x => new ElectricityBillListItemResponse(
                x.Id,
                x.ContractId,
                x.Contract != null ? x.Contract.ContractNo : null,
                x.BillingStartUtc,
                x.BillingEndUtc,
                x.TotalAmount,
                x.TotalUnits,
                x.UnitPrice,
                x.PayableTotalAmount,
                x.ChargesCreated))
            .ToListAsync();
    }

    public async Task<ElectricityBillDetailResponse> GetBillDetailAsync(int billId)
    {
        var bill = await db.ElectricityBills
            .Include(x => x.Contract)
            .FirstOrDefaultAsync(x => x.Id == billId)
            ?? throw new DomainNotFoundException();

        var allocations = await db.ElectricityAllocations
            .Include(x => x.Contract)
            .Include(x => x.PropertyRoom)
            .Include(x => x.Tenant)
            .Where(x => x.ElectricityBillId == billId)
            .OrderBy(x => x.Id)
            .Select(x => new ElectricityBillAllocationDetail(
                x.Id,
                (int)x.TargetType,
                x.ContractId,
                x.Contract != null ? x.Contract.ContractNo : null,
                x.PropertyRoomId,
                x.PropertyRoom != null ? x.PropertyRoom.Name : null,
                x.TenantId,
                x.Tenant != null ? x.Tenant.Name : null,
                x.OccupancyStartUtc,
                x.OccupancyEndUtc,
                x.MeterStart,
                x.MeterEnd,
                x.TenantUnits,
                x.OccupantCount,
                x.OccupancyDays,
                x.PrivateAmount,
                x.PublicAmount,
                x.PayableAmount))
            .ToListAsync();

        return new ElectricityBillDetailResponse(
            bill.Id,
            bill.ContractId,
            bill.Contract?.ContractNo,
            bill.BillingStartUtc,
            bill.BillingEndUtc,
            bill.TotalAmount,
            bill.TotalUnits,
            bill.UnitPrice,
            bill.PrivateTotalAmount,
            bill.PublicTotalAmount,
            bill.PayableTotalAmount,
            allocations);
    }

    public async Task<ElectricityCreateChargesResponse> CreateChargesFromBillAsync(int billId, ElectricityCreateChargesRequest? request, string? mode)
    {
        var bill = await db.ElectricityBills.FirstOrDefaultAsync(x => x.Id == billId)
            ?? throw new DomainNotFoundException("帳單不存在");
        if (bill.ChargesCreated) throw new DomainValidationException("此帳單已轉入應收，不能重複轉入");

        var expenseBillIds = (request?.ExpenseBillIds ?? [])
            .Distinct()
            .Where(x => x > 0)
            .ToList();
        var expenseBills = expenseBillIds.Count == 0
            ? []
            : await db.ExpenseRecords
                .Where(x => expenseBillIds.Contains(x.Id) && x.Category == ExpenseCategory.Electricity)
                .OrderBy(x => x.OccurredAtUtc)
                .ThenBy(x => x.Id)
                .ToListAsync();
        var occurredAtUtc = expenseBills.Count > 0
            ? expenseBills[0].OccurredAtUtc
            : bill.BillingStartUtc;

        var allocations = await db.ElectricityAllocations
            .Where(x => x.ElectricityBillId == billId)
            .ToListAsync();
        if (allocations.Count == 0) throw new DomainValidationException("無分攤資料");
        var tenantAllocations = allocations.Where(x => x.TargetType == ElectricityAllocationTargetType.Tenant && x.ContractId.HasValue).ToList();

        var created = 0;
        var useMerged = string.Equals(mode, "merged", StringComparison.OrdinalIgnoreCase);
        if (useMerged)
        {
            var distinctContractIds = tenantAllocations.Select(x => x.ContractId!.Value).Distinct().ToList();
            if (distinctContractIds.Count != 1) throw new DomainValidationException("跨多份合約的帳單不可使用合併轉入");
            // 只計租客分攤，排除房東自付（landlord）分攤，與分攤轉入口徑一致
            var charge = new ChargeRecord
            {
                ContractId = distinctContractIds[0],
                Category = ChargeCategory.Electricity,
                OccurredAtUtc = occurredAtUtc,
                BillingStartUtc = bill.BillingStartUtc,
                BillingEndUtc = bill.BillingEndUtc,
                UsageUnits = tenantAllocations.Sum(x => x.TenantUnits),
                Amount = ElectricityCalculationService.TruncateToInteger(tenantAllocations.Sum(x => x.PayableAmount)),
                Notes = $"電費帳單#{billId} 合併轉入",
                IsPaid = false,
                CreatedAtUtc = DateTime.UtcNow
            };
            db.ChargeRecords.Add(charge);
            created++;
        }
        else
        {
            foreach (var group in tenantAllocations.GroupBy(x => x.ContractId!.Value))
            {
                var charge = new ChargeRecord
                {
                    ContractId = group.Key,
                    Category = ChargeCategory.Electricity,
                    OccurredAtUtc = occurredAtUtc,
                    BillingStartUtc = bill.BillingStartUtc,
                    BillingEndUtc = bill.BillingEndUtc,
                    UsageUnits = group.Sum(x => x.TenantUnits),
                    Amount = ElectricityCalculationService.TruncateToInteger(group.Sum(x => x.PayableAmount)),
                    Notes = $"電費帳單#{billId} 分攤轉入",
                    IsPaid = false,
                    CreatedAtUtc = DateTime.UtcNow
                };
                db.ChargeRecords.Add(charge);
                created++;
            }
        }

        await db.SaveChangesAsync();

        if (expenseBillIds.Count > 0)
        {
            foreach (var expenseBill in expenseBills)
            {
                expenseBill.SplitStatus = ExpenseSplitStatus.Split;
            }
        }

        bill.ChargesCreated = true;
        bill.ChargesCreatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return new ElectricityCreateChargesResponse(created);
    }

    private static bool HasOverlap(DateTime startA, DateTime endA, DateTime startB, DateTime endB)
        => startA.Date <= endB.Date && startB.Date <= endA.Date;

    private async Task<Dictionary<int, List<PropertyRoom>>> ResolveExpenseBillRoomsAsync(List<ExpenseRecord> expenseBills)
    {
        var explicitRooms = expenseBills
            .Where(x => x.PropertyRoomId.HasValue && x.PropertyRoom is not null)
            .ToDictionary(x => x.Id, x => new List<PropertyRoom> { x.PropertyRoom! });

        var unitIds = expenseBills
            .Where(x => !x.PropertyRoomId.HasValue)
            .Select(x => x.PropertyUnitId)
            .Distinct()
            .ToList();

        var roomsByUnit = unitIds.Count == 0
            ? new Dictionary<int, List<PropertyRoom>>()
            : await db.PropertyRooms
                .Where(x => unitIds.Contains(x.PropertyUnitId))
                .OrderBy(x => x.PropertyUnitId)
                .ThenBy(x => x.Name)
                .ThenBy(x => x.Id)
                .GroupBy(x => x.PropertyUnitId)
                .ToDictionaryAsync(g => g.Key, g => g.ToList());

        foreach (var bill in expenseBills.Where(x => !explicitRooms.ContainsKey(x.Id)))
        {
            explicitRooms[bill.Id] = roomsByUnit.TryGetValue(bill.PropertyUnitId, out var rooms)
                ? rooms
                : [];
        }

        return explicitRooms;
    }

    /// <summary>
    /// 取出落在帳期內的抄表讀數序列，並補上帳期起訖的邊界讀數（起：帳期開始當日或之前最後一筆；訖：帳期結束當日或之後第一筆）。
    /// 收尾讀數落在帳期迄日「當天」即視為有效邊界，對應「每期於週期兩端各抄一次、抄表日對齊帳期起訖」的常見用法。
    /// </summary>
    public static List<ElectricityMeterReading> BuildBillReadingSeries(List<ElectricityMeterReading> roomReadings, DateTime billingStartUtc, DateTime billingEndUtc)
    {
        var ordered = roomReadings
            .OrderBy(x => x.ReadingDateUtc)
            .ThenBy(x => x.Id)
            .ToList();

        var startBoundary = ordered
            .LastOrDefault(x => x.ReadingDateUtc.Date <= billingStartUtc.Date);
        var endBoundary = ordered
            .FirstOrDefault(x => x.ReadingDateUtc.Date >= billingEndUtc.Date);

        if (startBoundary is null || endBoundary is null)
        {
            return [];
        }

        return ordered
            .Where(x =>
                x.Id == startBoundary.Id ||
                x.Id == endBoundary.Id ||
                (x.ReadingDateUtc.Date > billingStartUtc.Date && x.ReadingDateUtc.Date <= billingEndUtc.Date))
            .DistinctBy(x => x.Id)
            .OrderBy(x => x.ReadingDateUtc)
            .ThenBy(x => x.Id)
            .ToList();
    }

    private static DateTime MaxDate(DateTime left, DateTime right)
        => left.Date >= right.Date ? left.Date : right.Date;

    private static DateTime MinDate(DateTime left, DateTime right)
        => left.Date <= right.Date ? left.Date : right.Date;
}
