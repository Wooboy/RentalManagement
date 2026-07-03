using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalManager.Api.Dtos;
using RentalManager.Api.Models;
using RentalManager.Api.Services;

namespace RentalManager.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/charges")]
public class ChargesController(ChargeService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ChargeListItemResponse>>> GetAll(
        [FromQuery] DateTime? startDateUtc, [FromQuery] DateTime? endDateUtc, [FromQuery] bool? isPaid, [FromQuery] ChargeCategory? category)
        => Ok(await service.GetAllAsync(startDateUtc, endDateUtc, isPaid, category));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ChargeResponse>> GetById(int id)
        => Ok(await service.GetByIdAsync(id));

    [HttpPost]
    public async Task<ActionResult<ChargeResponse>> Create(ChargeUpsertRequest request)
        => Ok(await service.CreateAsync(request));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ChargeResponse>> Update(int id, ChargeUpsertRequest request)
        => Ok(await service.UpdateAsync(id, request));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }
}
