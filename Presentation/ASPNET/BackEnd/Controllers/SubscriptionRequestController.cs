using Application.Features.SubscriptionRequestManager.Commands;
using Application.Features.SubscriptionRequestManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

/// <summary>
/// Plan-change requests. A tenant creates/cancels and views its own requests (any authenticated
/// user); a platform administrator lists and decides every tenant's requests (gated on the Tenants
/// role, which provisioning withholds from tenant administrators).
/// </summary>
[Route("api/[controller]")]
public class SubscriptionRequestController : BaseApiController
{
    public SubscriptionRequestController(ISender sender) : base(sender)
    {
    }

    // ---- Tenant self-service ----

    [Authorize]
    [HttpGet("GetMySubscriptionRequestList")]
    public async Task<ActionResult<ApiSuccessResult<GetMySubscriptionRequestListResult>>> GetMySubscriptionRequestListAsync(CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new GetMySubscriptionRequestListRequest(), cancellationToken);

        return Ok(new ApiSuccessResult<GetMySubscriptionRequestListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetMySubscriptionRequestListAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("CreateSubscriptionRequest")]
    public async Task<ActionResult<ApiSuccessResult<CreateSubscriptionRequestResult>>> CreateSubscriptionRequestAsync(CreateSubscriptionRequestRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<CreateSubscriptionRequestResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(CreateSubscriptionRequestAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("CancelSubscriptionRequest")]
    public async Task<ActionResult<ApiSuccessResult<CancelSubscriptionRequestResult>>> CancelSubscriptionRequestAsync(CancelSubscriptionRequestRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<CancelSubscriptionRequestResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(CancelSubscriptionRequestAsync)}",
            Content = response
        });
    }

    // ---- Platform administration ----

    [Authorize(Roles = "Tenants")]
    [HttpGet("GetSubscriptionRequestList")]
    public async Task<ActionResult<ApiSuccessResult<GetSubscriptionRequestListResult>>> GetSubscriptionRequestListAsync(
        CancellationToken cancellationToken,
        [FromQuery] bool pendingOnly = false)
    {
        var response = await _sender.Send(new GetSubscriptionRequestListRequest { PendingOnly = pendingOnly }, cancellationToken);

        return Ok(new ApiSuccessResult<GetSubscriptionRequestListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetSubscriptionRequestListAsync)}",
            Content = response
        });
    }

    [Authorize(Roles = "Tenants")]
    [HttpPost("ApproveSubscriptionRequest")]
    public async Task<ActionResult<ApiSuccessResult<ApproveSubscriptionRequestResult>>> ApproveSubscriptionRequestAsync(ApproveSubscriptionRequestRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<ApproveSubscriptionRequestResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(ApproveSubscriptionRequestAsync)}",
            Content = response
        });
    }

    [Authorize(Roles = "Tenants")]
    [HttpPost("RejectSubscriptionRequest")]
    public async Task<ActionResult<ApiSuccessResult<RejectSubscriptionRequestResult>>> RejectSubscriptionRequestAsync(RejectSubscriptionRequestRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<RejectSubscriptionRequestResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(RejectSubscriptionRequestAsync)}",
            Content = response
        });
    }
}
