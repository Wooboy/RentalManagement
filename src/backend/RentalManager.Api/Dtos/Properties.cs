using RentalManager.Api.Models;

namespace RentalManager.Api.Dtos;

public record PropertyUpsertRequest(string? Code, string Name, string Address, string? Notes);

public record PropertyResponse(
    int Id,
    string Code,
    string Name,
    string Address,
    string? Notes,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc
)
{
    public static PropertyResponse From(PropertyUnit x) => new(
        x.Id, x.Code, x.Name, x.Address, x.Notes, x.CreatedAtUtc, x.UpdatedAtUtc);
}

public record RoomUpsertRequest(int PropertyUnitId, string? Code, string Name, string? Notes);

public record RoomResponse(
    int Id,
    int PropertyUnitId,
    PropertyResponse? PropertyUnit,
    string Code,
    string Name,
    string? Notes,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc
)
{
    public static RoomResponse From(PropertyRoom x) => new(
        x.Id, x.PropertyUnitId,
        x.PropertyUnit is null ? null : PropertyResponse.From(x.PropertyUnit),
        x.Code, x.Name, x.Notes, x.CreatedAtUtc, x.UpdatedAtUtc);
}
