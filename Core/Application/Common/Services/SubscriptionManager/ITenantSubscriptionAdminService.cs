using Domain.Enums;

namespace Application.Common.Services.SubscriptionManager;

/// <summary>
/// Platform-side, cross-tenant subscription administration. Implementations elevate to root scope
/// internally (mirroring TenantProvisioningService) so a platform administrator — who operates
/// under their own tenant scope — can read and write any tenant's subscription. Not for tenant
/// self-service, which reads its own row under the normal tenant filter.
/// </summary>
public interface ITenantSubscriptionAdminService
{
    /// <summary>Every tenant's current subscription, newest first. Tenants without a subscription
    /// are omitted; the caller can reconcile against the tenant registry.</summary>
    Task<List<TenantSubscriptionDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<TenantSubscriptionDto?> GetForTenantAsync(string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns (or re-assigns) a plan to a tenant, snapshotting the plan's limits and starting a new
    /// billing period. Creates the subscription row if the tenant has none. Returns the saved state.
    /// </summary>
    Task<TenantSubscriptionDto> AssignPlanAsync(
        string tenantId,
        string planId,
        SubscriptionStatus status,
        DateTime? periodEndUtc,
        string? actedById,
        CancellationToken cancellationToken = default);

    Task<TenantSubscriptionDto> ChangeStatusAsync(
        string tenantId,
        SubscriptionStatus status,
        DateTime? periodEndUtc,
        string? actedById,
        CancellationToken cancellationToken = default);

    Task<TenantSubscriptionDto> CancelAsync(
        string tenantId,
        string? actedById,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns a plan identified by its code (e.g. the sign-up plan for a freshly provisioned
    /// tenant). A no-op returning null when no plan with that code exists, so provisioning does not
    /// fail on an installation whose catalogue has not been seeded.
    /// </summary>
    Task<TenantSubscriptionDto?> AssignPlanByCodeAsync(
        string tenantId,
        string planCode,
        SubscriptionStatus status,
        string? actedById,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures every active tenant has a subscription, assigning the configured default plan with an
    /// active status to any that lack one. Idempotent; used at startup so an existing install is not
    /// bricked by read-only enforcement the moment the feature ships.
    /// </summary>
    Task BackfillMissingAsync(string defaultPlanCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies the state received from a payment-provider webhook to a tenant's subscription: marks
    /// it provider-driven, stores the provider ids and period end, and snapshots the plan matched by
    /// <paramref name="stripePriceId"/> when one is given. Creates the row if the tenant has none.
    /// </summary>
    Task SyncFromProviderAsync(
        string tenantId,
        string? stripePriceId,
        SubscriptionStatus status,
        SubscriptionProvider provider,
        string? providerCustomerId,
        string? providerSubscriptionId,
        DateTime? currentPeriodEndUtc,
        CancellationToken cancellationToken = default);
}
