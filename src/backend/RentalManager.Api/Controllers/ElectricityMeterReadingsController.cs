using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalManager.Api.Dtos;
using RentalManager.Api.Services;

namespace RentalManager.Api.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/electricity-meter-readings")]
public class ElectricityMeterReadingsController(MeterReadingService service) : ControllerBase
{
    [HttpGet("latest-by-room")]
    public async Task<ActionResult<IEnumerable<MeterReadingLatestByRoomResponse>>> GetLatestByRoom([FromQuery] int? propertyUnitId)
        => Ok(await service.GetLatestByRoomAsync(propertyUnitId));

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MeterReadingListItemResponse>>> GetAll([FromQuery] int? propertyUnitId, [FromQuery] int? propertyRoomId)
        => Ok(await service.GetAllAsync(propertyUnitId, propertyRoomId));

    [HttpPost]
    public async Task<ActionResult<MeterReadingResponse>> Create(ElectricityMeterReadingUpsertRequest request)
        => Ok(await service.CreateAsync(request));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<MeterReadingResponse>> Update(int id, ElectricityMeterReadingUpsertRequest request)
        => Ok(await service.UpdateAsync(id, request));
}
