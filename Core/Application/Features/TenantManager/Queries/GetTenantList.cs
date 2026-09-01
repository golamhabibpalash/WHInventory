using Application.Common.CQS.Queries;
using Application.Common.Extensions;
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

    public GetTenantListHandler(IQueryContext context, ITenantProvisioningService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
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

        var data = tenants
            .Select(x => new GetTenantListDto
            {
                Id = x.Id,
                Name = x.Name,
                Slug = x.Slug,
                IsActive = x.IsActive,
                UserCount = userCounts.GetValueOrDefault(x.Id, 0),
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToList();

        return new GetTenantListResult { Data = data };
    }
}
