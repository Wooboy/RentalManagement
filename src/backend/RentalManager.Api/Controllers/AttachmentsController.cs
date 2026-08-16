using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalManager.Api.Dtos;
using RentalManager.Api.Models;
using RentalManager.Api.Services;

namespace RentalManager.Api.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/attachments")]
public class AttachmentsController(AttachmentService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AttachmentResponse>>> GetList(
        [FromQuery] AttachmentEntityType entityType, [FromQuery] int entityId)
        => Ok(await service.GetListAsync(entityType, entityId));

    [HttpPost]
    public async Task<ActionResult<AttachmentResponse>> Upload(
        [FromForm] AttachmentEntityType entityType, [FromForm] int entityId, IFormFile file)
    {
        var userIdText = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        int? userId = int.TryParse(userIdText, out var parsed) ? parsed : null;
        return Ok(await service.UploadAsync(entityType, entityId, file, userId));
    }

    [HttpGet("{id:int}/download")]
    public async Task<IActionResult> Download(int id)
    {
        var (content, contentType, fileName) = await service.DownloadAsync(id);
        return File(content, contentType, fileName);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }
}
