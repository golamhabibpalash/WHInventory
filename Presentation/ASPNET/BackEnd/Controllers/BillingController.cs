using Application.Common.Services.CurrentUserManager;
using Application.Common.Services.SubscriptionManager;
using Application.Common.Tenancy;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

public class CreateCheckoutSessionRequestDto
{
    public string? PlanId { get; init; }
}

public class CreateCheckoutSessionResponse
{
    public string? Url { get; init; }
}

/// <summary>
/// Payment-provider integration surface: a tenant starts hosted checkout, and the provider posts
/// webhooks back. Both bypass MediatR (and therefore read-only enforcement) deliberately — a lapsed
/// tenant must still be able to pay. The webhook is anonymous and authenticates by signature.
/// </summary>
[Route("api/[controller]")]
public class BillingController : BaseApiController
{
    private readonly IBillingGateway _gateway;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService _currentUser;

    public BillingController(
        ISender sender,
        IBillingGateway gateway,
        ITenantContext tenantContext,
        ICurrentUserService currentUser) : base(sender)
    {
        _gateway = gateway;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
    }

    [Authorize]
    [HttpPost("CreateCheckoutSession")]
    public async Task<ActionResult<ApiSuccessResult<CreateCheckoutSessionResponse>>> CreateCheckoutSessionAsync(
        CreateCheckoutSessionRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!_gateway.IsEnabled)
        {
            return BadRequest(new ApiErrorResult
            {
                Code = StatusCodes.Status400BadRequest,
                Message = "Online checkout is not available on this installation."
            });
        }

        var tenantId = _tenantContext.TenantId;
        if (string.IsNullOrEmpty(tenantId))
        {
            return BadRequest(new ApiErrorResult
            {
                Code = StatusCodes.Status400BadRequest,
                Message = "No tenant resolved for the current request."
            });
        }

        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        var url = await _gateway.CreateCheckoutSessionUrlAsync(new CheckoutRequest
        {
            TenantId = tenantId,
            PlanId = request.PlanId ?? "",
            CustomerEmail = _currentUser.Email,
            SuccessUrl = $"{baseUrl}/?checkout=success",
            CancelUrl = $"{baseUrl}/?checkout=cancel"
        }, cancellationToken);

        if (string.IsNullOrEmpty(url))
        {
            return BadRequest(new ApiErrorResult
            {
                Code = StatusCodes.Status400BadRequest,
                Message = "The selected plan is not available for online checkout."
            });
        }

        return Ok(new ApiSuccessResult<CreateCheckoutSessionResponse>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(CreateCheckoutSessionAsync)}",
            Content = new CreateCheckoutSessionResponse { Url = url }
        });
    }

    [AllowAnonymous]
    [HttpPost("StripeWebhook")]
    public async Task<IActionResult> StripeWebhookAsync(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var json = await reader.ReadToEndAsync(cancellationToken);
        var signature = Request.Headers["Stripe-Signature"].ToString();

        await _gateway.HandleWebhookAsync(json, signature, cancellationToken);
        return Ok();
    }
}
