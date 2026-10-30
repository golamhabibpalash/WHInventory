using Application.Common.Services.SubscriptionManager;
using MediatR;

namespace Application.Common.Behaviors;

/// <summary>
/// Puts a tenant whose subscription has lapsed into read-only mode: any mutating command is rejected
/// with <see cref="SubscriptionInactiveException"/> (HTTP 402) before it reaches its handler, while
/// queries still run so the tenant can view data and reach the billing page.
///
/// "Mutating" is inferred from the CQRS convention — requests under a <c>*.Commands</c> namespace —
/// so no per-command annotation is needed. Commands that must survive a lapse (billing, account,
/// sign-out) opt out with <see cref="ISubscriptionExempt"/>. The guard itself exempts root scope and
/// tenants that have no subscription row.
/// </summary>
public class SubscriptionEnforcementBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ISubscriptionGuard _guard;

    public SubscriptionEnforcementBehaviour(ISubscriptionGuard guard)
    {
        _guard = guard;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (IsMutatingCommand(request) && request is not ISubscriptionExempt)
        {
            if (await _guard.IsReadOnlyAsync(cancellationToken))
            {
                throw new SubscriptionInactiveException(
                    "Your subscription is not active, so your workspace is read-only. " +
                    "Renew or change your plan from the Billing page to make changes again.");
            }
        }

        return await next();
    }

    private static bool IsMutatingCommand(TRequest request)
    {
        var ns = request.GetType().Namespace;
        return ns != null && ns.Contains(".Commands", StringComparison.Ordinal);
    }
}
