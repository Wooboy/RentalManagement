namespace RentalManager.Api.Dtos;

public record AdminUserUpsertRequest(string Username, string? Password, int Role);
public record AdminUserResponse(int Id, string Username, int Role, DateTime CreatedAtUtc);
