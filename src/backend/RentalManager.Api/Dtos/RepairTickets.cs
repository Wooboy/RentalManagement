using RentalManager.Api.Models;

namespace RentalManager.Api.Dtos;

public record RepairTicketCreateRequest(
    int ContractId,
    int? PropertyRoomId,
    string Title,
    string Description,
    RepairTicketPriority Priority = RepairTicketPriority.Normal
);

public record RepairTicketStatusUpdateRequest(RepairTicketStatus Status);

public record RepairTicketCommentCreateRequest(string Content);

public record RepairTicketCommentResponse(
    int Id,
    string? UserName,
    bool IsAdmin,
    string Content,
    DateTime CreatedAtUtc
);

public record RepairTicketResponse(
    int Id,
    int ContractId,
    string? ContractName,
    string? TenantName,
    int? PropertyUnitId,
    string? PropertyUnitName,
    int? PropertyRoomId,
    string? PropertyRoomName,
    string Title,
    string Description,
    RepairTicketStatus Status,
    RepairTicketPriority Priority,
    string? CreatedByName,
    string? HandledByName,
    DateTime? ResolvedAtUtc,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc
);

public record RepairTicketDetailResponse(
    RepairTicketResponse Ticket,
    List<RepairTicketCommentResponse> Comments
);
