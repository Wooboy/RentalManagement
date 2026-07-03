using RentalManager.Api.Models;

namespace RentalManager.Api.Dtos;

public record ExpenseUpsertRequest(
    int PropertyUnitId,
    int? PropertyRoomId,
    ExpenseCategory Category,
    DateTime BillingStartUtc,
    DateTime BillingEndUtc,
    decimal Amount,
    decimal? UsageUnits,
    ExpenseSplitStatus SplitStatus,
    string? Notes,
    DateTime? OccurredAtUtc
);

public record ExpenseListItemResponse(
    int Id,
    int PropertyUnitId,
    string? PropertyUnitName,
    int? PropertyRoomId,
    string? PropertyRoomName,
    ExpenseCategory Category,
    DateTime BillingStartUtc,
    DateTime BillingEndUtc,
    decimal Amount,
    decimal? UsageUnits,
    ExpenseSplitStatus SplitStatus,
    string? Notes,
    DateTime OccurredAtUtc,
    DateTime CreatedAtUtc
);

public record ExpenseResponse(
    int Id,
    int PropertyUnitId,
    int? PropertyRoomId,
    ExpenseCategory Category,
    DateTime BillingStartUtc,
    DateTime BillingEndUtc,
    decimal Amount,
    decimal? UsageUnits,
    ExpenseSplitStatus SplitStatus,
    string? Notes,
    DateTime OccurredAtUtc,
    DateTime CreatedAtUtc
)
{
    public static ExpenseResponse From(ExpenseRecord x) => new(
        x.Id, x.PropertyUnitId, x.PropertyRoomId, x.Category, x.BillingStartUtc, x.BillingEndUtc,
        x.Amount, x.UsageUnits, x.SplitStatus, x.Notes, x.OccurredAtUtc, x.CreatedAtUtc);
}
