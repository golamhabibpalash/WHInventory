using Application.Common.Services.SubscriptionManager;
using MediatR;

namespace Application.Features.TenantSubscriptionManager.Queries;

public class GetTenantSubscriptionListResult
{
    public List<TenantSubscriptionDto>? Data { get; init; }
}

/// <summary>Platform-only: every tenant's current subscription. Served by the admin service, which
/// elevates to root scope internally.</summary>
public class GetTenantSubscriptionListRequest : IRequest<GetTenantSubscriptionListResult>
{
}

public class GetTenantSubscriptionListHandler : IRequestHandler<GetTenantSubscriptionListRequest, GetTenantSubscriptionListResult>
{
    private readonly ITenantSubscriptionAdminService _adminService;

    public GetTenantSubscriptionListHandler(ITenantSubscriptionAdminService adminService)
    {
        _adminService = adminService;
    }

    public async Task<GetTenantSubscriptionListResult> Handle(GetTenantSubscriptionListRequest request, CancellationToken cancellationToken)
    {
        var data = await _adminService.GetAllAsync(cancellationToken);
        return new GetTenantSubscriptionListResult { Data = data };
    }
}
