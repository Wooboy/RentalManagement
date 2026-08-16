using Microsoft.EntityFrameworkCore;
using RentalManager.Api.Common;
using RentalManager.Api.Data;
using RentalManager.Api.Dtos;
using RentalManager.Api.Models;

namespace RentalManager.Api.Services;

public class ContractService(AppDbContext db, AttachmentService attachments)
{
    public async Task<List<ContractResponse>> GetAllAsync(string? keyword, int? propertyUnitId, int? propertyRoomId, int? status)
    {
        var query = db.Contracts.Include(x => x.Tenant).Include(x => x.PropertyUnit).AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(x => x.ContractNo.Contains(keyword) || x.ContractName.Contains(keyword) || x.PropertyName.Contains(keyword));
        if (status.HasValue) query = query.Where(x => (int)x.Status == status.Value);

        if (propertyUnitId.HasValue || propertyRoomId.HasValue)
        {
            var filteredContractIds = db.ContractRooms
                .Include(x => x.PropertyRoom)
                .Where(x =>
                    (!propertyUnitId.HasValue || x.PropertyRoom!.PropertyUnitId == propertyUnitId.Value) &&
                    (!propertyRoomId.HasValue || x.PropertyRoomId == propertyRoomId.Value))
                .Select(x => x.ContractId)
                .Distinct();

            query = query.Where(x => filteredContractIds.Contains(x.Id));
        }

        var contracts = await query.OrderByDescending(x => x.Id).ToListAsync();
        var ids = contracts.Select(x => x.Id).ToList();
        var roomMap = await db.ContractRooms
            .Include(x => x.PropertyRoom)
            .ThenInclude(x => x!.PropertyUnit)
            .Where(x => ids.Contains(x.ContractId))
            .GroupBy(x => x.ContractId)
            .ToDictionaryAsync(g => g.Key, g => g.Select(x => new ContractRoomInfo(
                x.PropertyRoomId,
                x.PropertyRoom!.PropertyUnitId,
                x.PropertyRoom.PropertyUnit != null ? x.PropertyRoom.PropertyUnit.Name : string.Empty,
                x.PropertyRoom.Code,
                x.PropertyRoom.Name)).ToList());

        return contracts
            .Select(c => ContractResponse.From(c, roomMap.TryGetValue(c.Id, out var rooms) ? rooms : null))
            .ToList();
    }

    public async Task<ContractResponse> CreateAsync(ContractUpsertRequest request)
    {
        var resolved = await ValidateAndResolveAsync(request);

        var contract = new Contract
        {
            ContractNo = string.IsNullOrWhiteSpace(request.ContractNo) ? $"AUTO-{DateTime.UtcNow:yyyyMMddHHmmss}" : request.ContractNo.Trim(),
            ContractName = request.ContractName.Trim(),
            TenantId = request.TenantId,
            PropertyUnitId = resolved.PropertyUnitId,
            PropertyName = resolved.PropertyDisplay,
            PropertyAddress = resolved.PropertyAddress,
            StartDateUtc = request.StartDateUtc,
            EndDateUtc = request.EndDateUtc,
            MonthlyRent = request.MonthlyRent,
            PaymentIntervalMonths = request.PaymentIntervalMonths,
            PeriodPayableAmount = request.MonthlyRent * request.PaymentIntervalMonths,
            Deposit = request.Deposit,
            OccupantCount = request.OccupantCount,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            ElectricityRuleType = (ElectricityRuleType)request.ElectricityRuleType,
            Status = (ContractStatus)request.Status,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        db.Contracts.Add(contract);
        await db.SaveChangesAsync();

        db.ContractRooms.AddRange(request.PropertyRoomIds.Distinct().Select(rid => new ContractRoom { ContractId = contract.Id, PropertyRoomId = rid }));
        await db.SaveChangesAsync();
        return ContractResponse.From(contract);
    }

    public async Task<ContractResponse> UpdateAsync(int id, ContractUpsertRequest request)
    {
        var item = await db.Contracts.FindAsync(id) ?? throw new DomainNotFoundException();
        var resolved = await ValidateAndResolveAsync(request);

        if (!string.IsNullOrWhiteSpace(request.ContractNo))
        {
            item.ContractNo = request.ContractNo.Trim();
        }
        item.ContractName = request.ContractName.Trim();
        item.TenantId = request.TenantId;
        item.PropertyUnitId = resolved.PropertyUnitId;
        item.PropertyName = resolved.PropertyDisplay;
        item.PropertyAddress = resolved.PropertyAddress;
        item.StartDateUtc = request.StartDateUtc;
        item.EndDateUtc = request.EndDateUtc;
        item.MonthlyRent = request.MonthlyRent;
        item.PaymentIntervalMonths = request.PaymentIntervalMonths;
        item.PeriodPayableAmount = request.MonthlyRent * request.PaymentIntervalMonths;
        item.Deposit = request.Deposit;
        item.OccupantCount = request.OccupantCount;
        item.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        item.ElectricityRuleType = (ElectricityRuleType)request.ElectricityRuleType;
        item.Status = (ContractStatus)request.Status;
        item.UpdatedAtUtc = DateTime.UtcNow;

        var old = db.ContractRooms.Where(x => x.ContractId == id);
        db.ContractRooms.RemoveRange(old);
        db.ContractRooms.AddRange(request.PropertyRoomIds.Distinct().Select(rid => new ContractRoom { ContractId = id, PropertyRoomId = rid }));

        await db.SaveChangesAsync();
        return ContractResponse.From(item);
    }

    public async Task DeleteAsync(int id)
    {
        var item = await db.Contracts.FindAsync(id) ?? throw new DomainNotFoundException();

        // 合約刪除會 cascade 掉其應收與報修單，但這些實體的多型附件（無 FK cascade）
        // 不會被自動清除，需先連同實體檔一併移除，避免孤兒檔案。
        var chargeIds = await db.ChargeRecords.Where(x => x.ContractId == id).Select(x => x.Id).ToListAsync();
        var repairTicketIds = await db.RepairTickets.Where(x => x.ContractId == id).Select(x => x.Id).ToListAsync();
        await attachments.RemoveForEntitiesAsync(AttachmentEntityType.Contract, [id]);
        await attachments.RemoveForEntitiesAsync(AttachmentEntityType.ChargeRecord, chargeIds);
        await attachments.RemoveForEntitiesAsync(AttachmentEntityType.RepairTicket, repairTicketIds);

        db.Contracts.Remove(item);
        await db.SaveChangesAsync();
    }

    public async Task<ContractBatchChargeCreateResponse> BatchCreatePeriodChargesAsync(ContractBatchChargeCreateRequest request)
    {
        var contractIds = (request.ContractIds ?? []).Distinct().Where(x => x > 0).ToList();
        if (contractIds.Count == 0) throw new DomainValidationException("請至少選擇一份合約");

        var targetMonth = (request.TargetMonthUtc ?? request.ReferenceDateUtc ?? DateTime.UtcNow).Date;
        var contracts = await db.Contracts
            .Where(x => contractIds.Contains(x.Id))
            .OrderBy(x => x.Id)
            .ToListAsync();
        if (contracts.Count != contractIds.Count) throw new DomainValidationException("部分合約不存在");

        var existingCharges = await db.ChargeRecords
            .Where(x => contractIds.Contains(x.ContractId) && x.Category == ChargeCategory.Rent)
            .Select(x => new { x.ContractId, x.BillingStartUtc, x.BillingEndUtc })
            .ToListAsync();

        var created = new List<ContractBatchChargeCreatedItem>();
        var skipped = new List<ContractBatchChargeSkippedItem>();

        foreach (var contract in contracts)
        {
            var period = BillingPeriodCalculator.ResolveBillingPeriodForMonth(contract, targetMonth);

            var duplicated = existingCharges.Any(x =>
                x.ContractId == contract.Id &&
                x.BillingStartUtc.Date == period.Start.Date &&
                x.BillingEndUtc.Date == period.End.Date);
            if (duplicated)
            {
                skipped.Add(new ContractBatchChargeSkippedItem(contract.Id, contract.ContractNo, "本期租金應收已存在"));
                continue;
            }

            var charge = new ChargeRecord
            {
                ContractId = contract.Id,
                Category = ChargeCategory.Rent,
                OccurredAtUtc = period.Start,
                BillingStartUtc = period.Start,
                BillingEndUtc = period.End,
                Amount = contract.PeriodPayableAmount,
                Notes = $"系統批次建立本期租金（{period.Start:yyyy-MM-dd} ~ {period.End:yyyy-MM-dd}）",
                IsPaid = false,
                CreatedAtUtc = DateTime.UtcNow
            };

            db.ChargeRecords.Add(charge);
            created.Add(new ContractBatchChargeCreatedItem(contract.Id, contract.ContractNo, charge.BillingStartUtc, charge.BillingEndUtc, charge.Amount));
        }

        if (created.Count > 0)
        {
            await db.SaveChangesAsync();
        }

        return new ContractBatchChargeCreateResponse(
            new DateTime(targetMonth.Year, targetMonth.Month, 1),
            created.Count,
            skipped.Count,
            created,
            skipped);
    }

    private async Task<(int? PropertyUnitId, string PropertyDisplay, string PropertyAddress)> ValidateAndResolveAsync(ContractUpsertRequest request)
    {
        if (request.TenantId <= 0) throw new DomainValidationException("請選擇租客");
        if (string.IsNullOrWhiteSpace(request.ContractName)) throw new DomainValidationException("請輸入合約名稱");
        if (request.PaymentIntervalMonths != 1 && request.PaymentIntervalMonths != 3 && request.PaymentIntervalMonths != 12) throw new DomainValidationException("付款間隔只允許每月、每季、每年");
        if (request.PropertyRoomIds is null || request.PropertyRoomIds.Count == 0) throw new DomainValidationException("請至少選擇一間房間");
        if (!await db.Tenants.AnyAsync(x => x.Id == request.TenantId)) throw new DomainValidationException("租客不存在");

        var roomIds = request.PropertyRoomIds.Distinct().ToList();
        var rooms = await db.PropertyRooms.Include(x => x.PropertyUnit).Where(x => roomIds.Contains(x.Id)).ToListAsync();
        if (rooms.Count != roomIds.Count || rooms.Any(x => x.PropertyUnit is null)) throw new DomainValidationException("房間資料不存在");

        var propertyGroups = rooms
            .GroupBy(x => x.PropertyUnitId)
            .Select(g => new
            {
                PropertyUnitId = g.Key,
                Property = g.First().PropertyUnit!,
                Rooms = g.OrderBy(x => x.Name).ThenBy(x => x.Id).ToList()
            })
            .OrderBy(x => x.Property.Name)
            .ThenBy(x => x.Property.Id)
            .ToList();

        var propertyDisplay = string.Join("；", propertyGroups.Select(g =>
            $"{g.Property.Name}-" + string.Join(",", g.Rooms.Select(r => string.IsNullOrWhiteSpace(r.Name) ? r.Code : r.Name))));
        var propertyAddress = string.Join("；", propertyGroups.Select(g => g.Property.Address).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct());
        int? propertyUnitId = propertyGroups.Count == 1 ? propertyGroups[0].PropertyUnitId : null;

        return (propertyUnitId, propertyDisplay, propertyAddress);
    }
}
