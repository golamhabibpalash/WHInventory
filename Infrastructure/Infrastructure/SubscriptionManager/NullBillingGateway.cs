using Application.Common.Services.SubscriptionManager;

namespace Infrastructure.SubscriptionManager;

/// <summary>
/// The gateway used when no payment provider is configured: billing is manual-only. Checkout is
/// unavailable and no webhooks are expected, so a stray call is a configuration error.
/// </summary>
public class NullBillingGateway : IBillingGateway
{
    public bool IsEnabled => false;

    public Task<string?> CreateCheckoutSessionUrlAsync(CheckoutRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<string?>(null);
    }

    public Task HandleWebhookAsync(string json, string signatureHeader, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("No payment provider is configured, so webhooks cannot be processed.");
    }
}
