using RentalManager.Api.Models;

namespace RentalManager.Api.Dtos;

public record ChargeUpsertRequest(
    int ContractId,
    ChargeCategory Category,
    DateTime? OccurredAtUtc,
    DateTime BillingStartUtc,
    DateTime BillingEndUtc,
    decimal? MeterStart,
    decimal? MeterEnd,
    decimal? UsageUnits,
    decimal Amount,
    string? Notes,
    bool IsPaid,
    DateTime? PaidAtUtc
);

public record ChargeListItemResponse(
    int Id,
    int ContractId,
    string? ContractName,
    string? ContractNo,
    ChargeCategory Category,
    DateTime OccurredAtUtc,
    DateTime BillingStartUtc,
    DateTime BillingEndUtc,
    decimal? MeterStart,
    decimal? MeterEnd,
    decimal? UsageUnits,
    decimal Amount,
    string? Notes,
    bool IsPaid,
    DateTime? PaidAtUtc,
    DateTime CreatedAtUtc
);

public record ChargeResponse(
    int Id,
    int ContractId,
    ChargeCategory Category,
    DateTime OccurredAtUtc,
    DateTime BillingStartUtc,
    DateTime BillingEndUtc,
    decimal? MeterStart,
    decimal? MeterEnd,
    decimal? UsageUnits,
    decimal Amount,
    string? Notes,
    bool IsPaid,
    DateTime? PaidAtUtc,
    DateTime CreatedAtUtc
)
{
    public static ChargeResponse From(ChargeRecord x) => new(
        x.Id, x.ContractId, x.Category, x.OccurredAtUtc, x.BillingStartUtc, x.BillingEndUtc,
        x.MeterStart, x.MeterEnd, x.UsageUnits, x.Amount, x.Notes, x.IsPaid, x.PaidAtUtc, x.CreatedAtUtc);
}
