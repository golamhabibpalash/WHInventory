using System.Security.Claims;
using Application.Features.NotificationManager.Commands;
using Application.Features.NotificationManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

[Route("api/[controller]")]
public class NotificationController : BaseApiController
{
    public NotificationController(ISender sender) : base(sender)
    {
    }

    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    [Authorize]
    [HttpPost("CreateNotification")]
    public async Task<ActionResult<ApiSuccessResult<CreateNotificationResult>>> CreateNotificationAsync(CreateNotificationRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<CreateNotificationResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(CreateNotificationAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetNotificationList")]
    public async Task<ActionResult<ApiSuccessResult<GetNotificationListResult>>> GetNotificationListAsync(
        CancellationToken cancellationToken,
        [FromQuery] bool onlyUnread = false,
        [FromQuery] int take = 50
        )
    {
        var request = new GetNotificationListRequest { UserId = CurrentUserId, OnlyUnread = onlyUnread, Take = take };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetNotificationListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetNotificationListAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetNotificationUnreadCount")]
    public async Task<ActionResult<ApiSuccessResult<GetNotificationUnreadCountResult>>> GetNotificationUnreadCountAsync(CancellationToken cancellationToken)
    {
        var request = new GetNotificationUnreadCountRequest { UserId = CurrentUserId };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetNotificationUnreadCountResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetNotificationUnreadCountAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("MarkNotificationRead")]
    public async Task<ActionResult<ApiSuccessResult<MarkNotificationReadResult>>> MarkNotificationReadAsync(MarkNotificationReadRequest request, CancellationToken cancellationToken)
    {
        // Never trust a client-supplied identity — the reader is always the caller.
        var secured = new MarkNotificationReadRequest { Id = request.Id, UserId = CurrentUserId };
        var response = await _sender.Send(secured, cancellationToken);

        return Ok(new ApiSuccessResult<MarkNotificationReadResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(MarkNotificationReadAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("MarkAllNotificationsRead")]
    public async Task<ActionResult<ApiSuccessResult<MarkAllNotificationsReadResult>>> MarkAllNotificationsReadAsync(CancellationToken cancellationToken)
    {
        var request = new MarkAllNotificationsReadRequest { UserId = CurrentUserId };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<MarkAllNotificationsReadResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(MarkAllNotificationsReadAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("DeleteNotification")]
    public async Task<ActionResult<ApiSuccessResult<DeleteNotificationResult>>> DeleteNotificationAsync(DeleteNotificationRequest request, CancellationToken cancellationToken)
    {
        var secured = new DeleteNotificationRequest { Id = request.Id, UserId = CurrentUserId };
        var response = await _sender.Send(secured, cancellationToken);

        return Ok(new ApiSuccessResult<DeleteNotificationResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(DeleteNotificationAsync)}",
            Content = response
        });
    }
}
