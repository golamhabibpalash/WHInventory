using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.QuantityBreakManager.Queries;

public record GetQuantityBreakListDto
{
    public string? Id { get; init; }
    public string? ProductPriceId { get; init; }
    public double MinQuantity { get; init; }
    public double? MaxQuantity { get; init; }
    public double Price { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetQuantityBreakListResult
{
    public List<GetQuantityBreakListDto>? Data { get; init; }
}

public class GetQuantityBreakListRequest : IRequest<GetQuantityBreakListResult>
{
    public bool IsDeleted { get; init; } = false;
    public string? ProductPriceId { get; init; }
}

public class GetQuantityBreakListHandler : IRequestHandler<GetQuantityBreakListRequest, GetQuantityBreakListResult>
{
    private readonly IQueryContext _context;

    public GetQuantityBreakListHandler(IQueryContext context)
    {
        _context = context;
    }

    public async Task<GetQuantityBreakListResult> Handle(GetQuantityBreakListRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .QuantityBreak
            .AsNoTracking()
            .ApplyIsDeletedFilter(request.IsDeleted)
            .Where(x => request.ProductPriceId == null || x.ProductPriceId == request.ProductPriceId)
            .OrderBy(x => x.MinQuantity)
            .Select(x => new GetQuantityBreakListDto
            {
                Id = x.Id,
                ProductPriceId = x.ProductPriceId,
                MinQuantity = x.MinQuantity,
                MaxQuantity = x.MaxQuantity,
                Price = x.Price,
                CreatedAtUtc = x.CreatedAtUtc,
            });

        var dtos = await query.Take(500).ToListAsync(cancellationToken);

        return new GetQuantityBreakListResult { Data = dtos };
    }
}
