using Domain.Enums;

namespace Application.Common.Services.SubscriptionManager;

/// <summary>Flat view of a tenant's current subscription, shared by the guard, the admin service
/// and the read models.</summary>
public class TenantSubscriptionDto
{
    public string? Id { get; init; }
    public string? TenantId { get; init; }
    public string? TenantName { get; init; }
    public string? TenantSlug { get; init; }
    public string? PlanId { get; init; }
    public string? PlanName { get; init; }
    public string? PlanCode { get; init; }
    public SubscriptionStatus Status { get; init; }
    public BillingInterval BillingInterval { get; init; }
    public SubscriptionProvider Provider { get; init; }
    public DateTime? StartedAtUtc { get; init; }
    public DateTime? CurrentPeriodStartUtc { get; init; }
    public DateTime? CurrentPeriodEndUtc { get; init; }
    public DateTime? TrialEndsAtUtc { get; init; }
    public DateTime? CanceledAtUtc { get; init; }
    public int? MaxUsers { get; init; }
    public int? MaxWarehouses { get; init; }
    public int? MaxProducts { get; init; }
    public string? StripeCustomerId { get; init; }
    public string? StripeSubscriptionId { get; init; }
    public string? Notes { get; init; }

    /// <summary>True when the subscription grants full read-write access right now.</summary>
    public bool IsEntitled { get; init; }
}
