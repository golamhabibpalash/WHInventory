using Application.Common.CQS.Queries;
using Application.Common.Repositories;
using Application.Common.Services.SubscriptionManager;
using Application.Common.Tenancy;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.SubscriptionManager;

/// <summary>
/// Cross-tenant subscription administration. A platform administrator operates under their own
/// tenant scope, so every method here temporarily elevates to root (as TenantProvisioningService
/// does) to read and write any tenant's row, then restores the caller's scope. Writes set TenantId
/// explicitly because the automatic stamping does nothing at root scope.
/// </summary>
public class TenantSubscriptionAdminService : ITenantSubscriptionAdminService
{
    private readonly ITenantContext _tenantContext;
    private readonly IQueryContext _queryContext;
    private readonly ICommandRepository<TenantSubscription> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public TenantSubscriptionAdminService(
        ITenantContext tenantContext,
        IQueryContext queryContext,
        ICommandRepository<TenantSubscription> repository,
        IUnitOfWork unitOfWork)
    {
        _tenantContext = tenantContext;
        _queryContext = queryContext;
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<TenantSubscriptionDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await RunAtRootAsync(async () =>
        {
            var subscriptions = await _queryContext.TenantSubscription
                .AsNoTracking()
                .Where(x => x.IsDeleted == false)
                .ToListAsync(cancellationToken);

            var tenants = await _queryContext.Tenant
                .AsNoTracking()
                .Where(x => x.IsDeleted == false)
                .ToDictionaryAsync(x => x.Id, x => x, cancellationToken);

            return subscriptions
                .OrderByDescending(x => x.CreatedAtUtc)
                .Select(x => Map(x, x.TenantId != null && tenants.TryGetValue(x.TenantId, out var t) ? t : null))
                .ToList();
        });
    }

    public async Task<TenantSubscriptionDto?> GetForTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        return await RunAtRootAsync(async () =>
        {
            var entity = await FindCurrentAsync(tenantId, cancellationToken);
            if (entity == null) return null;

            var tenant = await _queryContext.Tenant.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == tenantId, cancellationToken);

            return Map(entity, tenant);
        });
    }

    public async Task<TenantSubscriptionDto> AssignPlanAsync(
        string tenantId,
        string planId,
        SubscriptionStatus status,
        DateTime? periodEndUtc,
        string? actedById,
        CancellationToken cancellationToken = default)
    {
        return await RunAtRootAsync(async () =>
        {
            var plan = await _queryContext.SubscriptionPlan.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == planId && x.IsDeleted == false, cancellationToken)
                ?? throw new ValidationException($"Subscription plan not found: {planId}");

            var tenant = await _queryContext.Tenant.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == tenantId && x.IsDeleted == false, cancellationToken)
                ?? throw new ValidationException($"Tenant not found: {tenantId}");

            var now = DateTime.UtcNow;
            var entity = await FindCurrentTrackedAsync(tenantId, cancellationToken);
            var isNew = entity == null;

            if (entity == null)
            {
                entity = new TenantSubscription { TenantId = tenantId };
            }

            entity.PlanId = plan.Id;
            entity.PlanName = plan.Name;
            entity.PlanCode = plan.Code;
            entity.BillingInterval = plan.BillingInterval;
            entity.MaxUsers = plan.MaxUsers;
            entity.MaxWarehouses = plan.MaxWarehouses;
            entity.MaxProducts = plan.MaxProducts;
            entity.Provider = SubscriptionProvider.Manual;
            entity.Status = status;
            entity.StartedAtUtc ??= now;
            entity.CurrentPeriodStartUtc = now;
            entity.CurrentPeriodEndUtc = periodEndUtc ?? ComputePeriodEnd(now, status, plan);
            entity.TrialEndsAtUtc = status == SubscriptionStatus.Trialing && plan.TrialDays > 0
                ? now.AddDays(plan.TrialDays)
                : entity.TrialEndsAtUtc;
            entity.CanceledAtUtc = null;

            if (isNew)
            {
                entity.CreatedById = actedById;
                await _repository.CreateAsync(entity, cancellationToken);
            }
            else
            {
                entity.UpdatedById = actedById;
                _repository.Update(entity);
            }

            await _unitOfWork.SaveAsync(cancellationToken);
            return Map(entity, tenant);
        });
    }

    public async Task<TenantSubscriptionDto> ChangeStatusAsync(
        string tenantId,
        SubscriptionStatus status,
        DateTime? periodEndUtc,
        string? actedById,
        CancellationToken cancellationToken = default)
    {
        return await RunAtRootAsync(async () =>
        {
            var entity = await FindCurrentTrackedAsync(tenantId, cancellationToken)
                ?? throw new ValidationException($"Tenant has no subscription to change: {tenantId}");

            entity.Status = status;
            if (periodEndUtc != null)
            {
                entity.CurrentPeriodEndUtc = periodEndUtc;
            }
            if (status == SubscriptionStatus.Canceled)
            {
                entity.CanceledAtUtc = DateTime.UtcNow;
            }
            entity.UpdatedById = actedById;

            _repository.Update(entity);
            await _unitOfWork.SaveAsync(cancellationToken);

            var tenant = await _queryContext.Tenant.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == tenantId, cancellationToken);
            return Map(entity, tenant);
        });
    }

    public async Task<TenantSubscriptionDto> CancelAsync(
        string tenantId,
        string? actedById,
        CancellationToken cancellationToken = default)
    {
        return await ChangeStatusAsync(tenantId, SubscriptionStatus.Canceled, null, actedById, cancellationToken);
    }

    public async Task<TenantSubscriptionDto?> AssignPlanByCodeAsync(
        string tenantId,
        string planCode,
        SubscriptionStatus status,
        string? actedById,
        CancellationToken cancellationToken = default)
    {
        var planId = await RunAtRootAsync(async () =>
            await _queryContext.SubscriptionPlan.AsNoTracking()
                .Where(x => x.Code == planCode && x.IsDeleted == false)
                .Select(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken));

        if (string.IsNullOrEmpty(planId))
        {
            return null;
        }

        return await AssignPlanAsync(tenantId, planId, status, null, actedById, cancellationToken);
    }

    public async Task BackfillMissingAsync(string defaultPlanCode, CancellationToken cancellationToken = default)
    {
        await RunAtRootAsync(async () =>
        {
            var plan = await _queryContext.SubscriptionPlan.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Code == defaultPlanCode && x.IsDeleted == false, cancellationToken);
            if (plan == null) return true;

            var tenants = await _queryContext.Tenant.AsNoTracking()
                .Where(x => x.IsDeleted == false && x.IsActive)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);

            var alreadyEnrolled = await _queryContext.TenantSubscription.AsNoTracking()
                .Where(x => x.IsDeleted == false && x.TenantId != null)
                .Select(x => x.TenantId!)
                .ToListAsync(cancellationToken);

            var enrolledSet = alreadyEnrolled.ToHashSet();
            var now = DateTime.UtcNow;
            var created = 0;

            foreach (var tenantId in tenants.Where(id => !enrolledSet.Contains(id)))
            {
                var entity = new TenantSubscription
                {
                    TenantId = tenantId,
                    PlanId = plan.Id,
                    PlanName = plan.Name,
                    PlanCode = plan.Code,
                    BillingInterval = plan.BillingInterval,
                    MaxUsers = plan.MaxUsers,
                    MaxWarehouses = plan.MaxWarehouses,
                    MaxProducts = plan.MaxProducts,
                    Provider = SubscriptionProvider.Manual,
                    Status = SubscriptionStatus.Active,
                    StartedAtUtc = now,
                    CurrentPeriodStartUtc = now,
                    CurrentPeriodEndUtc = ComputePeriodEnd(now, SubscriptionStatus.Active, plan)
                };

                await _repository.CreateAsync(entity, cancellationToken);
                created++;
            }

            if (created > 0)
            {
                await _unitOfWork.SaveAsync(cancellationToken);
            }

            return true;
        });
    }

    public async Task SyncFromProviderAsync(
        string tenantId,
        string? stripePriceId,
        SubscriptionStatus status,
        SubscriptionProvider provider,
        string? providerCustomerId,
        string? providerSubscriptionId,
        DateTime? currentPeriodEndUtc,
        CancellationToken cancellationToken = default)
    {
        await RunAtRootAsync(async () =>
        {
            var plan = stripePriceId == null
                ? null
                : await _queryContext.SubscriptionPlan.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.StripePriceId == stripePriceId && x.IsDeleted == false, cancellationToken);

            var now = DateTime.UtcNow;
            var entity = await FindCurrentTrackedAsync(tenantId, cancellationToken);
            var isNew = entity == null;

            if (entity == null)
            {
                entity = new TenantSubscription { TenantId = tenantId, StartedAtUtc = now };
            }

            if (plan != null)
            {
                entity.PlanId = plan.Id;
                entity.PlanName = plan.Name;
                entity.PlanCode = plan.Code;
                entity.BillingInterval = plan.BillingInterval;
                entity.MaxUsers = plan.MaxUsers;
                entity.MaxWarehouses = plan.MaxWarehouses;
                entity.MaxProducts = plan.MaxProducts;
            }

            entity.Provider = provider;
            entity.Status = status;
            entity.StripeCustomerId = providerCustomerId ?? entity.StripeCustomerId;
            entity.StripeSubscriptionId = providerSubscriptionId ?? entity.StripeSubscriptionId;
            entity.CurrentPeriodEndUtc = currentPeriodEndUtc ?? entity.CurrentPeriodEndUtc;
            if (status == SubscriptionStatus.Canceled)
            {
                entity.CanceledAtUtc = now;
            }

            if (isNew)
            {
                await _repository.CreateAsync(entity, cancellationToken);
            }
            else
            {
                _repository.Update(entity);
            }

            await _unitOfWork.SaveAsync(cancellationToken);
            return true;
        });
    }

    // A free plan (no price) is open-ended — no period end, so it never lapses on time alone.
    private static DateTime? ComputePeriodEnd(DateTime now, SubscriptionStatus status, SubscriptionPlan plan)
    {
        if (status == SubscriptionStatus.Trialing && plan.TrialDays > 0)
        {
            return now.AddDays(plan.TrialDays);
        }

        if (plan.Price <= 0)
        {
            return null;
        }

        return plan.BillingInterval == BillingInterval.Yearly ? now.AddYears(1) : now.AddMonths(1);
    }

    private async Task<TenantSubscription?> FindCurrentAsync(string tenantId, CancellationToken cancellationToken)
    {
        return await _queryContext.TenantSubscription.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IsDeleted == false)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<TenantSubscription?> FindCurrentTrackedAsync(string tenantId, CancellationToken cancellationToken)
    {
        return await _repository.GetQuery()
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<T> RunAtRootAsync<T>(Func<Task<T>> action)
    {
        var callerTenantId = _tenantContext.TenantId;
        var callerWasRoot = _tenantContext.IsRoot;

        try
        {
            _tenantContext.SetRootScope();
            return await action();
        }
        finally
        {
            if (callerWasRoot)
            {
                _tenantContext.SetRootScope();
            }
            else
            {
                _tenantContext.SetTenant(callerTenantId);
            }
        }
    }

    private static TenantSubscriptionDto Map(TenantSubscription entity, Tenant? tenant)
    {
        var dto = SubscriptionGuard.Map(entity);
        return new TenantSubscriptionDto
        {
            Id = dto.Id,
            TenantId = dto.TenantId,
            TenantName = tenant?.Name,
            TenantSlug = tenant?.Slug,
            PlanId = dto.PlanId,
            PlanName = dto.PlanName,
            PlanCode = dto.PlanCode,
            Status = dto.Status,
            BillingInterval = dto.BillingInterval,
            Provider = dto.Provider,
            StartedAtUtc = dto.StartedAtUtc,
            CurrentPeriodStartUtc = dto.CurrentPeriodStartUtc,
            CurrentPeriodEndUtc = dto.CurrentPeriodEndUtc,
            TrialEndsAtUtc = dto.TrialEndsAtUtc,
            CanceledAtUtc = dto.CanceledAtUtc,
            MaxUsers = dto.MaxUsers,
            MaxWarehouses = dto.MaxWarehouses,
            MaxProducts = dto.MaxProducts,
            StripeCustomerId = dto.StripeCustomerId,
            StripeSubscriptionId = dto.StripeSubscriptionId,
            Notes = dto.Notes,
            IsEntitled = dto.IsEntitled
        };
    }
}
