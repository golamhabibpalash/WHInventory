using Application.Features.TenantSubscriptionManager.Commands;
using Application.Features.TenantSubscriptionManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

/// <summary>
/// A tenant's own subscription (GetMySubscription, open to any authenticated user) plus platform
/// administration of every tenant's subscription (gated on the Tenants role, which provisioning
/// withholds from tenant administrators).
/// </summary>
[Route("api/[controller]")]
public class SubscriptionController : BaseApiController
{
    public SubscriptionController(ISender sender) : base(sender)
    {
    }

    [Authorize]
    [HttpGet("GetMySubscription")]
    public async Task<ActionResult<ApiSuccessResult<GetMySubscriptionResult>>> GetMySubscriptionAsync(CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new GetMySubscriptionRequest(), cancellationToken);

        return Ok(new ApiSuccessResult<GetMySubscriptionResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetMySubscriptionAsync)}",
            Content = response
        });
    }

    [Authorize(Roles = "Tenants")]
    [HttpGet("GetTenantSubscriptionList")]
    public async Task<ActionResult<ApiSuccessResult<GetTenantSubscriptionListResult>>> GetTenantSubscriptionListAsync(CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new GetTenantSubscriptionListRequest(), cancellationToken);

        return Ok(new ApiSuccessResult<GetTenantSubscriptionListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetTenantSubscriptionListAsync)}",
            Content = response
        });
    }

    [Authorize(Roles = "Tenants")]
    [HttpPost("AssignSubscriptionPlan")]
    public async Task<ActionResult<ApiSuccessResult<AssignSubscriptionPlanResult>>> AssignSubscriptionPlanAsync(AssignSubscriptionPlanRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<AssignSubscriptionPlanResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(AssignSubscriptionPlanAsync)}",
            Content = response
        });
    }

    [Authorize(Roles = "Tenants")]
    [HttpPost("ChangeSubscriptionStatus")]
    public async Task<ActionResult<ApiSuccessResult<ChangeSubscriptionStatusResult>>> ChangeSubscriptionStatusAsync(ChangeSubscriptionStatusRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<ChangeSubscriptionStatusResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(ChangeSubscriptionStatusAsync)}",
            Content = response
        });
    }

    [Authorize(Roles = "Tenants")]
    [HttpPost("CancelSubscription")]
    public async Task<ActionResult<ApiSuccessResult<CancelSubscriptionResult>>> CancelSubscriptionAsync(CancelSubscriptionRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<CancelSubscriptionResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(CancelSubscriptionAsync)}",
            Content = response
        });
    }
}
