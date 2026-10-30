using System.ComponentModel;

namespace Domain.Enums;

/// <summary>
/// Who owns the truth for a subscription's billing state. <see cref="Manual"/> subscriptions are
/// driven entirely from the platform console; <see cref="Stripe"/> subscriptions are kept in sync
/// from Stripe webhooks.
/// </summary>
public enum SubscriptionProvider
{
    [Description("Manual")]
    Manual = 0,

    [Description("Stripe")]
    Stripe = 1
}
