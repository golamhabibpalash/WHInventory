namespace Application.Common.Services.SubscriptionManager;

/// <summary>
/// Marks a MediatR request that must run even when the tenant is in read-only mode. Billing and
/// account commands implement it so a lapsed tenant can still change plan, renew, or sign out —
/// without it, the read-only enforcement would lock the tenant out of the very actions that fix the
/// lapse. Query requests are never blocked, so they do not need this.
/// </summary>
public interface ISubscriptionExempt
{
}
