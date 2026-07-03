using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalManager.Api.Dtos;
using RentalManager.Api.Models;
using RentalManager.Api.Services;

namespace RentalManager.Api.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/repair-tickets")]
public class RepairTicketsController(RepairTicketService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RepairTicketResponse>>> GetAll(
        [FromQuery] RepairTicketStatus? status, [FromQuery] string? keyword)
        => Ok(await service.GetAllAsync(status, keyword));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RepairTicketDetailResponse>> GetDetail(int id)
        => Ok(await service.GetDetailAsync(id));

    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<RepairTicketResponse>> UpdateStatus(int id, RepairTicketStatusUpdateRequest request)
        => Ok(await service.UpdateStatusAsync(id, request.Status, CurrentUserId()));

    [HttpPost("{id:int}/comments")]
    public async Task<ActionResult<RepairTicketCommentResponse>> AddComment(int id, RepairTicketCommentCreateRequest request)
        => Ok(await service.AddCommentAsync(id, CurrentUserId(), request.Content));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }

    private int? CurrentUserId()
    {
        var text = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return int.TryParse(text, out var id) ? id : null;
    }
}
