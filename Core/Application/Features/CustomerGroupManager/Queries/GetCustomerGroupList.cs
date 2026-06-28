using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.CustomerGroupManager.Queries;

public record GetCustomerGroupListDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? PricePolicyId { get; init; }
    public string? PricePolicyName { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetCustomerGroupListResult
{
    public List<GetCustomerGroupListDto>? Data { get; init; }
}

public class GetCustomerGroupListRequest : IRequest<GetCustomerGroupListResult>
{
    public bool IsDeleted { get; init; } = false;
}


public class GetCustomerGroupListHandler : IRequestHandler<GetCustomerGroupListRequest, GetCustomerGroupListResult>
{
    private readonly IQueryContext _context;

    public GetCustomerGroupListHandler(IQueryContext context)
    {
        _context = context;
    }

    public async Task<GetCustomerGroupListResult> Handle(GetCustomerGroupListRequest request, CancellationToken cancellationToken)
    {
        var query =
            from cg in _context.CustomerGroup.AsNoTracking().ApplyIsDeletedFilter(request.IsDeleted)
            join pp in _context.PricePolicy.AsNoTracking().ApplyIsDeletedFilter(false) on cg.PricePolicyId equals pp.Id into ppLeft
            from pp in ppLeft.DefaultIfEmpty()
            orderby cg.Name
            select new GetCustomerGroupListDto
            {
                Id = cg.Id,
                Name = cg.Name,
                Description = cg.Description,
                PricePolicyId = cg.PricePolicyId,
                PricePolicyName = pp.Name,
                CreatedAtUtc = cg.CreatedAtUtc,
            };

        var dtos = await query.Take(2000).ToListAsync(cancellationToken);

        return new GetCustomerGroupListResult
        {
            Data = dtos
        };
    }


}



