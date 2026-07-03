using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalManager.Api.Dtos;
using RentalManager.Api.Services;

namespace RentalManager.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/reports")]
public class ReportsController(ReportService service) : ControllerBase
{
    [HttpGet("monthly")]
    public async Task<ActionResult<MonthlyReportResponse>> Monthly([FromQuery] int year, [FromQuery] int month)
        => Ok(await service.GetMonthlyAsync(year, month));
}
