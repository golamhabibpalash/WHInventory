namespace Application.Common.Services.SubscriptionManager;

/// <summary>
/// Abstraction over an external payment provider (Stripe). The manual subscription model is always
/// the source of truth; a gateway, when enabled, drives the same <c>TenantSubscription</c> fields
/// from hosted checkout and webhooks. When billing runs in manual-only mode a no-op implementation
/// is registered and <see cref="IsEnabled"/> is false.
/// </summary>
public interface IBillingGateway
{
    bool IsEnabled { get; }

    /// <summary>
    /// Starts a hosted checkout for a tenant to subscribe to a plan and returns the URL to redirect
    /// them to. Returns null when the gateway is disabled or the plan is not mapped to the provider.
    /// </summary>
    Task<string?> CreateCheckoutSessionUrlAsync(CheckoutRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies and processes a provider webhook payload, syncing the affected tenant's subscription.
    /// Throws when the signature is invalid.
    /// </summary>
    Task HandleWebhookAsync(string json, string signatureHeader, CancellationToken cancellationToken = default);
}

public class CheckoutRequest
{
    public string TenantId { get; init; } = "";
    public string PlanId { get; init; } = "";
    public string? CustomerEmail { get; init; }
    public string SuccessUrl { get; init; } = "";
    public string CancelUrl { get; init; } = "";
}
