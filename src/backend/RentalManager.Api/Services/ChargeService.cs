using Microsoft.EntityFrameworkCore;
using RentalManager.Api.Common;
using RentalManager.Api.Data;
using RentalManager.Api.Dtos;
using RentalManager.Api.Models;

namespace RentalManager.Api.Services;

public class ChargeService(AppDbContext db, AttachmentService attachments)
{
    public async Task<List<ChargeListItemResponse>> GetAllAsync(DateTime? startDateUtc, DateTime? endDateUtc, bool? isPaid, ChargeCategory? category)
    {
        var query = db.ChargeRecords.Include(x => x.Contract).AsQueryable();
        if (startDateUtc.HasValue) query = query.Where(x => x.OccurredAtUtc >= startDateUtc.Value);
        if (endDateUtc.HasValue) query = query.Where(x => x.OccurredAtUtc <= endDateUtc.Value);
        if (isPaid.HasValue) query = query.Where(x => x.IsPaid == isPaid.Value);
        if (category.HasValue) query = query.Where(x => x.Category == category.Value);

        return await query
            .OrderBy(x => x.ContractId)
            .ThenBy(x => x.OccurredAtUtc)
            .ThenBy(x => x.Id)
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

    public async Task<ChargeResponse> GetByIdAsync(int id)
    {
        var item = await db.ChargeRecords.FindAsync(id) ?? throw new DomainNotFoundException();
        return ChargeResponse.From(item);
    }

    public async Task<ChargeResponse> CreateAsync(ChargeUpsertRequest request)
    {
        var occurredAtUtc = NormalizeOccurredAtUtc(request);
        await ValidateAsync(request);

        var item = new ChargeRecord
        {
            ContractId = request.ContractId,
            Category = request.Category,
            OccurredAtUtc = occurredAtUtc,
            BillingStartUtc = request.BillingStartUtc,
            BillingEndUtc = request.BillingEndUtc,
            MeterStart = request.MeterStart,
            MeterEnd = request.MeterEnd,
            UsageUnits = request.MeterStart.HasValue && request.MeterEnd.HasValue
                ? request.MeterEnd.Value - request.MeterStart.Value
                : request.UsageUnits,
            Amount = request.Amount,
            Notes = request.Notes,
            IsPaid = request.IsPaid,
            PaidAtUtc = request.PaidAtUtc,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.ChargeRecords.Add(item);
        await db.SaveChangesAsync();
        return ChargeResponse.From(item);
    }

    public async Task<ChargeResponse> UpdateAsync(int id, ChargeUpsertRequest request)
    {
        var item = await db.ChargeRecords.FindAsync(id) ?? throw new DomainNotFoundException();
        var occurredAtUtc = NormalizeOccurredAtUtc(request);
        await ValidateAsync(request);

        item.ContractId = request.ContractId;
        item.Category = request.Category;
        item.OccurredAtUtc = occurredAtUtc;
        item.BillingStartUtc = request.BillingStartUtc;
        item.BillingEndUtc = request.BillingEndUtc;
        item.MeterStart = request.MeterStart;
        item.MeterEnd = request.MeterEnd;
        item.UsageUnits = request.MeterStart.HasValue && request.MeterEnd.HasValue
            ? request.MeterEnd.Value - request.MeterStart.Value
            : request.UsageUnits;
        item.Amount = request.Amount;
        item.Notes = request.Notes;
        item.IsPaid = request.IsPaid;
        item.PaidAtUtc = request.PaidAtUtc;

        await db.SaveChangesAsync();
        return ChargeResponse.From(item);
    }

    public async Task DeleteAsync(int id)
    {
        var item = await db.ChargeRecords.FindAsync(id) ?? throw new DomainNotFoundException();
        await attachments.RemoveForEntitiesAsync(AttachmentEntityType.ChargeRecord, [id]);
        db.ChargeRecords.Remove(item);
        await db.SaveChangesAsync();
    }

    private async Task ValidateAsync(ChargeUpsertRequest request)
    {
        if (request.ContractId <= 0) throw new DomainValidationException("請選擇合約");
        if (!await db.Contracts.AnyAsync(x => x.Id == request.ContractId)) throw new DomainValidationException("合約不存在");
        if (request.Amount < 0) throw new DomainValidationException("金額不可小於 0");
        if (request.BillingEndUtc < request.BillingStartUtc) throw new DomainValidationException("帳期結束日不可早於開始日");
        if ((request.Category == ChargeCategory.Water || request.Category == ChargeCategory.Electricity) &&
            request.MeterStart.HasValue && request.MeterEnd.HasValue &&
            request.MeterEnd.Value < request.MeterStart.Value)
        {
            throw new DomainValidationException("錶末讀數不可小於錶初讀數");
        }
    }

    private static DateTime NormalizeOccurredAtUtc(ChargeUpsertRequest request)
        => request.OccurredAtUtc is null || request.OccurredAtUtc == default(DateTime)
            ? request.BillingStartUtc
            : request.OccurredAtUtc.Value;
}
