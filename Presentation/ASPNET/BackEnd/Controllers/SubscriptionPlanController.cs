using Application.Features.SubscriptionPlanManager.Commands;
using Application.Features.SubscriptionPlanManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

/// <summary>
/// The subscription plan catalogue. Reading is open to any authenticated user (tenants need to see
/// what they can subscribe to); creating and changing plans is platform administration and is gated
/// on the Tenants role, which provisioning withholds from tenant administrators.
/// </summary>
[Route("api/[controller]")]
public class SubscriptionPlanController : BaseApiController
{
    public SubscriptionPlanController(ISender sender) : base(sender)
    {
    }

    [Authorize]
    [HttpGet("GetSubscriptionPlanList")]
    public async Task<ActionResult<ApiSuccessResult<GetSubscriptionPlanListResult>>> GetSubscriptionPlanListAsync(
        CancellationToken cancellationToken,
        [FromQuery] bool isDeleted = false,
        [FromQuery] bool activeOnly = false,
        [FromQuery] bool publicOnly = false)
    {
        var request = new GetSubscriptionPlanListRequest { IsDeleted = isDeleted, ActiveOnly = activeOnly, PublicOnly = publicOnly };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetSubscriptionPlanListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetSubscriptionPlanListAsync)}",
            Content = response
        });
    }

    [Authorize(Roles = "Tenants")]
    [HttpPost("CreateSubscriptionPlan")]
    public async Task<ActionResult<ApiSuccessResult<CreateSubscriptionPlanResult>>> CreateSubscriptionPlanAsync(CreateSubscriptionPlanRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<CreateSubscriptionPlanResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(CreateSubscriptionPlanAsync)}",
            Content = response
        });
    }

    [Authorize(Roles = "Tenants")]
    [HttpPost("UpdateSubscriptionPlan")]
    public async Task<ActionResult<ApiSuccessResult<UpdateSubscriptionPlanResult>>> UpdateSubscriptionPlanAsync(UpdateSubscriptionPlanRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<UpdateSubscriptionPlanResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(UpdateSubscriptionPlanAsync)}",
            Content = response
        });
    }

    [Authorize(Roles = "Tenants")]
    [HttpPost("DeleteSubscriptionPlan")]
    public async Task<ActionResult<ApiSuccessResult<DeleteSubscriptionPlanResult>>> DeleteSubscriptionPlanAsync(DeleteSubscriptionPlanRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<DeleteSubscriptionPlanResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(DeleteSubscriptionPlanAsync)}",
            Content = response
        });
    }
}
