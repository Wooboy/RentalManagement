using Microsoft.EntityFrameworkCore;
using RentalManager.Api.Data;
using RentalManager.Api.Dtos;

namespace RentalManager.Api.Services;

public class ReportService(AppDbContext db)
{
    public async Task<MonthlyReportResponse> GetMonthlyAsync(int year, int month)
    {
        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);

        var income = await db.ChargeRecords
            .Where(x => x.BillingStartUtc >= start && x.BillingStartUtc < end)
            .SumAsync(x => (decimal?)x.Amount) ?? 0;

        var expense = await db.ExpenseRecords
            .Where(x => x.BillingStartUtc >= start && x.BillingStartUtc < end)
            .SumAsync(x => (decimal?)x.Amount) ?? 0;

        return new MonthlyReportResponse(year, month, income, expense, income - expense);
    }
}
