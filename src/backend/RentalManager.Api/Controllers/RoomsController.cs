using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalManager.Api.Dtos;
using RentalManager.Api.Services;

namespace RentalManager.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/rooms")]
public class RoomsController(RoomService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RoomResponse>>> GetAll([FromQuery] int? propertyUnitId)
        => Ok(await service.GetAllAsync(propertyUnitId));

    [HttpPost]
    public async Task<ActionResult<RoomResponse>> Create(RoomUpsertRequest request)
        => Ok(await service.CreateAsync(request));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<RoomResponse>> Update(int id, RoomUpsertRequest request)
        => Ok(await service.UpdateAsync(id, request));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }
}
