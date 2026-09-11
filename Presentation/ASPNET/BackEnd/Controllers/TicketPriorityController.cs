using Application.Features.TicketPriorityManager.Commands;
using Application.Features.TicketPriorityManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

[Route("api/[controller]")]
public class TicketPriorityController : BaseApiController
{
    public TicketPriorityController(ISender sender) : base(sender)
    {
    }

    [Authorize]
    [HttpPost("CreateTicketPriority")]
    public async Task<ActionResult<ApiSuccessResult<CreateTicketPriorityResult>>> CreateTicketPriorityAsync(CreateTicketPriorityRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<CreateTicketPriorityResult> { Code = StatusCodes.Status200OK, Message = $"Success executing {nameof(CreateTicketPriorityAsync)}", Content = response });
    }

    [Authorize]
    [HttpPost("UpdateTicketPriority")]
    public async Task<ActionResult<ApiSuccessResult<UpdateTicketPriorityResult>>> UpdateTicketPriorityAsync(UpdateTicketPriorityRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<UpdateTicketPriorityResult> { Code = StatusCodes.Status200OK, Message = $"Success executing {nameof(UpdateTicketPriorityAsync)}", Content = response });
    }

    [Authorize]
    [HttpPost("DeleteTicketPriority")]
    public async Task<ActionResult<ApiSuccessResult<DeleteTicketPriorityResult>>> DeleteTicketPriorityAsync(DeleteTicketPriorityRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<DeleteTicketPriorityResult> { Code = StatusCodes.Status200OK, Message = $"Success executing {nameof(DeleteTicketPriorityAsync)}", Content = response });
    }

    [Authorize]
    [HttpGet("GetTicketPriorityList")]
    public async Task<ActionResult<ApiSuccessResult<GetTicketPriorityListResult>>> GetTicketPriorityListAsync(CancellationToken cancellationToken, [FromQuery] bool isDeleted = false)
    {
        var response = await _sender.Send(new GetTicketPriorityListRequest { IsDeleted = isDeleted }, cancellationToken);
        return Ok(new ApiSuccessResult<GetTicketPriorityListResult> { Code = StatusCodes.Status200OK, Message = $"Success executing {nameof(GetTicketPriorityListAsync)}", Content = response });
    }
}
