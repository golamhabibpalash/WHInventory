using Application.Features.TicketCategoryManager.Commands;
using Application.Features.TicketCategoryManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

[Route("api/[controller]")]
public class TicketCategoryController : BaseApiController
{
    public TicketCategoryController(ISender sender) : base(sender)
    {
    }

    [Authorize]
    [HttpPost("CreateTicketCategory")]
    public async Task<ActionResult<ApiSuccessResult<CreateTicketCategoryResult>>> CreateTicketCategoryAsync(CreateTicketCategoryRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<CreateTicketCategoryResult> { Code = StatusCodes.Status200OK, Message = $"Success executing {nameof(CreateTicketCategoryAsync)}", Content = response });
    }

    [Authorize]
    [HttpPost("UpdateTicketCategory")]
    public async Task<ActionResult<ApiSuccessResult<UpdateTicketCategoryResult>>> UpdateTicketCategoryAsync(UpdateTicketCategoryRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<UpdateTicketCategoryResult> { Code = StatusCodes.Status200OK, Message = $"Success executing {nameof(UpdateTicketCategoryAsync)}", Content = response });
    }

    [Authorize]
    [HttpPost("DeleteTicketCategory")]
    public async Task<ActionResult<ApiSuccessResult<DeleteTicketCategoryResult>>> DeleteTicketCategoryAsync(DeleteTicketCategoryRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<DeleteTicketCategoryResult> { Code = StatusCodes.Status200OK, Message = $"Success executing {nameof(DeleteTicketCategoryAsync)}", Content = response });
    }

    [Authorize]
    [HttpGet("GetTicketCategoryList")]
    public async Task<ActionResult<ApiSuccessResult<GetTicketCategoryListResult>>> GetTicketCategoryListAsync(CancellationToken cancellationToken, [FromQuery] bool isDeleted = false)
    {
        var response = await _sender.Send(new GetTicketCategoryListRequest { IsDeleted = isDeleted }, cancellationToken);
        return Ok(new ApiSuccessResult<GetTicketCategoryListResult> { Code = StatusCodes.Status200OK, Message = $"Success executing {nameof(GetTicketCategoryListAsync)}", Content = response });
    }
}
