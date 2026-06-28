using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Features.PriceManager.Services;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.PriceManager.Queries;

public record GetPriceReportDto
{
    public string? ProductId { get; init; }
    public string? ProductName { get; init; }
    public string? ProductNumber { get; init; }
    public double? DefaultPrice { get; init; }
    public double? WeightedAverageCost { get; init; }
    public double? ActiveFixedPrice { get; init; }
    public string? ActivePricePolicyName { get; init; }
    public double? Margin { get; init; }
    public double? MarginPercent { get; init; }
    public bool HasPromotion { get; init; }
    public double? PromotionalPrice { get; init; }
}

public class GetPriceReportResult
{
    public List<GetPriceReportDto>? Data { get; init; }
}

public class GetPriceReportRequest : IRequest<GetPriceReportResult>
{
    public string? ProductGroupId { get; init; }
    public string? ProductId { get; init; }
}

public class GetPriceReportHandler : IRequestHandler<GetPriceReportRequest, GetPriceReportResult>
{
    private readonly IQueryContext _context;
    private readonly WacService _wacService;

    public GetPriceReportHandler(IQueryContext context, WacService wacService)
    {
        _context = context;
        _wacService = wacService;
    }

    public async Task<GetPriceReportResult> Handle(GetPriceReportRequest request, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;

        var products = await _context.Product
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(p => request.ProductGroupId == null || p.ProductGroupId == request.ProductGroupId)
            .Where(p => request.ProductId == null || p.Id == request.ProductId)
            .OrderBy(p => p.Name)
            .Take(500)
            .ToListAsync(cancellationToken);

        var productIds = products.Select(p => p.Id).ToList();

        var activePrices = await _context.ProductPrice
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(pp => pp.ProductId != null && productIds.Contains(pp.ProductId))
            .Where(pp =>
                pp.IsActive &&
                (pp.EffectiveFrom == null || pp.EffectiveFrom <= today) &&
                (pp.EffectiveTo == null || pp.EffectiveTo >= today))
            .Join(_context.PricePolicy.AsNoTracking().ApplyIsDeletedFilter(false),
                pp => pp.PricePolicyId, pol => pol.Id,
                (pp, pol) => new { pp, pol })
            .OrderByDescending(x => x.pp.Priority)
            .ToListAsync(cancellationToken);

        var activePromotions = await _context.Promotion
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(pr => pr.ProductId != null && productIds.Contains(pr.ProductId!))
            .Where(pr =>
                pr.IsActive &&
                (pr.StartDate == null || pr.StartDate <= today) &&
                (pr.EndDate == null || pr.EndDate >= today))
            .ToListAsync(cancellationToken);

        var dtos = new List<GetPriceReportDto>();

        foreach (var product in products)
        {
            var wac = await _wacService.GetWeightedAverageCostAsync(product.Id, cancellationToken);
            var bestPrice = activePrices.FirstOrDefault(x => x.pp.ProductId == product.Id);
            var activePromo = activePromotions.FirstOrDefault(pr => pr.ProductId == product.Id);

            double? activeFixedPrice = null;
            string? policyName = null;

            if (bestPrice != null)
            {
                activeFixedPrice = bestPrice.pp.CalculationMethod switch
                {
                    PricingCalculationMethod.FixedPrice => bestPrice.pp.FixedPrice,
                    PricingCalculationMethod.CostPlusMarkupPercent => wac * (1 + (bestPrice.pp.MarkupPercent ?? 0) / 100.0),
                    PricingCalculationMethod.CostPlusMarkupAmount => wac + (bestPrice.pp.MarkupAmount ?? 0),
                    PricingCalculationMethod.GrossMargin => (bestPrice.pp.MarginPercent ?? 0) >= 100 ? wac :
                        wac / (1.0 - (bestPrice.pp.MarginPercent ?? 0) / 100.0),
                    PricingCalculationMethod.FormulaBased => wac * (bestPrice.pp.FormulaMultiplier ?? 1),
                    _ => bestPrice.pp.FixedPrice,
                };
                policyName = bestPrice.pol.Name;
            }

            var effectivePrice = activeFixedPrice ?? product.UnitPrice ?? 0;
            var margin = effectivePrice - wac;
            var marginPercent = wac > 0 ? (margin / wac) * 100.0 : 0;

            dtos.Add(new GetPriceReportDto
            {
                ProductId = product.Id,
                ProductName = product.Name,
                ProductNumber = product.Number,
                DefaultPrice = product.UnitPrice,
                WeightedAverageCost = Math.Round(wac, 4),
                ActiveFixedPrice = activeFixedPrice.HasValue ? Math.Round(activeFixedPrice.Value, 4) : null,
                ActivePricePolicyName = policyName,
                Margin = Math.Round(margin, 4),
                MarginPercent = Math.Round(marginPercent, 2),
                HasPromotion = activePromo != null,
                PromotionalPrice = activePromo?.PromotionalPrice,
            });
        }

        return new GetPriceReportResult { Data = dtos };
    }
}
