using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Services.SubscriptionManager;
using Application.Common.Services.TenantManager;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TenantManager.Queries;

public record GetTenantListDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Slug { get; init; }
    public bool IsActive { get; init; }
    public int UserCount { get; init; }
    public DateTime? CreatedAtUtc { get; init; }

    // Subscription summary — null plan means the tenant has no subscription row yet.
    public string? PlanName { get; init; }
    public string? SubscriptionStatusText { get; init; }
    public bool IsEntitled { get; init; }
}

public class GetTenantListResult
{
    public List<GetTenantListDto>? Data { get; init; }
}

public class GetTenantListRequest : IRequest<GetTenantListResult>
{
    public bool IsDeleted { get; init; } = false;
}

public class GetTenantListHandler : IRequestHandler<GetTenantListRequest, GetTenantListResult>
{
    private readonly IQueryContext _context;
    private readonly ITenantProvisioningService _tenantService;
    private readonly ITenantSubscriptionAdminService _subscriptionAdminService;

    public GetTenantListHandler(
        IQueryContext context,
        ITenantProvisioningService tenantService,
        ITenantSubscriptionAdminService subscriptionAdminService)
    {
        _context = context;
        _tenantService = tenantService;
        _subscriptionAdminService = subscriptionAdminService;
    }

    public async Task<GetTenantListResult> Handle(GetTenantListRequest request, CancellationToken cancellationToken)
    {
        // Tenant is the registry itself and carries no tenant filter, so this reads across all of
        // them by design. The controller restricts who may ask.
        var tenants = await _context.Tenant
            .AsNoTracking()
            .ApplyIsDeletedFilter(request.IsDeleted)
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Slug,
                x.IsActive,
                x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        var ids = tenants.Select(x => x.Id).ToList();

        var userCounts = await _tenantService.GetUserCountsAsync(ids, cancellationToken);

        // Cross-tenant read; the admin service elevates to root internally. One current row per
        // tenant (newest first), so the first seen per tenant id wins.
        var subscriptions = await _subscriptionAdminService.GetAllAsync(cancellationToken);
        var subscriptionByTenant = subscriptions
            .Where(x => x.TenantId != null)
            .GroupBy(x => x.TenantId!)
            .ToDictionary(g => g.Key, g => g.First());

        var data = tenants
            .Select(x =>
            {
                subscriptionByTenant.TryGetValue(x.Id, out var sub);
                return new GetTenantListDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Slug = x.Slug,
                    IsActive = x.IsActive,
                    UserCount = userCounts.GetValueOrDefault(x.Id, 0),
                    CreatedAtUtc = x.CreatedAtUtc,
                    PlanName = sub?.PlanName,
                    SubscriptionStatusText = sub?.Status.ToString(),
                    IsEntitled = sub?.IsEntitled ?? false
                };
            })
            .ToList();

        return new GetTenantListResult { Data = data };
    }
}
