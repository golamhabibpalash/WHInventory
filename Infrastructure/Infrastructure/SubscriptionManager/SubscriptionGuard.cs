using Application.Common.CQS.Queries;
using Application.Common.Services.SubscriptionManager;
using Application.Common.Tenancy;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.SubscriptionManager;

/// <summary>
/// Reads the current tenant's subscription under the ambient tenant scope (the tenant query filter
/// returns only its own row) and decides read-only status. Scoped: the lookup is cached for the
/// request so the enforcement behaviour can run on every command without re-querying.
/// </summary>
public class SubscriptionGuard : ISubscriptionGuard
{
    private readonly IQueryContext _queryContext;
    private readonly ITenantContext _tenantContext;

    private bool _loaded;
    private TenantSubscriptionDto? _cached;

    public SubscriptionGuard(IQueryContext queryContext, ITenantContext tenantContext)
    {
        _queryContext = queryContext;
        _tenantContext = tenantContext;
    }

    public async Task<bool> IsReadOnlyAsync(CancellationToken cancellationToken = default)
    {
        var current = await GetCurrentAsync(cancellationToken);

        // No subscription means the tenant is not enrolled in billing yet — never read-only, so an
        // install that predates this feature is not bricked before plans are assigned.
        return current != null && !current.IsEntitled;
    }

    public async Task<TenantSubscriptionDto?> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        if (_loaded)
        {
            return _cached;
        }

        // Platform/root scope is never subject to tenant billing, and the tenant filter would return
        // every tenant's row there anyway — short-circuit before querying.
        if (_tenantContext.IsRoot || string.IsNullOrEmpty(_tenantContext.TenantId))
        {
            _loaded = true;
            _cached = null;
            return null;
        }

        var entity = await _queryContext.TenantSubscription
            .AsNoTracking()
            .Where(x => x.IsDeleted == false)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        _cached = entity == null ? null : Map(entity);
        _loaded = true;
        return _cached;
    }

    internal static TenantSubscriptionDto Map(TenantSubscription entity)
    {
        return new TenantSubscriptionDto
        {
            Id = entity.Id,
            TenantId = entity.TenantId,
            PlanId = entity.PlanId,
            PlanName = entity.PlanName,
            PlanCode = entity.PlanCode,
            Status = entity.Status,
            BillingInterval = entity.BillingInterval,
            Provider = entity.Provider,
            StartedAtUtc = entity.StartedAtUtc,
            CurrentPeriodStartUtc = entity.CurrentPeriodStartUtc,
            CurrentPeriodEndUtc = entity.CurrentPeriodEndUtc,
            TrialEndsAtUtc = entity.TrialEndsAtUtc,
            CanceledAtUtc = entity.CanceledAtUtc,
            MaxUsers = entity.MaxUsers,
            MaxWarehouses = entity.MaxWarehouses,
            MaxProducts = entity.MaxProducts,
            StripeCustomerId = entity.StripeCustomerId,
            StripeSubscriptionId = entity.StripeSubscriptionId,
            Notes = entity.Notes,
            IsEntitled = entity.IsEntitled(DateTime.UtcNow)
        };
    }
}
