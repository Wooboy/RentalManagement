using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalManager.Api.Data;
using RentalManager.Api.Dtos;
using RentalManager.Api.Models;

namespace RentalManager.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/electricity")]
public class ElectricityController(AppDbContext db) : ControllerBase
{
    private sealed class PreviewAllocationRow
    {
        public required ExpenseRecord ExpenseBill { get; init; }
        public required Contract Contract { get; init; }
        public required PropertyRoom Room { get; init; }
        public decimal TenantUnits { get; set; }
        public decimal? MeterStart { get; set; }
        public decimal? MeterEnd { get; set; }
    }

    [HttpPost("calculate")]
    public ActionResult<ElectricityCalculateResponse> Calculate(ElectricityCalculateRequest request)
    {
        if (request.RuleType == 1)
        {
            var unitPrice = request.UnitPrice ?? 0;
            var units = request.TenantUnits ?? 0;
            var amount = units * unitPrice;
            return Ok(new ElectricityCalculateResponse(unitPrice, amount, 0, amount, null));
        }

        if (request.RuleType == 2)
        {
            var billAmount = request.BillAmount ?? 0;
            var totalUnits = request.TotalUnits ?? 1;
            var units = request.TenantUnits ?? 0;
            var unitPrice = totalUnits == 0 ? 0 : billAmount / totalUnits;
            var amount = units * unitPrice;
            return Ok(new ElectricityCalculateResponse(unitPrice, amount, 0, amount, null));
        }

        if (request.RuleType == 3)
        {
            var tenants = request.Tenants ?? [];
            var totalAmount = request.BillAmount ?? 0;
            var totalUnits = request.TotalUnits ?? 1;
            var unitPrice = totalUnits == 0 ? 0 : totalAmount / totalUnits;

            var privateAmounts = tenants.Select(t => t.TenantUnits * unitPrice).ToList();
            var privateTotal = privateAmounts.Sum();
            var publicTotal = totalAmount - privateTotal;
            var divisor = tenants.Sum(x => x.OccupantCount * x.OccupancyDays);
            var avgDailyPublic = divisor == 0 ? 0 : publicTotal / divisor;

            var payables = tenants
                .Select((t, i) => privateAmounts[i] + (t.OccupantCount * t.OccupancyDays * avgDailyPublic))
                .ToList();

            return Ok(new ElectricityCalculateResponse(unitPrice, privateTotal, publicTotal, payables.Sum(), payables));
        }

        return BadRequest("未知規則");
    }

    [HttpPost("preview-from-expenses")]
    public async Task<ActionResult<object>> PreviewFromExpenses(ElectricityExpensePreviewRequest request)
    {
        var expenseBillIds = (request.ExpenseBillIds ?? []).Distinct().Where(x => x > 0).ToList();
        if (expenseBillIds.Count == 0) return BadRequest("請至少選擇一張電費帳單");

        var expenseBills = await db.ExpenseRecords
            .Include(x => x.PropertyUnit)
            .Include(x => x.PropertyRoom)
            .Where(x => expenseBillIds.Contains(x.Id) && x.Category == ExpenseCategory.Electricity)
            .OrderBy(x => x.BillingStartUtc)
            .ThenBy(x => x.Id)
            .ToListAsync();
        if (expenseBills.Count != expenseBillIds.Count) return BadRequest("部分電費帳單不存在");

        var billRoomsMap = await ResolveExpenseBillRoomsAsync(expenseBills);
        var roomIds = billRoomsMap.Values.SelectMany(x => x).Select(x => x.Id).Distinct().ToList();
        if (roomIds.Count == 0) return BadRequest("找不到可分帳的房間");

        var minDate = expenseBills.Min(x => x.BillingStartUtc).Date;
        var maxDate = expenseBills.Max(x => x.BillingEndUtc).Date;

        var contracts = await db.ContractRooms
            .Include(x => x.Contract)!.ThenInclude(x => x!.Tenant)
            .Include(x => x.PropertyRoom)
            .Where(x => roomIds.Contains(x.PropertyRoomId) && x.Contract != null)
            .ToListAsync();

        var readings = await db.ElectricityMeterReadings
            .Where(x => roomIds.Contains(x.PropertyRoomId) &&
                        x.ReadingDateUtc >= minDate.AddDays(-1) &&
                        x.ReadingDateUtc <= maxDate.AddDays(1))
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
                        TenantUnits = 0,
                        MeterStart = null,
                        MeterEnd = null
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
                        warnings.Add($"帳單#{bill.Id} 房間 {billRoom.Name} 在 {startReading.ReadingDateUtc:yyyy-MM-dd} 的用電區段找不到對應合約");
                        continue;
                    }

                    var row = rows.First(x => x.ExpenseBill.Id == bill.Id && x.Contract.Id == owner.Contract.Id && x.Room.Id == roomId);
                    row.TenantUnits += units;
                    row.MeterStart ??= startReading.ReadingValue;
                    row.MeterEnd = endReading.ReadingValue;
                }
            }
        }

        var allocationRows = rows
            .Select(x =>
            {
                var occupancyStart = MaxDate(x.Contract.StartDateUtc, x.ExpenseBill.BillingStartUtc);
                var occupancyEnd = MinDate(x.Contract.EndDateUtc, x.ExpenseBill.BillingEndUtc);
                return new
                {
                    ExpenseBillId = x.ExpenseBill.Id,
                    ExpenseBillAmount = x.ExpenseBill.Amount,
                    ExpenseBillUnits = x.ExpenseBill.UsageUnits ?? 0,
                    x.ExpenseBill.PropertyUnitId,
                    PropertyUnitName = x.ExpenseBill.PropertyUnit != null ? x.ExpenseBill.PropertyUnit.Name : null,
                    PropertyRoomId = x.Room.Id,
                    PropertyRoomName = x.Room.Name,
                    ContractId = x.Contract.Id,
                    x.Contract.ContractNo,
                    TenantId = x.Contract.TenantId,
                    TenantName = x.Contract.Tenant != null ? x.Contract.Tenant.Name : null,
                    OccupantCount = x.Contract.OccupantCount,
                    OccupancyStartUtc = occupancyStart,
                    OccupancyEndUtc = occupancyEnd,
                    OccupancyDays = CalculateInclusiveDays(occupancyStart, occupancyEnd),
                    TenantUnits = x.TenantUnits,
                    x.MeterStart,
                    x.MeterEnd
                };
            })
            .OrderBy(x => x.ExpenseBillId)
            .ThenBy(x => x.PropertyRoomId)
            .ThenBy(x => x.OccupancyStartUtc)
            .ThenBy(x => x.ContractId)
            .ToList();

        return Ok(new
        {
            bills = expenseBills.Select(x => new
            {
                x.Id,
                x.PropertyUnitId,
                PropertyUnitName = x.PropertyUnit != null ? x.PropertyUnit.Name : null,
                x.PropertyRoomId,
                PropertyRoomName = x.PropertyRoom != null ? x.PropertyRoom.Name : null,
                x.BillingStartUtc,
                x.BillingEndUtc,
                x.Amount,
                UsageUnits = x.UsageUnits ?? 0
            }),
            allocations = allocationRows,
            warnings = warnings.Distinct().ToList()
        });
    }

    [HttpPost("bills")]
    public async Task<ActionResult<object>> SaveBill(ElectricityBillSaveRequest request)
    {
        if (!await db.Contracts.AnyAsync(x => x.Id == request.ContractId)) return BadRequest("合約不存在");
        if (request.BillingEndUtc < request.BillingStartUtc) return BadRequest("帳期結束不可早於開始");
        if (request.Allocations is null || request.Allocations.Count == 0) return BadRequest("無分攤資料");

        var contractIds = request.Allocations.Select(x => x.ContractId).Distinct().ToList();
        var roomIds = request.Allocations.Select(x => x.PropertyRoomId).Distinct().ToList();
        if (await db.Contracts.CountAsync(x => contractIds.Contains(x.Id)) != contractIds.Count) return BadRequest("部分分攤合約不存在");
        if (await db.PropertyRooms.CountAsync(x => roomIds.Contains(x.Id)) != roomIds.Count) return BadRequest("部分分攤房間不存在");

        var unitPrice = request.TotalUnits == 0 ? 0 : request.TotalAmount / request.TotalUnits;
        var privateTotal = request.Allocations.Sum(x => x.TenantUnits * unitPrice);
        var publicTotal = request.TotalAmount - privateTotal;
        var divisor = request.Allocations.Sum(x => x.OccupantCount * x.OccupancyDays);
        var avgPublic = divisor == 0 ? 0 : publicTotal / divisor;

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

        return Ok(new { billId = bill.Id, bill.PayableTotalAmount });
    }

    [HttpGet("bills")]
    public async Task<ActionResult<IEnumerable<object>>> GetBills([FromQuery] int? contractId)
    {
        var query = db.ElectricityBills.Include(x => x.Contract).AsQueryable();
        if (contractId.HasValue) query = query.Where(x => x.ContractId == contractId.Value);

        var rows = await query.OrderByDescending(x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.ContractId,
                ContractNo = x.Contract!.ContractNo,
                x.BillingStartUtc,
                x.BillingEndUtc,
                x.TotalAmount,
                x.TotalUnits,
                x.UnitPrice,
                x.PayableTotalAmount,
                x.ChargesCreated
            }).ToListAsync();

        return Ok(rows);
    }

    [HttpGet("bills/{billId:int}")]
    public async Task<ActionResult<object>> GetBillDetail(int billId)
    {
        var bill = await db.ElectricityBills
            .Include(x => x.Contract)
            .FirstOrDefaultAsync(x => x.Id == billId);
        if (bill is null) return NotFound();

        var allocations = await db.ElectricityAllocations
            .Include(x => x.Contract)
            .Include(x => x.PropertyRoom)
            .Include(x => x.Tenant)
            .Where(x => x.ElectricityBillId == billId)
            .OrderBy(x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.ContractId,
                ContractNo = x.Contract != null ? x.Contract.ContractNo : null,
                x.PropertyRoomId,
                PropertyRoomName = x.PropertyRoom != null ? x.PropertyRoom.Name : null,
                x.TenantId,
                TenantName = x.Tenant != null ? x.Tenant.Name : null,
                x.OccupancyStartUtc,
                x.OccupancyEndUtc,
                x.MeterStart,
                x.MeterEnd,
                x.TenantUnits,
                x.OccupantCount,
                x.OccupancyDays,
                x.PrivateAmount,
                x.PublicAmount,
                x.PayableAmount
            }).ToListAsync();

        return Ok(new
        {
            bill.Id,
            bill.ContractId,
            ContractNo = bill.Contract!.ContractNo,
            bill.BillingStartUtc,
            bill.BillingEndUtc,
            bill.TotalAmount,
            bill.TotalUnits,
            bill.UnitPrice,
            bill.PrivateTotalAmount,
            bill.PublicTotalAmount,
            bill.PayableTotalAmount,
            allocations
        });
    }

    [HttpPost("bills/{billId:int}/create-charges")]
    public async Task<ActionResult<object>> CreateChargesFromBill(int billId, [FromBody] ElectricityCreateChargesRequest? request, [FromQuery] string? mode)
    {
        var bill = await db.ElectricityBills.FirstOrDefaultAsync(x => x.Id == billId);
        if (bill is null) return NotFound("帳單不存在");
        if (bill.ChargesCreated) return BadRequest("此帳單已轉入應收，不能重複轉入");

        var allocations = await db.ElectricityAllocations
            .Where(x => x.ElectricityBillId == billId)
            .ToListAsync();
        if (allocations.Count == 0) return BadRequest("無分攤資料");

        var created = 0;
        var useMerged = string.Equals(mode, "merged", StringComparison.OrdinalIgnoreCase);
        if (useMerged)
        {
            var distinctContractIds = allocations.Select(x => x.ContractId).Distinct().ToList();
            if (distinctContractIds.Count != 1) return BadRequest("跨多份合約的帳單不可使用合併轉入");
            var charge = new ChargeRecord
            {
                ContractId = distinctContractIds[0],
                Category = ChargeCategory.Electricity,
                BillingStartUtc = bill.BillingStartUtc,
                BillingEndUtc = bill.BillingEndUtc,
                UsageUnits = bill.TotalUnits,
                Amount = TruncateToInteger(bill.PayableTotalAmount),
                Notes = $"電費帳單#{billId} 合併轉入",
                IsPaid = false,
                CreatedAtUtc = DateTime.UtcNow
            };
            db.ChargeRecords.Add(charge);
            created++;
        }
        else
        {
            foreach (var group in allocations.GroupBy(x => x.ContractId))
            {
                var charge = new ChargeRecord
                {
                    ContractId = group.Key,
                    Category = ChargeCategory.Electricity,
                    BillingStartUtc = bill.BillingStartUtc,
                    BillingEndUtc = bill.BillingEndUtc,
                    UsageUnits = group.Sum(x => x.TenantUnits),
                    Amount = TruncateToInteger(group.Sum(x => x.PayableAmount)),
                    Notes = $"電費帳單#{billId} 分攤轉入",
                    IsPaid = false,
                    CreatedAtUtc = DateTime.UtcNow
                };
                db.ChargeRecords.Add(charge);
                created++;
            }
        }

        await db.SaveChangesAsync();

        var expenseBillIds = (request?.ExpenseBillIds ?? [])
            .Distinct()
            .Where(x => x > 0)
            .ToList();
        if (expenseBillIds.Count > 0)
        {
            var expenseBills = await db.ExpenseRecords
                .Where(x => expenseBillIds.Contains(x.Id) && x.Category == ExpenseCategory.Electricity)
                .ToListAsync();
            foreach (var expenseBill in expenseBills)
            {
                expenseBill.SplitStatus = ExpenseSplitStatus.Split;
            }
        }

        bill.ChargesCreated = true;
        bill.ChargesCreatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(new { createdCount = created });
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

    private static List<ElectricityMeterReading> BuildBillReadingSeries(List<ElectricityMeterReading> roomReadings, DateTime billingStartUtc, DateTime billingEndUtc)
    {
        var ordered = roomReadings
            .OrderBy(x => x.ReadingDateUtc)
            .ThenBy(x => x.Id)
            .ToList();

        var startBoundary = ordered
            .LastOrDefault(x => x.ReadingDateUtc.Date <= billingStartUtc.Date);
        var endBoundary = ordered
            .FirstOrDefault(x => x.ReadingDateUtc.Date > billingEndUtc.Date);

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

    private static int CalculateInclusiveDays(DateTime start, DateTime end)
        => end.Date < start.Date ? 0 : (end.Date - start.Date).Days + 1;

    private static decimal TruncateToInteger(decimal amount)
        => Math.Floor(amount);
}
