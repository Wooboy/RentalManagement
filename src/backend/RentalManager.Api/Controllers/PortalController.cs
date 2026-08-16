using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalManager.Api.Common;
using RentalManager.Api.Dtos;
using RentalManager.Api.Models;
using RentalManager.Api.Services;

namespace RentalManager.Api.Controllers;

/// <summary>房客入口 API，所有端點皆以 JWT 內的 tenantId 過濾資料。</summary>
[ApiController]
[Authorize(Policy = "TenantOnly")]
[Route("api/portal")]
public class PortalController(
    PortalService portalService,
    RepairTicketService repairTicketService,
    AttachmentService attachmentService) : ControllerBase
{
    [HttpGet("contracts")]
    public async Task<ActionResult<IEnumerable<ContractResponse>>> GetMyContracts()
        => Ok(await portalService.GetMyContractsAsync(CurrentTenantId()));

    [HttpGet("charges")]
    public async Task<ActionResult<IEnumerable<ChargeListItemResponse>>> GetMyCharges(
        [FromQuery] int? contractId, [FromQuery] bool? isPaid)
        => Ok(await portalService.GetMyChargesAsync(CurrentTenantId(), contractId, isPaid));

    [HttpGet("repair-tickets")]
    public async Task<ActionResult<IEnumerable<RepairTicketResponse>>> GetMyRepairTickets()
        => Ok(await repairTicketService.GetByTenantAsync(CurrentTenantId()));

    [HttpPost("repair-tickets")]
    public async Task<ActionResult<RepairTicketResponse>> CreateRepairTicket(RepairTicketCreateRequest request)
        => Ok(await repairTicketService.CreateAsync(CurrentTenantId(), CurrentUserId(), request));

    [HttpGet("repair-tickets/{id:int}")]
    public async Task<ActionResult<RepairTicketDetailResponse>> GetRepairTicketDetail(int id)
        => Ok(await repairTicketService.GetDetailAsync(id, CurrentTenantId()));

    [HttpPost("repair-tickets/{id:int}/comments")]
    public async Task<ActionResult<RepairTicketCommentResponse>> AddRepairTicketComment(int id, RepairTicketCommentCreateRequest request)
        => Ok(await repairTicketService.AddCommentAsync(id, CurrentUserId(), request.Content, CurrentTenantId()));

    [HttpGet("attachments")]
    public async Task<ActionResult<IEnumerable<AttachmentResponse>>> GetAttachments(
        [FromQuery] AttachmentEntityType entityType, [FromQuery] int entityId)
    {
        await EnsureAttachmentTargetAccessAsync(entityType, entityId);
        return Ok(await attachmentService.GetListAsync(entityType, entityId));
    }

    [HttpPost("attachments")]
    public async Task<ActionResult<AttachmentResponse>> UploadAttachment(
        [FromForm] AttachmentEntityType entityType, [FromForm] int entityId, IFormFile file)
    {
        // 房客僅能上傳附件到自己的報修單
        if (entityType != AttachmentEntityType.RepairTicket) throw new DomainNotFoundException();
        await EnsureAttachmentTargetAccessAsync(entityType, entityId);
        return Ok(await attachmentService.UploadAsync(entityType, entityId, file, CurrentUserId()));
    }

    [HttpGet("attachments/{id:int}/download")]
    public async Task<IActionResult> DownloadAttachment(int id)
    {
        var attachment = await attachmentService.FindAsync(id);
        await EnsureAttachmentTargetAccessAsync(attachment.EntityType, attachment.EntityId);
        var (content, contentType, fileName) = await attachmentService.DownloadAsync(id);
        return File(content, contentType, fileName);
    }

    private async Task EnsureAttachmentTargetAccessAsync(AttachmentEntityType entityType, int entityId)
    {
        if (!await portalService.CanAccessAttachmentTargetAsync(CurrentTenantId(), entityType, entityId))
        {
            throw new DomainNotFoundException();
        }
    }

    private int CurrentTenantId()
    {
        var text = User.FindFirstValue("tenantId");
        if (!int.TryParse(text, out var tenantId) || tenantId <= 0)
        {
            throw new DomainUnauthorizedException("帳號未關聯租客");
        }
        return tenantId;
    }

    private int? CurrentUserId()
    {
        var text = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return int.TryParse(text, out var id) ? id : null;
    }
}
