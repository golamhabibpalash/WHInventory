namespace Application.Common.Services.SubscriptionManager;

/// <summary>
/// Answers "may the current tenant make changes right now?" for the ambient request scope. Scoped
/// so it can cache the lookup for the lifetime of a single HTTP request (a request fans out into
/// many MediatR sends).
/// </summary>
public interface ISubscriptionGuard
{
    /// <summary>
    /// True when the current tenant is restricted to read-only because its subscription has lapsed.
    /// Root (platform) scope and tenants with no subscription row are never read-only.
    /// </summary>
    Task<bool> IsReadOnlyAsync(CancellationToken cancellationToken = default);

    /// <summary>The current tenant's subscription, or null when none exists or at root scope.</summary>
    Task<TenantSubscriptionDto?> GetCurrentAsync(CancellationToken cancellationToken = default);
}
