using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalManager.Api.Dtos;
using RentalManager.Api.Services;

namespace RentalManager.Api.Controllers;

[ApiController]
[Route("api/contracts")]
[Authorize]
public class ContractsController(ContractService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ContractResponse>>> GetAll(
        [FromQuery] string? keyword, [FromQuery] int? propertyUnitId, [FromQuery] int? propertyRoomId, [FromQuery] int? status)
        => Ok(await service.GetAllAsync(keyword, propertyUnitId, propertyRoomId, status));

    [HttpPost]
    public async Task<ActionResult<ContractResponse>> Create(ContractUpsertRequest request)
        => Ok(await service.CreateAsync(request));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ContractResponse>> Update(int id, ContractUpsertRequest request)
        => Ok(await service.UpdateAsync(id, request));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }

    [HttpPost("batch-create-period-charges")]
    public async Task<ActionResult<ContractBatchChargeCreateResponse>> BatchCreatePeriodCharges(ContractBatchChargeCreateRequest request)
        => Ok(await service.BatchCreatePeriodChargesAsync(request));
}
