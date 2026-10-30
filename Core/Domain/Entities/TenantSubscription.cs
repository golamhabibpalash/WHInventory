using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// A tenant's current subscription: the link between a tenant and a <see cref="SubscriptionPlan"/>
/// plus its billing lifecycle. One live row per tenant, updated in place as the plan or status
/// changes. Tenant-scoped like any other entity, so a tenant sees only its own row while platform
/// (root) scope sees them all.
///
/// Plan fields are snapshotted (<see cref="PlanName"/>, <see cref="PlanCode"/> and the Max* limits)
/// so a later edit to the catalogue plan does not silently change what a tenant is entitled to until
/// the subscription is explicitly re-synced.
/// </summary>
public class TenantSubscription : BaseEntity
{
    public string? PlanId { get; set; }

    public string? PlanName { get; set; }

    public string? PlanCode { get; set; }

    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Trialing;

    public BillingInterval BillingInterval { get; set; } = BillingInterval.Monthly;

    public SubscriptionProvider Provider { get; set; } = SubscriptionProvider.Manual;

    /// <summary>When the subscription first began.</summary>
    public DateTime? StartedAtUtc { get; set; }

    public DateTime? CurrentPeriodStartUtc { get; set; }

    /// <summary>
    /// End of the paid/trial period. Once this is in the past the subscription is treated as lapsed
    /// (read-only) regardless of <see cref="Status"/>, unless the plan is free with no end date.
    /// </summary>
    public DateTime? CurrentPeriodEndUtc { get; set; }

    public DateTime? TrialEndsAtUtc { get; set; }

    public DateTime? CanceledAtUtc { get; set; }

    // Entitlement snapshot — copied from the plan when assigned/synced. Null means unlimited.
    public int? MaxUsers { get; set; }

    public int? MaxWarehouses { get; set; }

    public int? MaxProducts { get; set; }

    public string? StripeCustomerId { get; set; }

    public string? StripeSubscriptionId { get; set; }

    public string? Notes { get; set; }

    /// <summary>
    /// True when the tenant should have full (read-write) access: an active or trialing status whose
    /// period has not elapsed. Everything else is read-only. A null period end counts as open-ended
    /// (free plans), so it never expires on time alone.
    /// </summary>
    public bool IsEntitled(DateTime utcNow)
    {
        if (Status != SubscriptionStatus.Active && Status != SubscriptionStatus.Trialing)
        {
            return false;
        }

        return CurrentPeriodEndUtc == null || CurrentPeriodEndUtc.Value >= utcNow;
    }
}
