namespace RentalManager.Api.Dtos;

public record UserUpsertRequest(
    string Username,
    string? Password,
    int Role,
    string? DisplayName = null,
    string? Email = null,
    int? TenantId = null,
    bool IsActive = true
);

public record UserResponse(
    int Id,
    string Username,
    int Role,
    string? DisplayName,
    string? Email,
    int? TenantId,
    string? TenantName,
    bool IsActive,
    DateTime CreatedAtUtc
);
