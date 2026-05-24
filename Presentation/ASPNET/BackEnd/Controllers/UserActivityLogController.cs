using Application.Features.UserActivityLogManager.Commands;
using Application.Features.UserActivityLogManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

[Route("api/[controller]")]
public class UserActivityLogController : BaseApiController
{
    public UserActivityLogController(ISender sender) : base(sender) { }

    [Authorize]
    [HttpPost("CreateUserActivityLog")]
    public async Task<ActionResult<ApiSuccessResult<CreateUserActivityLogResult>>> CreateUserActivityLogAsync(
        CreateUserActivityLogRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<CreateUserActivityLogResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(CreateUserActivityLogAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetUserActivityLogList")]
    public async Task<ActionResult<ApiSuccessResult<GetUserActivityLogListResult>>> GetUserActivityLogListAsync(
        CancellationToken cancellationToken,
        [FromQuery] string? userId = null,
        [FromQuery] string? activityType = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var request = new GetUserActivityLogListRequest
        {
            UserId = userId,
            ActivityType = activityType,
            FromDate = fromDate,
            ToDate = toDate
        };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetUserActivityLogListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetUserActivityLogListAsync)}",
            Content = response
        });
    }
}
