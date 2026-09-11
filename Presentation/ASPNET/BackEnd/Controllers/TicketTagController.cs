using Application.Features.TicketTagManager.Commands;
using Application.Features.TicketTagManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

[Route("api/[controller]")]
public class TicketTagController : BaseApiController
{
    public TicketTagController(ISender sender) : base(sender)
    {
    }

    [Authorize]
    [HttpPost("CreateTicketTag")]
    public async Task<ActionResult<ApiSuccessResult<CreateTicketTagResult>>> CreateTicketTagAsync(CreateTicketTagRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<CreateTicketTagResult> { Code = StatusCodes.Status200OK, Message = $"Success executing {nameof(CreateTicketTagAsync)}", Content = response });
    }

    [Authorize]
    [HttpPost("DeleteTicketTag")]
    public async Task<ActionResult<ApiSuccessResult<DeleteTicketTagResult>>> DeleteTicketTagAsync(DeleteTicketTagRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<DeleteTicketTagResult> { Code = StatusCodes.Status200OK, Message = $"Success executing {nameof(DeleteTicketTagAsync)}", Content = response });
    }

    [Authorize]
    [HttpGet("GetTicketTagList")]
    public async Task<ActionResult<ApiSuccessResult<GetTicketTagListResult>>> GetTicketTagListAsync(CancellationToken cancellationToken, [FromQuery] bool isDeleted = false)
    {
        var response = await _sender.Send(new GetTicketTagListRequest { IsDeleted = isDeleted }, cancellationToken);
        return Ok(new ApiSuccessResult<GetTicketTagListResult> { Code = StatusCodes.Status200OK, Message = $"Success executing {nameof(GetTicketTagListAsync)}", Content = response });
    }
}
