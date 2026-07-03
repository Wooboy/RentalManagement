using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalManager.Api.Dtos;
using RentalManager.Api.Services;

namespace RentalManager.Api.Controllers;

[ApiController]
[Route("api/tenants")]
[Authorize(Policy = "AdminOnly")]
public class TenantsController(TenantService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TenantResponse>>> GetAll([FromQuery] string? keyword)
        => Ok(await service.GetAllAsync(keyword));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TenantResponse>> GetById(int id)
        => Ok(await service.GetByIdAsync(id));

    [HttpPost]
    public async Task<ActionResult<TenantResponse>> Create(TenantUpsertRequest request)
        => Ok(await service.CreateAsync(request));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<TenantResponse>> Update(int id, TenantUpsertRequest request)
        => Ok(await service.UpdateAsync(id, request));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }
}
