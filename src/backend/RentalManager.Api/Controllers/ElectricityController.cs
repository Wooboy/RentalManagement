using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalManager.Api.Dtos;
using RentalManager.Api.Services;

namespace RentalManager.Api.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/electricity")]
public class ElectricityController(ElectricityBillingService service) : ControllerBase
{
    [HttpPost("calculate")]
    public ActionResult<ElectricityCalculateResponse> Calculate(ElectricityCalculateRequest request)
        => Ok(ElectricityCalculationService.Calculate(request));

    [HttpPost("preview-from-expenses")]
    public async Task<ActionResult<ElectricityPreviewResponse>> PreviewFromExpenses(ElectricityExpensePreviewRequest request)
        => Ok(await service.PreviewFromExpensesAsync(request));

    [HttpPost("bills")]
    public async Task<ActionResult<ElectricityBillSaveResponse>> SaveBill(ElectricityBillSaveRequest request)
        => Ok(await service.SaveBillAsync(request));

    [HttpGet("bills")]
    public async Task<ActionResult<IEnumerable<ElectricityBillListItemResponse>>> GetBills([FromQuery] int? contractId)
        => Ok(await service.GetBillsAsync(contractId));

    [HttpGet("bills/{billId:int}")]
    public async Task<ActionResult<ElectricityBillDetailResponse>> GetBillDetail(int billId)
        => Ok(await service.GetBillDetailAsync(billId));

    [HttpPost("bills/{billId:int}/create-charges")]
    public async Task<ActionResult<ElectricityCreateChargesResponse>> CreateChargesFromBill(
        int billId, [FromBody] ElectricityCreateChargesRequest? request, [FromQuery] string? mode)
        => Ok(await service.CreateChargesFromBillAsync(billId, request, mode));
}
