using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.PriceManager.Services;

public class PriceResolutionResult
{
    public double CalculatedPrice { get; set; }
    public PriceSource PriceSource { get; set; }
    public string? PriceSourceName { get; set; }
    public double CostPrice { get; set; }
    public double Profit { get; set; }
    public double ProfitPercent { get; set; }
}

public class PriceResolutionEngine
{
    private readonly IQueryContext _context;
    private readonly WacService _wacService;

    public PriceResolutionEngine(IQueryContext context, WacService wacService)
    {
        _context = context;
        _wacService = wacService;
    }

    public async Task<PriceResolutionResult> ResolvePriceAsync(
        string productId,
        string? customerId,
        double quantity,
        DateTime? saleDate,
        CancellationToken cancellationToken = default)
    {
        var effectiveDate = saleDate ?? DateTime.UtcNow.Date;
        var costPrice = await _wacService.GetWeightedAverageCostAsync(productId, cancellationToken);

        // 1. Promotional Price — highest priority
        var promotion = await _context.Promotion
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(pr =>
                pr.ProductId == productId &&
                pr.IsActive &&
                (pr.StartDate == null || pr.StartDate <= effectiveDate) &&
                (pr.EndDate == null || pr.EndDate >= effectiveDate))
            .OrderByDescending(pr => pr.Priority)
            .FirstOrDefaultAsync(cancellationToken);

        if (promotion != null)
        {
            var promoPrice = promotion.PromotionalPrice ?? 0;

            if (promotion.DiscountPercent.HasValue && promotion.DiscountPercent > 0)
            {
                var basePrice = await GetDefaultProductPriceAsync(productId, cancellationToken);
                promoPrice = basePrice * (1 - promotion.DiscountPercent.Value / 100.0);
            }

            return BuildResult(promoPrice, PriceSource.Promotional, costPrice);
        }

        // 2. Customer Price Group — resolve via CustomerGroup.PricePolicyId
        if (customerId != null)
        {
            var customer = await _context.Customer
                .AsNoTracking()
                .ApplyIsDeletedFilter(false)
                .Where(c => c.Id == customerId)
                .Select(c => new { c.CustomerGroupId })
                .FirstOrDefaultAsync(cancellationToken);

            if (customer?.CustomerGroupId != null)
            {
                var customerGroup = await _context.CustomerGroup
                    .AsNoTracking()
                    .ApplyIsDeletedFilter(false)
                    .Where(cg => cg.Id == customer.CustomerGroupId)
                    .Select(cg => new { cg.PricePolicyId })
                    .FirstOrDefaultAsync(cancellationToken);

                if (customerGroup?.PricePolicyId != null)
                {
                    var groupPrice = await _context.ProductPrice
                        .AsNoTracking()
                        .ApplyIsDeletedFilter(false)
                        .Where(pp =>
                            pp.ProductId == productId &&
                            pp.PricePolicyId == customerGroup.PricePolicyId &&
                            pp.IsActive &&
                            (pp.EffectiveFrom == null || pp.EffectiveFrom <= effectiveDate) &&
                            (pp.EffectiveTo == null || pp.EffectiveTo >= effectiveDate))
                        .OrderByDescending(pp => pp.Priority)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (groupPrice != null)
                    {
                        // 3. Quantity break check for this policy price
                        var qbBreak = await _context.QuantityBreak
                            .AsNoTracking()
                            .ApplyIsDeletedFilter(false)
                            .Where(qb =>
                                qb.ProductPriceId == groupPrice.Id &&
                                qb.MinQuantity <= quantity &&
                                (qb.MaxQuantity == null || qb.MaxQuantity >= quantity))
                            .OrderByDescending(qb => qb.MinQuantity)
                            .FirstOrDefaultAsync(cancellationToken);

                        if (qbBreak != null)
                        {
                            return BuildResult(qbBreak.Price, PriceSource.QuantityBreak, costPrice);
                        }

                        var calculatedGroupPrice = CalculatePrice(groupPrice, costPrice);
                        return BuildResult(calculatedGroupPrice, PriceSource.CustomerPriceGroup, costPrice);
                    }
                }
            }
        }

        // 4. General product price — best matching policy (highest priority, no specific policy filter)
        var bestProductPrice = await _context.ProductPrice
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(pp =>
                pp.ProductId == productId &&
                pp.IsActive &&
                (pp.EffectiveFrom == null || pp.EffectiveFrom <= effectiveDate) &&
                (pp.EffectiveTo == null || pp.EffectiveTo >= effectiveDate))
            .OrderByDescending(pp => pp.Priority)
            .FirstOrDefaultAsync(cancellationToken);

        if (bestProductPrice != null)
        {
            // Quantity break check
            var qbBreak = await _context.QuantityBreak
                .AsNoTracking()
                .ApplyIsDeletedFilter(false)
                .Where(qb =>
                    qb.ProductPriceId == bestProductPrice.Id &&
                    qb.MinQuantity <= quantity &&
                    (qb.MaxQuantity == null || qb.MaxQuantity >= quantity))
                .OrderByDescending(qb => qb.MinQuantity)
                .FirstOrDefaultAsync(cancellationToken);

            if (qbBreak != null)
            {
                return BuildResult(qbBreak.Price, PriceSource.QuantityBreak, costPrice);
            }

            var calculatedPrice = CalculatePrice(bestProductPrice, costPrice);
            return BuildResult(calculatedPrice, PriceSource.ProductPricePolicy, costPrice);
        }

        // 5. Default product unit price fallback
        var defaultPrice = await GetDefaultProductPriceAsync(productId, cancellationToken);
        return BuildResult(defaultPrice, PriceSource.DefaultProductPrice, costPrice);
    }

    private async Task<double> GetDefaultProductPriceAsync(string productId, CancellationToken cancellationToken)
    {
        var product = await _context.Product
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(p => p.Id == productId)
            .Select(p => new { p.UnitPrice })
            .FirstOrDefaultAsync(cancellationToken);

        return product?.UnitPrice ?? 0;
    }

    private static double CalculatePrice(Domain.Entities.ProductPrice pp, double costPrice)
    {
        return pp.CalculationMethod switch
        {
            PricingCalculationMethod.FixedPrice =>
                pp.FixedPrice ?? 0,
            PricingCalculationMethod.CostPlusMarkupPercent =>
                costPrice * (1 + (pp.MarkupPercent ?? 0) / 100.0),
            PricingCalculationMethod.CostPlusMarkupAmount =>
                costPrice + (pp.MarkupAmount ?? 0),
            PricingCalculationMethod.GrossMargin =>
                (pp.MarginPercent ?? 0) >= 100 ? costPrice :
                costPrice / (1.0 - (pp.MarginPercent ?? 0) / 100.0),
            PricingCalculationMethod.FormulaBased =>
                costPrice * (pp.FormulaMultiplier ?? 1),
            _ => pp.FixedPrice ?? 0,
        };
    }

    private static PriceResolutionResult BuildResult(double calculatedPrice, PriceSource source, double costPrice)
    {
        var profit = calculatedPrice - costPrice;
        var profitPercent = costPrice > 0 ? (profit / costPrice) * 100.0 : 0;

        return new PriceResolutionResult
        {
            CalculatedPrice = Math.Round(calculatedPrice, 4),
            PriceSource = source,
            PriceSourceName = source.ToString(),
            CostPrice = Math.Round(costPrice, 4),
            Profit = Math.Round(profit, 4),
            ProfitPercent = Math.Round(profitPercent, 2),
        };
    }
}
