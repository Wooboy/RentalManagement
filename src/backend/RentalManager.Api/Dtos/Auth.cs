namespace RentalManager.Api.Dtos;

public record LoginRequest(string Username, string Password);
public record LoginResponse(string Token, int UserId, string Username, int Role, int? TenantId, string? DisplayName);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
