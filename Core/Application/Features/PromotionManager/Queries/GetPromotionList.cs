using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.PromotionManager.Queries;

public record GetPromotionListDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Code { get; init; }
    public string? Description { get; init; }
    public string? ProductId { get; init; }
    public string? ProductName { get; init; }
    public string? PricePolicyId { get; init; }
    public string? PricePolicyName { get; init; }
    public double? PromotionalPrice { get; init; }
    public double? DiscountPercent { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public int Priority { get; init; }
    public bool IsActive { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetPromotionListResult
{
    public List<GetPromotionListDto>? Data { get; init; }
}

public class GetPromotionListRequest : IRequest<GetPromotionListResult>
{
    public bool IsDeleted { get; init; } = false;
    public string? ProductId { get; init; }
}

public class GetPromotionListHandler : IRequestHandler<GetPromotionListRequest, GetPromotionListResult>
{
    private readonly IQueryContext _context;

    public GetPromotionListHandler(IQueryContext context)
    {
        _context = context;
    }

    public async Task<GetPromotionListResult> Handle(GetPromotionListRequest request, CancellationToken cancellationToken)
    {
        var query =
            from promo in _context.Promotion.AsNoTracking().ApplyIsDeletedFilter(request.IsDeleted)
            join p in _context.Product.AsNoTracking().ApplyIsDeletedFilter(false) on promo.ProductId equals p.Id into pLeft
            from p in pLeft.DefaultIfEmpty()
            join pol in _context.PricePolicy.AsNoTracking().ApplyIsDeletedFilter(false) on promo.PricePolicyId equals pol.Id into polLeft
            from pol in polLeft.DefaultIfEmpty()
            where (request.ProductId == null || promo.ProductId == request.ProductId)
            orderby promo.Priority descending, promo.StartDate descending
            select new GetPromotionListDto
            {
                Id = promo.Id,
                Name = promo.Name,
                Code = promo.Code,
                Description = promo.Description,
                ProductId = promo.ProductId,
                ProductName = p.Name,
                PricePolicyId = promo.PricePolicyId,
                PricePolicyName = pol.Name,
                PromotionalPrice = promo.PromotionalPrice,
                DiscountPercent = promo.DiscountPercent,
                StartDate = promo.StartDate,
                EndDate = promo.EndDate,
                Priority = promo.Priority,
                IsActive = promo.IsActive,
                CreatedAtUtc = promo.CreatedAtUtc,
            };

        var dtos = await query.Take(2000).ToListAsync(cancellationToken);

        return new GetPromotionListResult { Data = dtos };
    }
}
