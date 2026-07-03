using RentalManager.Api.Models;

namespace RentalManager.Api.Dtos;

public record AttachmentResponse(
    int Id,
    AttachmentEntityType EntityType,
    int EntityId,
    string FileName,
    string ContentType,
    long FileSize,
    string? UploadedByName,
    DateTime CreatedAtUtc
);
