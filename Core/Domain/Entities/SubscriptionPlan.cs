using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// A plan in the platform-wide subscription catalogue. Like TicketCategory/TicketPriority this is a
/// global entity shared across every tenant (no tenant query filter): tenants read it to see what
/// they can subscribe to, but only platform administrators create or change it.
///
/// Quota limits use null to mean "unlimited". Stripe ids are only populated when a plan is mirrored
/// into Stripe; a purely manual installation leaves them null.
/// </summary>
public class SubscriptionPlan : BaseEntity
{
    public string? Name { get; set; }

    /// <summary>Stable machine code, unique across the catalogue, e.g. "FREE", "PRO".</summary>
    public string? Code { get; set; }

    public string? Description { get; set; }

    /// <summary>Recurring price per <see cref="BillingInterval"/>, in <see cref="Currency"/>.</summary>
    public double Price { get; set; }

    public string? Currency { get; set; } = "USD";

    public BillingInterval BillingInterval { get; set; } = BillingInterval.Monthly;

    /// <summary>Length of the free trial granted on first subscribe. Zero means no trial.</summary>
    public int TrialDays { get; set; }

    /// <summary>Maximum active users allowed. Null means unlimited.</summary>
    public int? MaxUsers { get; set; }

    /// <summary>Maximum real (non-system) warehouses allowed. Null means unlimited.</summary>
    public int? MaxWarehouses { get; set; }

    /// <summary>Maximum products allowed. Null means unlimited.</summary>
    public int? MaxProducts { get; set; }

    /// <summary>Available for new subscriptions. A retired plan is kept for existing subscribers.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Shown on the public pricing/sign-up surface.</summary>
    public bool IsPublic { get; set; } = true;

    public int SortOrder { get; set; }

    public string? StripeProductId { get; set; }

    public string? StripePriceId { get; set; }
}
