using Application.Common.CQS.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.NavigationSortOrderManager;

public class GetNavigationSortOrderResult
{
    public string? SortOrderJson { get; init; }
}

public class GetNavigationSortOrderRequest : IRequest<GetNavigationSortOrderResult>
{
    public string? UserId { get; init; }
}

public class GetNavigationSortOrderHandler : IRequestHandler<GetNavigationSortOrderRequest, GetNavigationSortOrderResult>
{
    private readonly IQueryContext _context;

    public GetNavigationSortOrderHandler(IQueryContext context)
    {
        _context = context;
    }

    public async Task<GetNavigationSortOrderResult> Handle(GetNavigationSortOrderRequest request, CancellationToken cancellationToken)
    {
        var entity = await _context.NavigationMenuSortOrder
            .AsNoTracking()
            .Where(x => x.UserId == request.UserId && !x.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        return new GetNavigationSortOrderResult { SortOrderJson = entity?.SortOrderJson };
    }
}
