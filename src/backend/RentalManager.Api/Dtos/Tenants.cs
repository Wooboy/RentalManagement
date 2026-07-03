using RentalManager.Api.Models;

namespace RentalManager.Api.Dtos;

public record TenantUpsertRequest(
    TenantType Type,
    string Name,
    DateTime? BirthdayUtc,
    string? TaxId,
    string? PersonalId,
    string Phone,
    string? Email,
    string? Address,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? Notes
);

public record TenantResponse(
    int Id,
    TenantType Type,
    string Name,
    DateTime? BirthdayUtc,
    string? TaxId,
    string? PersonalId,
    string Phone,
    string? Email,
    string? Address,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? Notes,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc
)
{
    public static TenantResponse From(Tenant x) => new(
        x.Id, x.Type, x.Name, x.BirthdayUtc, x.TaxId, x.PersonalId, x.Phone, x.Email, x.Address,
        x.EmergencyContactName, x.EmergencyContactPhone, x.Notes, x.CreatedAtUtc, x.UpdatedAtUtc);
}
