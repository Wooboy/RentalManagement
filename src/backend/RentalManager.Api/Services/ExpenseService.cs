using Microsoft.EntityFrameworkCore;
using RentalManager.Api.Common;
using RentalManager.Api.Data;
using RentalManager.Api.Dtos;
using RentalManager.Api.Models;

namespace RentalManager.Api.Services;

public class ExpenseService(AppDbContext db)
{
    public async Task<List<ExpenseListItemResponse>> GetAllAsync(DateTime? startDateUtc, DateTime? endDateUtc, ExpenseCategory? category, ExpenseSplitStatus? splitStatus)
    {
        var query = db.ExpenseRecords.Include(x => x.PropertyUnit).Include(x => x.PropertyRoom).AsQueryable();
        if (startDateUtc.HasValue) query = query.Where(x => x.BillingStartUtc >= startDateUtc.Value);
        if (endDateUtc.HasValue) query = query.Where(x => x.BillingStartUtc <= endDateUtc.Value);
        if (category.HasValue) query = query.Where(x => x.Category == category.Value);
        if (splitStatus.HasValue) query = query.Where(x => x.SplitStatus == splitStatus.Value);

        return await query.OrderByDescending(x => x.Id)
            .Select(x => new ExpenseListItemResponse(
                x.Id,
                x.PropertyUnitId,
                x.PropertyUnit != null ? x.PropertyUnit.Name : null,
                x.PropertyRoomId,
                x.PropertyRoom != null ? x.PropertyRoom.Name : null,
                x.Category,
                x.BillingStartUtc,
                x.BillingEndUtc,
                x.Amount,
                x.UsageUnits,
                x.SplitStatus,
                x.Notes,
                x.OccurredAtUtc,
                x.CreatedAtUtc))
            .ToListAsync();
    }

    public async Task<ExpenseResponse> GetByIdAsync(int id)
    {
        var item = await db.ExpenseRecords.FindAsync(id) ?? throw new DomainNotFoundException();
        return ExpenseResponse.From(item);
    }

    public async Task<ExpenseResponse> CreateAsync(ExpenseUpsertRequest request)
    {
        await ValidateAsync(request);

        var item = new ExpenseRecord
        {
            PropertyUnitId = request.PropertyUnitId,
            PropertyRoomId = request.PropertyRoomId,
            Category = request.Category,
            BillingStartUtc = request.BillingStartUtc,
            BillingEndUtc = request.BillingEndUtc,
            Amount = request.Amount,
            UsageUnits = request.UsageUnits,
            SplitStatus = request.SplitStatus,
            Notes = request.Notes,
            OccurredAtUtc = NormalizeOccurredAtUtc(request),
            CreatedAtUtc = DateTime.UtcNow
        };
        db.ExpenseRecords.Add(item);
        await db.SaveChangesAsync();
        return ExpenseResponse.From(item);
    }

    public async Task<ExpenseResponse> UpdateAsync(int id, ExpenseUpsertRequest request)
    {
        var item = await db.ExpenseRecords.FindAsync(id) ?? throw new DomainNotFoundException();
        await ValidateAsync(request);

        item.PropertyUnitId = request.PropertyUnitId;
        item.PropertyRoomId = request.PropertyRoomId;
        item.Category = request.Category;
        item.BillingStartUtc = request.BillingStartUtc;
        item.BillingEndUtc = request.BillingEndUtc;
        item.Amount = request.Amount;
        item.UsageUnits = request.UsageUnits;
        item.SplitStatus = request.SplitStatus;
        item.Notes = request.Notes;
        item.OccurredAtUtc = NormalizeOccurredAtUtc(request);

        await db.SaveChangesAsync();
        return ExpenseResponse.From(item);
    }

    public async Task DeleteAsync(int id)
    {
        var item = await db.ExpenseRecords.FindAsync(id) ?? throw new DomainNotFoundException();
        db.ExpenseRecords.Remove(item);
        await db.SaveChangesAsync();
    }

    private async Task ValidateAsync(ExpenseUpsertRequest request)
    {
        if (request.PropertyUnitId <= 0) throw new DomainValidationException("請選擇房源");
        if (!await db.PropertyUnits.AnyAsync(x => x.Id == request.PropertyUnitId)) throw new DomainValidationException("房源不存在");

        if (request.PropertyRoomId.HasValue)
        {
            var room = await db.PropertyRooms.FirstOrDefaultAsync(x => x.Id == request.PropertyRoomId.Value)
                ?? throw new DomainValidationException("房間不存在");
            if (room.PropertyUnitId != request.PropertyUnitId) throw new DomainValidationException("房間不屬於所選房源");
        }

        if (request.Amount < 0) throw new DomainValidationException("金額不可小於 0");
        if (request.UsageUnits.HasValue && request.UsageUnits.Value < 0) throw new DomainValidationException("度數不可小於 0");
        if (request.BillingEndUtc < request.BillingStartUtc) throw new DomainValidationException("帳期結束日不可早於開始日");
        if (!Enum.IsDefined(request.SplitStatus)) throw new DomainValidationException("支出狀態不正確");
    }

    private static DateTime NormalizeOccurredAtUtc(ExpenseUpsertRequest request)
        => request.OccurredAtUtc is null || request.OccurredAtUtc == default(DateTime)
            ? DateTime.UtcNow
            : request.OccurredAtUtc.Value;
}
