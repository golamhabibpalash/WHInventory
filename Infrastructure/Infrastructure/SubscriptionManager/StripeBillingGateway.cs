using Application.Common.CQS.Queries;
using Application.Common.Services.SubscriptionManager;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;

namespace Infrastructure.SubscriptionManager;

/// <summary>
/// Stripe-backed billing. Hosted Checkout starts a subscription; webhooks keep the tenant's
/// <c>TenantSubscription</c> in sync with Stripe's view. The tenant id travels on the Checkout
/// session's client_reference_id and on the subscription metadata so webhook events can be routed
/// back to the right tenant. Registered only when <c>Stripe:Enabled</c> is true and a secret key is
/// present; otherwise <see cref="NullBillingGateway"/> is used instead.
/// </summary>
public class StripeBillingGateway : IBillingGateway
{
    private const string TenantMetadataKey = "tenantId";

    private readonly IQueryContext _queryContext;
    private readonly ITenantSubscriptionAdminService _adminService;
    private readonly ILogger<StripeBillingGateway> _logger;
    private readonly string _secretKey;
    private readonly string _webhookSecret;

    public StripeBillingGateway(
        IConfiguration configuration,
        IQueryContext queryContext,
        ITenantSubscriptionAdminService adminService,
        ILogger<StripeBillingGateway> logger)
    {
        _queryContext = queryContext;
        _adminService = adminService;
        _logger = logger;
        _secretKey = configuration["Stripe:SecretKey"] ?? "";
        _webhookSecret = configuration["Stripe:WebhookSecret"] ?? "";
    }

    public bool IsEnabled => true;

    public async Task<string?> CreateCheckoutSessionUrlAsync(CheckoutRequest request, CancellationToken cancellationToken = default)
    {
        var priceId = await _queryContext.SubscriptionPlan.AsNoTracking()
            .Where(x => x.Id == request.PlanId && x.IsDeleted == false)
            .Select(x => x.StripePriceId)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(priceId))
        {
            _logger.LogWarning("Plan {PlanId} has no Stripe price id; cannot start checkout.", request.PlanId);
            return null;
        }

        var tenantMetadata = new Dictionary<string, string> { [TenantMetadataKey] = request.TenantId };

        var options = new SessionCreateOptions
        {
            Mode = "subscription",
            ClientReferenceId = request.TenantId,
            CustomerEmail = request.CustomerEmail,
            SuccessUrl = request.SuccessUrl,
            CancelUrl = request.CancelUrl,
            LineItems = new List<SessionLineItemOptions>
            {
                new() { Price = priceId, Quantity = 1 }
            },
            Metadata = tenantMetadata,
            SubscriptionData = new SessionSubscriptionDataOptions { Metadata = tenantMetadata }
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options, cancellationToken: cancellationToken);
        return session.Url;
    }

    public async Task HandleWebhookAsync(string json, string signatureHeader, CancellationToken cancellationToken = default)
    {
        StripeConfiguration.ApiKey = _secretKey;

        // Throws StripeException on a bad signature — surfaced as a 400 by the webhook controller.
        var stripeEvent = EventUtility.ConstructEvent(json, signatureHeader, _webhookSecret);

        switch (stripeEvent.Type)
        {
            case "checkout.session.completed":
                if (stripeEvent.Data.Object is Session session)
                {
                    await HandleCheckoutCompletedAsync(session, cancellationToken);
                }
                break;

            case "customer.subscription.created":
            case "customer.subscription.updated":
            case "customer.subscription.deleted":
                if (stripeEvent.Data.Object is Subscription subscription)
                {
                    await SyncSubscriptionAsync(subscription, cancellationToken);
                }
                break;

            default:
                _logger.LogDebug("Ignoring unhandled Stripe event type {Type}.", stripeEvent.Type);
                break;
        }
    }

    private async Task HandleCheckoutCompletedAsync(Session session, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(session.SubscriptionId))
        {
            return;
        }

        var subscription = await new SubscriptionService()
            .GetAsync(session.SubscriptionId, cancellationToken: cancellationToken);

        // Checkout carries the tenant on client_reference_id; copy it onto the subscription so later
        // subscription.* events can route without the session.
        if (subscription.Metadata == null || !subscription.Metadata.ContainsKey(TenantMetadataKey))
        {
            subscription.Metadata ??= new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(session.ClientReferenceId))
            {
                subscription.Metadata[TenantMetadataKey] = session.ClientReferenceId;
            }
        }

        await SyncSubscriptionAsync(subscription, cancellationToken);
    }

    private async Task SyncSubscriptionAsync(Subscription subscription, CancellationToken cancellationToken)
    {
        var tenantId = subscription.Metadata != null && subscription.Metadata.TryGetValue(TenantMetadataKey, out var id)
            ? id
            : null;

        if (string.IsNullOrEmpty(tenantId))
        {
            _logger.LogWarning("Stripe subscription {SubscriptionId} has no tenant metadata; cannot sync.", subscription.Id);
            return;
        }

        var item = subscription.Items?.Data?.FirstOrDefault();
        var priceId = item?.Price?.Id;

        await _adminService.SyncFromProviderAsync(
            tenantId,
            priceId,
            MapStatus(subscription.Status),
            SubscriptionProvider.Stripe,
            subscription.CustomerId,
            subscription.Id,
            item?.CurrentPeriodEnd,
            cancellationToken);
    }

    private static SubscriptionStatus MapStatus(string stripeStatus)
    {
        return stripeStatus switch
        {
            "trialing" => SubscriptionStatus.Trialing,
            "active" => SubscriptionStatus.Active,
            "past_due" => SubscriptionStatus.PastDue,
            "unpaid" => SubscriptionStatus.PastDue,
            "incomplete" => SubscriptionStatus.PastDue,
            "paused" => SubscriptionStatus.Suspended,
            "canceled" => SubscriptionStatus.Canceled,
            "incomplete_expired" => SubscriptionStatus.Canceled,
            _ => SubscriptionStatus.PastDue
        };
    }
}
