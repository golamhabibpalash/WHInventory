using Application.Features.QuickShortcutManager.Commands;
using Application.Features.QuickShortcutManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

[Route("api/[controller]")]
public class QuickShortcutController : BaseApiController
{
    public QuickShortcutController(ISender sender) : base(sender)
    {
    }

    [Authorize]
    [HttpPost("CreateQuickShortcut")]
    public async Task<ActionResult<ApiSuccessResult<CreateQuickShortcutResult>>> CreateQuickShortcutAsync(CreateQuickShortcutRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<CreateQuickShortcutResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(CreateQuickShortcutAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("UpdateQuickShortcut")]
    public async Task<ActionResult<ApiSuccessResult<UpdateQuickShortcutResult>>> UpdateQuickShortcutAsync(UpdateQuickShortcutRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<UpdateQuickShortcutResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(UpdateQuickShortcutAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("DeleteQuickShortcut")]
    public async Task<ActionResult<ApiSuccessResult<DeleteQuickShortcutResult>>> DeleteQuickShortcutAsync(DeleteQuickShortcutRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<DeleteQuickShortcutResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(DeleteQuickShortcutAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetQuickShortcutList")]
    public async Task<ActionResult<ApiSuccessResult<GetQuickShortcutListResult>>> GetQuickShortcutListAsync(
        CancellationToken cancellationToken,
        [FromQuery] bool isDeleted = false
        )
    {
        var request = new GetQuickShortcutListRequest { IsDeleted = isDeleted };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetQuickShortcutListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetQuickShortcutListAsync)}",
            Content = response
        });
    }
}
