using System.Security.Claims;
using Application.Features.TicketManager.Commands;
using Application.Features.TicketManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using Infrastructure.FileDocumentManager;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

[Route("api/[controller]")]
public class TicketController : BaseApiController
{
    public TicketController(ISender sender) : base(sender)
    {
    }

    [Authorize]
    [HttpPost("CreateTicket")]
    public async Task<ActionResult<ApiSuccessResult<CreateTicketResult>>> CreateTicketAsync(CreateTicketRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<CreateTicketResult> { Code = StatusCodes.Status200OK, Message = $"Success executing {nameof(CreateTicketAsync)}", Content = response });
    }

    [Authorize]
    [HttpPost("UpdateTicket")]
    public async Task<ActionResult<ApiSuccessResult<UpdateTicketResult>>> UpdateTicketAsync(UpdateTicketRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<UpdateTicketResult> { Code = StatusCodes.Status200OK, Message = $"Success executing {nameof(UpdateTicketAsync)}", Content = response });
    }

    [Authorize]
    [HttpPost("AssignTicket")]
    public async Task<ActionResult<ApiSuccessResult<AssignTicketResult>>> AssignTicketAsync(AssignTicketRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<AssignTicketResult> { Code = StatusCodes.Status200OK, Message = $"Success executing {nameof(AssignTicketAsync)}", Content = response });
    }

    [Authorize]
    [HttpPost("ChangeTicketStatus")]
    public async Task<ActionResult<ApiSuccessResult<ChangeTicketStatusResult>>> ChangeTicketStatusAsync(ChangeTicketStatusRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<ChangeTicketStatusResult> { Code = StatusCodes.Status200OK, Message = $"Success executing {nameof(ChangeTicketStatusAsync)}", Content = response });
    }

    [Authorize]
    [HttpPost("ChangeTicketPriority")]
    public async Task<ActionResult<ApiSuccessResult<ChangeTicketPriorityResult>>> ChangeTicketPriorityAsync(ChangeTicketPriorityRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<ChangeTicketPriorityResult> { Code = StatusCodes.Status200OK, Message = $"Success executing {nameof(ChangeTicketPriorityAsync)}", Content = response });
    }

    [Authorize]
    [HttpPost("ResolveTicket")]
    public async Task<ActionResult<ApiSuccessResult<ResolveTicketResult>>> ResolveTicketAsync(ResolveTicketRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<ResolveTicketResult> { Code = StatusCodes.Status200OK, Message = $"Success executing {nameof(ResolveTicketAsync)}", Content = response });
    }

    [Authorize]
    [HttpPost("CloseTicket")]
    public async Task<ActionResult<ApiSuccessResult<CloseTicketResult>>> CloseTicketAsync(CloseTicketRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<CloseTicketResult> { Code = StatusCodes.Status200OK, Message = $"Success executing {nameof(CloseTicketAsync)}", Content = response });
    }

    [Authorize]
    [HttpPost("ReopenTicket")]
    public async Task<ActionResult<ApiSuccessResult<ReopenTicketResult>>> ReopenTicketAsync(ReopenTicketRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<ReopenTicketResult> { Code = StatusCodes.Status200OK, Message = $"Success executing {nameof(ReopenTicketAsync)}", Content = response });
    }

    [Authorize]
    [HttpPost("AddComment")]
    public async Task<ActionResult<ApiSuccessResult<AddTicketCommentResult>>> AddCommentAsync(AddTicketCommentRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<AddTicketCommentResult> { Code = StatusCodes.Status200OK, Message = $"Success executing {nameof(AddCommentAsync)}", Content = response });
    }

    [Authorize]
    [HttpPost("SetTicketTags")]
    public async Task<ActionResult<ApiSuccessResult<SetTicketTagsResult>>> SetTicketTagsAsync(SetTicketTagsRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<SetTicketTagsResult> { Code = StatusCodes.Status200OK, Message = $"Success executing {nameof(SetTicketTagsAsync)}", Content = response });
    }

    [Authorize]
    [HttpGet("GetTicketList")]
    public async Task<ActionResult<ApiSuccessResult<GetTicketListResult>>> GetTicketListAsync([FromQuery] GetTicketListRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<GetTicketListResult> { Code = StatusCodes.Status200OK, Message = $"Success executing {nameof(GetTicketListAsync)}", Content = response });
    }

    [Authorize]
    [HttpGet("GetTicket")]
    public async Task<ActionResult<ApiSuccessResult<GetTicketResult>>> GetTicketAsync([FromQuery] string id, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new GetTicketRequest { Id = id }, cancellationToken);
        return Ok(new ApiSuccessResult<GetTicketResult> { Code = StatusCodes.Status200OK, Message = $"Success executing {nameof(GetTicketAsync)}", Content = response });
    }

    [Authorize]
    [HttpGet("GetTicketHistory")]
    public async Task<ActionResult<ApiSuccessResult<GetTicketHistoryResult>>> GetTicketHistoryAsync([FromQuery] string ticketId, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new GetTicketHistoryRequest { TicketId = ticketId }, cancellationToken);
        return Ok(new ApiSuccessResult<GetTicketHistoryResult> { Code = StatusCodes.Status200OK, Message = $"Success executing {nameof(GetTicketHistoryAsync)}", Content = response });
    }

    [Authorize]
    [HttpGet("GetTicketDashboard")]
    public async Task<ActionResult<ApiSuccessResult<GetTicketDashboardResult>>> GetTicketDashboardAsync(CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new GetTicketDashboardRequest(), cancellationToken);
        return Ok(new ApiSuccessResult<GetTicketDashboardResult> { Code = StatusCodes.Status200OK, Message = $"Success executing {nameof(GetTicketDashboardAsync)}", Content = response });
    }

    [Authorize]
    [HttpPost("UploadTicketAttachment")]
    public async Task<ActionResult<ApiSuccessResult<UploadTicketAttachmentResult>>> UploadTicketAttachmentAsync([FromForm] string ticketId, IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("Invalid file.");
        }

        var createdById = User.FindFirstValue(ClaimTypes.NameIdentifier);

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, cancellationToken);
        var fileData = memoryStream.ToArray();
        var extension = Path.GetExtension(file.FileName).TrimStart('.');

        var response = await _sender.Send(new UploadTicketAttachmentRequest
        {
            TicketId = ticketId,
            OriginalFileName = file.FileName,
            Extension = extension,
            Data = fileData,
            Size = fileData.Length,
            CreatedById = createdById
        }, cancellationToken);

        return Ok(new ApiSuccessResult<UploadTicketAttachmentResult> { Code = StatusCodes.Status200OK, Message = $"Success executing {nameof(UploadTicketAttachmentAsync)}", Content = response });
    }

    [Authorize]
    [HttpGet("GetTicketAttachments")]
    public async Task<ActionResult<ApiSuccessResult<GetTicketAttachmentsResult>>> GetTicketAttachmentsAsync([FromQuery] string ticketId, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new GetTicketAttachmentsRequest { TicketId = ticketId }, cancellationToken);
        return Ok(new ApiSuccessResult<GetTicketAttachmentsResult> { Code = StatusCodes.Status200OK, Message = $"Success executing {nameof(GetTicketAttachmentsAsync)}", Content = response });
    }

    [Authorize]
    [HttpGet("DownloadTicketAttachment")]
    public async Task<IActionResult> DownloadTicketAttachmentAsync([FromQuery] string attachmentId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DownloadTicketAttachmentRequest { AttachmentId = attachmentId }, cancellationToken);

        if (result?.Data == null)
        {
            return NotFound("Attachment not found.");
        }

        var extension = string.IsNullOrEmpty(result.Extension) ? string.Empty : "." + result.Extension.TrimStart('.');
        var mimeType = FileDocumentHelper.GetMimeType(extension);

        return File(result.Data, mimeType, result.OriginalName);
    }

    [Authorize]
    [HttpPost("DeleteTicketAttachment")]
    public async Task<ActionResult<ApiSuccessResult<DeleteTicketAttachmentResult>>> DeleteTicketAttachmentAsync(DeleteTicketAttachmentRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<DeleteTicketAttachmentResult> { Code = StatusCodes.Status200OK, Message = $"Success executing {nameof(DeleteTicketAttachmentAsync)}", Content = response });
    }
}
