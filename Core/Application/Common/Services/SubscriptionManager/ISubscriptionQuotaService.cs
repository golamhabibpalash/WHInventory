namespace Application.Common.Services.SubscriptionManager;

/// <summary>
/// Enforces the quota limits of the current tenant's plan before a limited resource is created.
/// Each method is a no-op when the tenant has no subscription, no plan limit for that resource, or
/// is running at root scope; otherwise it throws a <see cref="FluentValidation.ValidationException"/>
/// (surfaced as 400) when the limit would be exceeded.
/// </summary>
public interface ISubscriptionQuotaService
{
    Task EnsureCanAddUserAsync(CancellationToken cancellationToken = default);

    Task EnsureCanAddWarehouseAsync(CancellationToken cancellationToken = default);

    Task EnsureCanAddProductAsync(CancellationToken cancellationToken = default);

    /// <summary>Current usage counts for the ambient tenant, for display on the billing page.</summary>
    Task<SubscriptionUsageDto> GetUsageAsync(CancellationToken cancellationToken = default);
}
