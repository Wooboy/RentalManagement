using RentalManager.Api.Models;

namespace RentalManager.Api.Dtos;

public record ElectricityMeterReadingUpsertRequest(
    int PropertyUnitId,
    int PropertyRoomId,
    DateTime ReadingDateUtc,
    decimal ReadingValue,
    string? Notes
);

public record MeterReadingLatestByRoomResponse(
    int PropertyUnitId,
    string? PropertyUnitName,
    int PropertyRoomId,
    string PropertyRoomName,
    DateTime? LastReadingDateUtc,
    decimal? LastReadingValue
);

public record MeterReadingListItemResponse(
    int Id,
    int PropertyUnitId,
    string? PropertyUnitName,
    int PropertyRoomId,
    string? PropertyRoomName,
    DateTime ReadingDateUtc,
    decimal ReadingValue,
    string? Notes,
    DateTime CreatedAtUtc
);

public record MeterReadingResponse(
    int Id,
    int PropertyUnitId,
    int PropertyRoomId,
    DateTime ReadingDateUtc,
    decimal ReadingValue,
    string? Notes,
    DateTime CreatedAtUtc
)
{
    public static MeterReadingResponse From(ElectricityMeterReading x) => new(
        x.Id, x.PropertyUnitId, x.PropertyRoomId, x.ReadingDateUtc, x.ReadingValue, x.Notes, x.CreatedAtUtc);
}
