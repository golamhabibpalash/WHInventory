using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.PriceHistoryManager.Queries;

public record GetPriceHistoryListDto
{
    public string? Id { get; init; }
    public string? ProductPriceId { get; init; }
    public string? ProductId { get; init; }
    public string? ProductName { get; init; }
    public double? PreviousPrice { get; init; }
    public double? NewPrice { get; init; }
    public string? ChangedById { get; init; }
    public DateTime ChangedDate { get; init; }
    public string? ChangeReason { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetPriceHistoryListResult
{
    public List<GetPriceHistoryListDto>? Data { get; init; }
}

public class GetPriceHistoryListRequest : IRequest<GetPriceHistoryListResult>
{
    public string? ProductPriceId { get; init; }
    public string? ProductId { get; init; }
}

public class GetPriceHistoryListHandler : IRequestHandler<GetPriceHistoryListRequest, GetPriceHistoryListResult>
{
    private readonly IQueryContext _context;

    public GetPriceHistoryListHandler(IQueryContext context)
    {
        _context = context;
    }

    public async Task<GetPriceHistoryListResult> Handle(GetPriceHistoryListRequest request, CancellationToken cancellationToken)
    {
        var query =
            from h in _context.PriceHistory.AsNoTracking().ApplyIsDeletedFilter(false)
            join pp in _context.ProductPrice.AsNoTracking().ApplyIsDeletedFilter(false) on h.ProductPriceId equals pp.Id into ppLeft
            from pp in ppLeft.DefaultIfEmpty()
            join p in _context.Product.AsNoTracking().ApplyIsDeletedFilter(false) on pp.ProductId equals p.Id into pLeft
            from p in pLeft.DefaultIfEmpty()
            where (request.ProductPriceId == null || h.ProductPriceId == request.ProductPriceId)
            where (request.ProductId == null || pp.ProductId == request.ProductId)
            orderby h.ChangedDate descending
            select new GetPriceHistoryListDto
            {
                Id = h.Id,
                ProductPriceId = h.ProductPriceId,
                ProductId = pp.ProductId,
                ProductName = p.Name,
                PreviousPrice = h.PreviousPrice,
                NewPrice = h.NewPrice,
                ChangedById = h.ChangedById,
                ChangedDate = h.ChangedDate,
                ChangeReason = h.ChangeReason,
                CreatedAtUtc = h.CreatedAtUtc,
            };

        var dtos = await query.Take(2000).ToListAsync(cancellationToken);

        return new GetPriceHistoryListResult { Data = dtos };
    }
}
