using Application.Common.CQS.Queries;
using Application.Common.Services.SubscriptionManager;
using Application.Common.Tenancy;
using Infrastructure.SecurityManager.AspNetIdentity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FluentValidation;

namespace Infrastructure.SubscriptionManager;

/// <summary>
/// Enforces the plan's Max* limits for the ambient tenant. Limits come from the subscription's
/// snapshot (null = unlimited); counts are taken under the ambient tenant scope. At root scope or
/// with no subscription there are no limits to enforce, so every Ensure* call is a no-op.
/// </summary>
public class SubscriptionQuotaService : ISubscriptionQuotaService
{
    private readonly ISubscriptionGuard _guard;
    private readonly IQueryContext _queryContext;
    private readonly ITenantContext _tenantContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public SubscriptionQuotaService(
        ISubscriptionGuard guard,
        IQueryContext queryContext,
        ITenantContext tenantContext,
        UserManager<ApplicationUser> userManager)
    {
        _guard = guard;
        _queryContext = queryContext;
        _tenantContext = tenantContext;
        _userManager = userManager;
    }

    public async Task EnsureCanAddUserAsync(CancellationToken cancellationToken = default)
    {
        var limit = (await _guard.GetCurrentAsync(cancellationToken))?.MaxUsers;
        if (limit == null) return;

        var used = await CountUsersAsync(cancellationToken);
        if (used >= limit.Value)
        {
            throw new ValidationException(
                $"Your plan allows up to {limit.Value} user(s). Upgrade your plan to add more.");
        }
    }

    public async Task EnsureCanAddWarehouseAsync(CancellationToken cancellationToken = default)
    {
        var limit = (await _guard.GetCurrentAsync(cancellationToken))?.MaxWarehouses;
        if (limit == null) return;

        var used = await CountWarehousesAsync(cancellationToken);
        if (used >= limit.Value)
        {
            throw new ValidationException(
                $"Your plan allows up to {limit.Value} warehouse(s). Upgrade your plan to add more.");
        }
    }

    public async Task EnsureCanAddProductAsync(CancellationToken cancellationToken = default)
    {
        var limit = (await _guard.GetCurrentAsync(cancellationToken))?.MaxProducts;
        if (limit == null) return;

        var used = await CountProductsAsync(cancellationToken);
        if (used >= limit.Value)
        {
            throw new ValidationException(
                $"Your plan allows up to {limit.Value} product(s). Upgrade your plan to add more.");
        }
    }

    public async Task<SubscriptionUsageDto> GetUsageAsync(CancellationToken cancellationToken = default)
    {
        return new SubscriptionUsageDto
        {
            Users = await CountUsersAsync(cancellationToken),
            Warehouses = await CountWarehousesAsync(cancellationToken),
            Products = await CountProductsAsync(cancellationToken)
        };
    }

    private async Task<int> CountUsersAsync(CancellationToken cancellationToken)
    {
        // ApplicationUser is not tenant-filtered, so scope the count explicitly by the ambient tenant.
        var tenantId = _tenantContext.TenantId;
        if (string.IsNullOrEmpty(tenantId)) return 0;

        return await _userManager.Users
            .AsNoTracking()
            .CountAsync(x => x.TenantId == tenantId && x.IsDeleted != true, cancellationToken);
    }

    private async Task<int> CountWarehousesAsync(CancellationToken cancellationToken)
    {
        return await _queryContext.Warehouse
            .AsNoTracking()
            .CountAsync(x => x.IsDeleted == false && x.SystemWarehouse != true, cancellationToken);
    }

    private async Task<int> CountProductsAsync(CancellationToken cancellationToken)
    {
        return await _queryContext.Product
            .AsNoTracking()
            .CountAsync(x => x.IsDeleted == false, cancellationToken);
    }
}
