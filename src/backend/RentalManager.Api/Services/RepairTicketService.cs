using Microsoft.EntityFrameworkCore;
using RentalManager.Api.Common;
using RentalManager.Api.Data;
using RentalManager.Api.Dtos;
using RentalManager.Api.Models;

namespace RentalManager.Api.Services;

public class RepairTicketService(AppDbContext db, IFileStorage storage)
{
    public async Task<List<RepairTicketResponse>> GetAllAsync(RepairTicketStatus? status, string? keyword)
    {
        var query = QueryWithIncludes();
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(x =>
                x.Title.Contains(keyword) ||
                (x.Contract != null && x.Contract.ContractName.Contains(keyword)) ||
                (x.Contract != null && x.Contract.Tenant != null && x.Contract.Tenant.Name.Contains(keyword)));
        }

        var items = await query.OrderByDescending(x => x.Id).ToListAsync();
        return items.Select(ToResponse).ToList();
    }

    public async Task<List<RepairTicketResponse>> GetByTenantAsync(int tenantId)
    {
        var items = await QueryWithIncludes()
            .Where(x => x.Contract != null && x.Contract.TenantId == tenantId)
            .OrderByDescending(x => x.Id)
            .ToListAsync();
        return items.Select(ToResponse).ToList();
    }

    public async Task<RepairTicketDetailResponse> GetDetailAsync(int id, int? restrictToTenantId = null)
    {
        var item = await QueryWithIncludes().FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new DomainNotFoundException("報修單不存在");
        EnsureTenantOwnership(item, restrictToTenantId);

        var comments = await db.RepairTicketComments
            .Include(x => x.User)
            .Where(x => x.RepairTicketId == id)
            .OrderBy(x => x.Id)
            .Select(x => new RepairTicketCommentResponse(
                x.Id,
                x.User != null ? (x.User.DisplayName ?? x.User.Username) : null,
                x.User != null && x.User.Role == UserRoleType.Admin,
                x.Content,
                x.CreatedAtUtc))
            .ToListAsync();

        return new RepairTicketDetailResponse(ToResponse(item), comments);
    }

    public async Task<RepairTicketResponse> CreateAsync(int tenantId, int? createdByUserId, RepairTicketCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title)) throw new DomainValidationException("請輸入標題");
        if (string.IsNullOrWhiteSpace(request.Description)) throw new DomainValidationException("請描述報修內容");
        if (!Enum.IsDefined(request.Priority)) throw new DomainValidationException("優先度不正確");

        var contract = await db.Contracts.FirstOrDefaultAsync(x => x.Id == request.ContractId && x.TenantId == tenantId)
            ?? throw new DomainValidationException("找不到您的合約");

        int? propertyUnitId = contract.PropertyUnitId;
        if (request.PropertyRoomId.HasValue)
        {
            var contractRoom = await db.ContractRooms
                .Include(x => x.PropertyRoom)
                .FirstOrDefaultAsync(x => x.ContractId == contract.Id && x.PropertyRoomId == request.PropertyRoomId.Value)
                ?? throw new DomainValidationException("所選房間不在您的合約範圍內");
            propertyUnitId = contractRoom.PropertyRoom?.PropertyUnitId ?? propertyUnitId;
        }

        var item = new RepairTicket
        {
            ContractId = contract.Id,
            PropertyUnitId = propertyUnitId,
            PropertyRoomId = request.PropertyRoomId,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Status = RepairTicketStatus.Submitted,
            Priority = request.Priority,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        db.RepairTickets.Add(item);
        await db.SaveChangesAsync();

        var saved = await QueryWithIncludes().FirstAsync(x => x.Id == item.Id);
        return ToResponse(saved);
    }

    public async Task<RepairTicketResponse> UpdateStatusAsync(int id, RepairTicketStatus status, int? handledByUserId)
    {
        if (!Enum.IsDefined(status)) throw new DomainValidationException("狀態不正確");

        var item = await db.RepairTickets.FindAsync(id) ?? throw new DomainNotFoundException("報修單不存在");
        item.Status = status;
        item.HandledByUserId = handledByUserId ?? item.HandledByUserId;
        item.ResolvedAtUtc = status is RepairTicketStatus.Resolved or RepairTicketStatus.Closed
            ? item.ResolvedAtUtc ?? DateTime.UtcNow
            : null;
        item.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var saved = await QueryWithIncludes().FirstAsync(x => x.Id == id);
        return ToResponse(saved);
    }

    public async Task<RepairTicketCommentResponse> AddCommentAsync(int id, int? userId, string content, int? restrictToTenantId = null)
    {
        if (string.IsNullOrWhiteSpace(content)) throw new DomainValidationException("請輸入留言內容");

        var item = await QueryWithIncludes().FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new DomainNotFoundException("報修單不存在");
        EnsureTenantOwnership(item, restrictToTenantId);

        var comment = new RepairTicketComment
        {
            RepairTicketId = id,
            UserId = userId,
            Content = content.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };
        db.RepairTicketComments.Add(comment);

        item.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var user = userId.HasValue ? await db.Users.FindAsync(userId.Value) : null;
        return new RepairTicketCommentResponse(
            comment.Id,
            user != null ? (user.DisplayName ?? user.Username) : null,
            user?.Role == UserRoleType.Admin,
            comment.Content,
            comment.CreatedAtUtc);
    }

    public async Task DeleteAsync(int id)
    {
        var item = await db.RepairTickets.FindAsync(id) ?? throw new DomainNotFoundException("報修單不存在");

        // 附件是多型關聯（無 FK cascade），需手動連同實體檔案一併刪除
        var attachments = await db.Attachments
            .Where(x => x.EntityType == AttachmentEntityType.RepairTicket && x.EntityId == id)
            .ToListAsync();
        foreach (var attachment in attachments)
        {
            storage.Delete(attachment.StoragePath);
        }
        db.Attachments.RemoveRange(attachments);

        db.RepairTickets.Remove(item);
        await db.SaveChangesAsync();
    }

    /// <summary>租客是否擁有此報修單（供附件權限檢查使用）。</summary>
    public async Task<bool> IsOwnedByTenantAsync(int ticketId, int tenantId)
        => await db.RepairTickets
            .Include(x => x.Contract)
            .AnyAsync(x => x.Id == ticketId && x.Contract != null && x.Contract.TenantId == tenantId);

    private IQueryable<RepairTicket> QueryWithIncludes()
        => db.RepairTickets
            .Include(x => x.Contract)!.ThenInclude(x => x!.Tenant)
            .Include(x => x.PropertyUnit)
            .Include(x => x.PropertyRoom)
            .Include(x => x.CreatedBy)
            .Include(x => x.HandledBy);

    private static void EnsureTenantOwnership(RepairTicket item, int? restrictToTenantId)
    {
        if (restrictToTenantId.HasValue && item.Contract?.TenantId != restrictToTenantId.Value)
        {
            throw new DomainNotFoundException("報修單不存在");
        }
    }

    private static RepairTicketResponse ToResponse(RepairTicket x) => new(
        x.Id,
        x.ContractId,
        x.Contract?.ContractName,
        x.Contract?.Tenant?.Name,
        x.PropertyUnitId,
        x.PropertyUnit?.Name,
        x.PropertyRoomId,
        x.PropertyRoom?.Name,
        x.Title,
        x.Description,
        x.Status,
        x.Priority,
        x.CreatedBy != null ? (x.CreatedBy.DisplayName ?? x.CreatedBy.Username) : null,
        x.HandledBy != null ? (x.HandledBy.DisplayName ?? x.HandledBy.Username) : null,
        x.ResolvedAtUtc,
        x.CreatedAtUtc,
        x.UpdatedAtUtc);
}
