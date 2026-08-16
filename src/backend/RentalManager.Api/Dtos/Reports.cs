namespace RentalManager.Api.Dtos;

public record MonthlyReportResponse(int Year, int Month, decimal Income, decimal Expense, decimal Profit);
