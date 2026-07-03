using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalManager.Api.Dtos;
using RentalManager.Api.Services;

namespace RentalManager.Api.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/properties")]
public class PropertiesController(PropertyService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PropertyResponse>>> GetAll([FromQuery] string? keyword)
        => Ok(await service.GetAllAsync(keyword));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PropertyResponse>> GetById(int id)
        => Ok(await service.GetByIdAsync(id));

    [HttpPost]
    public async Task<ActionResult<PropertyResponse>> Create(PropertyUpsertRequest request)
        => Ok(await service.CreateAsync(request));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<PropertyResponse>> Update(int id, PropertyUpsertRequest request)
        => Ok(await service.UpdateAsync(id, request));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }
}
