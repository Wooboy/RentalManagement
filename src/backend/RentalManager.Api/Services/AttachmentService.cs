using Microsoft.EntityFrameworkCore;
using RentalManager.Api.Common;
using RentalManager.Api.Data;
using RentalManager.Api.Dtos;
using RentalManager.Api.Models;

namespace RentalManager.Api.Services;

public class AttachmentService(AppDbContext db, IFileStorage storage, IConfiguration configuration)
{
    private static readonly Dictionary<string, string[]> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = ["image/jpeg"],
        [".jpeg"] = ["image/jpeg"],
        [".png"] = ["image/png"],
        [".webp"] = ["image/webp"],
        [".heic"] = ["image/heic", "image/heif"],
        [".pdf"] = ["application/pdf"]
    };

    public async Task<List<AttachmentResponse>> GetListAsync(AttachmentEntityType entityType, int entityId)
    {
        return await db.Attachments
            .Include(x => x.UploadedBy)
            .Where(x => x.EntityType == entityType && x.EntityId == entityId)
            .OrderByDescending(x => x.Id)
            .Select(x => new AttachmentResponse(
                x.Id, x.EntityType, x.EntityId, x.FileName, x.ContentType, x.FileSize,
                x.UploadedBy != null ? (x.UploadedBy.DisplayName ?? x.UploadedBy.Username) : null,
                x.CreatedAtUtc))
            .ToListAsync();
    }

    public async Task<AttachmentResponse> UploadAsync(AttachmentEntityType entityType, int entityId, IFormFile file, int? uploadedByUserId)
    {
        if (file is null || file.Length == 0) throw new DomainValidationException("請選擇要上傳的檔案");

        var maxSizeMb = configuration.GetValue<int?>("Storage:MaxFileSizeMB") ?? 10;
        if (file.Length > maxSizeMb * 1024L * 1024L) throw new DomainValidationException($"檔案大小不可超過 {maxSizeMb}MB");

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedTypes.ContainsKey(extension))
        {
            throw new DomainValidationException($"不支援的檔案類型，允許：{string.Join("、", AllowedTypes.Keys)}");
        }

        await EnsureEntityExistsAsync(entityType, entityId);

        var storagePath = $"{entityType}/{entityId}/{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        await using (var stream = file.OpenReadStream())
        {
            await storage.SaveAsync(stream, storagePath);
        }

        var item = new Attachment
        {
            EntityType = entityType,
            EntityId = entityId,
            FileName = file.FileName,
            ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
            FileSize = file.Length,
            StoragePath = storagePath,
            UploadedByUserId = uploadedByUserId,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Attachments.Add(item);
        await db.SaveChangesAsync();

        return new AttachmentResponse(
            item.Id, item.EntityType, item.EntityId, item.FileName, item.ContentType, item.FileSize,
            null, item.CreatedAtUtc);
    }

    public async Task<(Stream Content, string ContentType, string FileName)> DownloadAsync(int id)
    {
        var item = await FindAsync(id);
        if (!storage.Exists(item.StoragePath)) throw new DomainNotFoundException("附件檔案不存在");
        return (storage.OpenRead(item.StoragePath), item.ContentType, item.FileName);
    }

    public async Task DeleteAsync(int id)
    {
        var item = await FindAsync(id);
        storage.Delete(item.StoragePath);
        db.Attachments.Remove(item);
        await db.SaveChangesAsync();
    }

    public async Task<Attachment> FindAsync(int id)
        => await db.Attachments.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new DomainNotFoundException("附件不存在");

    private async Task EnsureEntityExistsAsync(AttachmentEntityType entityType, int entityId)
    {
        var exists = entityType switch
        {
            AttachmentEntityType.Contract => await db.Contracts.AnyAsync(x => x.Id == entityId),
            AttachmentEntityType.ChargeRecord => await db.ChargeRecords.AnyAsync(x => x.Id == entityId),
            AttachmentEntityType.ExpenseRecord => await db.ExpenseRecords.AnyAsync(x => x.Id == entityId),
            _ => false
        };
        if (!exists) throw new DomainValidationException("附件關聯的資料不存在");
    }
}
