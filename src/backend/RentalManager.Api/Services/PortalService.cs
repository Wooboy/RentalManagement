using Microsoft.EntityFrameworkCore;
using RentalManager.Api.Data;
using RentalManager.Api.Dtos;
using RentalManager.Api.Models;

namespace RentalManager.Api.Services;

/// <summary>房客入口的自助查詢，所有查詢都以 tenantId 過濾。</summary>
public class PortalService(AppDbContext db)
{
    public async Task<List<ContractResponse>> GetMyContractsAsync(int tenantId)
    {
        var contracts = await db.Contracts
            .Include(x => x.Tenant)
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.Id)
            .ToListAsync();

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

    public async Task<List<ChargeListItemResponse>> GetMyChargesAsync(int tenantId, int? contractId, bool? isPaid)
    {
        var query = db.ChargeRecords
            .Include(x => x.Contract)
            .Where(x => x.Contract != null && x.Contract.TenantId == tenantId);
        if (contractId.HasValue) query = query.Where(x => x.ContractId == contractId.Value);
        if (isPaid.HasValue) query = query.Where(x => x.IsPaid == isPaid.Value);

        return await query
            .OrderByDescending(x => x.OccurredAtUtc)
            .ThenByDescending(x => x.Id)
            .Select(x => new ChargeListItemResponse(
                x.Id,
                x.ContractId,
                x.Contract != null ? x.Contract.ContractName : null,
                x.Contract != null ? x.Contract.ContractNo : null,
                x.Category,
                x.OccurredAtUtc,
                x.BillingStartUtc,
                x.BillingEndUtc,
                x.MeterStart,
                x.MeterEnd,
                x.UsageUnits,
                x.Amount,
                x.Notes,
                x.IsPaid,
                x.PaidAtUtc,
                x.CreatedAtUtc))
            .ToListAsync();
    }

    /// <summary>租客是否可存取指定附件目標（自己的合約、應收或報修單）。</summary>
    public async Task<bool> CanAccessAttachmentTargetAsync(int tenantId, AttachmentEntityType entityType, int entityId)
    {
        return entityType switch
        {
            AttachmentEntityType.Contract =>
                await db.Contracts.AnyAsync(x => x.Id == entityId && x.TenantId == tenantId),
            AttachmentEntityType.ChargeRecord =>
                await db.ChargeRecords.Include(x => x.Contract)
                    .AnyAsync(x => x.Id == entityId && x.Contract != null && x.Contract.TenantId == tenantId),
            AttachmentEntityType.RepairTicket =>
                await db.RepairTickets.Include(x => x.Contract)
                    .AnyAsync(x => x.Id == entityId && x.Contract != null && x.Contract.TenantId == tenantId),
            _ => false
        };
    }
}
