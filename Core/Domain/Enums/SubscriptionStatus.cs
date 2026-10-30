using System.ComponentModel;

namespace Domain.Enums;

/// <summary>
/// Lifecycle of a tenant's subscription. Only <see cref="Trialing"/> and <see cref="Active"/>
/// grant full access; every other state puts the tenant into read-only mode.
/// </summary>
public enum SubscriptionStatus
{
    /// <summary>In a trial period — full access until the trial ends.</summary>
    [Description("Trialing")]
    Trialing = 0,

    /// <summary>Paid and current — full access.</summary>
    [Description("Active")]
    Active = 1,

    /// <summary>A payment is overdue. Read-only until settled.</summary>
    [Description("Past Due")]
    PastDue = 2,

    /// <summary>Suspended by a platform administrator. Read-only.</summary>
    [Description("Suspended")]
    Suspended = 3,

    /// <summary>Cancelled by the tenant or platform. Read-only once the paid period ends.</summary>
    [Description("Canceled")]
    Canceled = 4,

    /// <summary>The paid/trial period has elapsed with no renewal. Read-only.</summary>
    [Description("Expired")]
    Expired = 5
}
