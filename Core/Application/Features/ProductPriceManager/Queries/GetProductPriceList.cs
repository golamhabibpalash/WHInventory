using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.ProductPriceManager.Queries;

public record GetProductPriceListDto
{
    public string? Id { get; init; }
    public string? ProductId { get; init; }
    public string? ProductName { get; init; }
    public string? PricePolicyId { get; init; }
    public string? PricePolicyName { get; init; }
    public PricingCalculationMethod CalculationMethod { get; init; }
    public string? CalculationMethodName { get; init; }
    public double? FixedPrice { get; init; }
    public double? MarkupPercent { get; init; }
    public double? MarkupAmount { get; init; }
    public double? MarginPercent { get; init; }
    public double? FormulaMultiplier { get; init; }
    public double? MinimumSellingPrice { get; init; }
    public double? MaximumDiscountPercent { get; init; }
    public DateTime? EffectiveFrom { get; init; }
    public DateTime? EffectiveTo { get; init; }
    public int Priority { get; init; }
    public bool IsActive { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetProductPriceListResult
{
    public List<GetProductPriceListDto>? Data { get; init; }
}

public class GetProductPriceListRequest : IRequest<GetProductPriceListResult>
{
    public bool IsDeleted { get; init; } = false;
    public string? ProductId { get; init; }
    public string? PricePolicyId { get; init; }
}

public class GetProductPriceListHandler : IRequestHandler<GetProductPriceListRequest, GetProductPriceListResult>
{
    private readonly IQueryContext _context;

    public GetProductPriceListHandler(IQueryContext context)
    {
        _context = context;
    }

    public async Task<GetProductPriceListResult> Handle(GetProductPriceListRequest request, CancellationToken cancellationToken)
    {
        var query =
            from pp in _context.ProductPrice.AsNoTracking().ApplyIsDeletedFilter(request.IsDeleted)
            join p in _context.Product.AsNoTracking().ApplyIsDeletedFilter(false) on pp.ProductId equals p.Id into pLeft
            from p in pLeft.DefaultIfEmpty()
            join pol in _context.PricePolicy.AsNoTracking().ApplyIsDeletedFilter(false) on pp.PricePolicyId equals pol.Id into polLeft
            from pol in polLeft.DefaultIfEmpty()
            where (request.ProductId == null || pp.ProductId == request.ProductId)
            where (request.PricePolicyId == null || pp.PricePolicyId == request.PricePolicyId)
            orderby pp.Priority descending, pp.CreatedAtUtc descending
            select new GetProductPriceListDto
            {
                Id = pp.Id,
                ProductId = pp.ProductId,
                ProductName = p.Name,
                PricePolicyId = pp.PricePolicyId,
                PricePolicyName = pol.Name,
                CalculationMethod = pp.CalculationMethod,
                CalculationMethodName = pp.CalculationMethod.ToString(),
                FixedPrice = pp.FixedPrice,
                MarkupPercent = pp.MarkupPercent,
                MarkupAmount = pp.MarkupAmount,
                MarginPercent = pp.MarginPercent,
                FormulaMultiplier = pp.FormulaMultiplier,
                MinimumSellingPrice = pp.MinimumSellingPrice,
                MaximumDiscountPercent = pp.MaximumDiscountPercent,
                EffectiveFrom = pp.EffectiveFrom,
                EffectiveTo = pp.EffectiveTo,
                Priority = pp.Priority,
                IsActive = pp.IsActive,
                CreatedAtUtc = pp.CreatedAtUtc,
            };

        var dtos = await query.Take(2000).ToListAsync(cancellationToken);

        return new GetProductPriceListResult { Data = dtos };
    }
}
