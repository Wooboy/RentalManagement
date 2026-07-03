using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalManager.Api.Dtos;
using RentalManager.Api.Models;
using RentalManager.Api.Services;

namespace RentalManager.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/expenses")]
public class ExpensesController(ExpenseService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ExpenseListItemResponse>>> GetAll(
        [FromQuery] DateTime? startDateUtc, [FromQuery] DateTime? endDateUtc, [FromQuery] ExpenseCategory? category, [FromQuery] ExpenseSplitStatus? splitStatus)
        => Ok(await service.GetAllAsync(startDateUtc, endDateUtc, category, splitStatus));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ExpenseResponse>> GetById(int id)
        => Ok(await service.GetByIdAsync(id));

    [HttpPost]
    public async Task<ActionResult<ExpenseResponse>> Create(ExpenseUpsertRequest request)
        => Ok(await service.CreateAsync(request));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ExpenseResponse>> Update(int id, ExpenseUpsertRequest request)
        => Ok(await service.UpdateAsync(id, request));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }
}
