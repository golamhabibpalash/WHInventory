using Application.Features.PriceManager.Services;
using Domain.Enums;
using MediatR;

namespace Application.Features.PriceManager.Queries;

public class ResolvePriceForProductResult
{
    public double CalculatedPrice { get; set; }
    public PriceSource PriceSource { get; set; }
    public string? PriceSourceName { get; set; }
    public double CostPrice { get; set; }
    public double Profit { get; set; }
    public double ProfitPercent { get; set; }
}

public class ResolvePriceForProductRequest : IRequest<ResolvePriceForProductResult>
{
    public string? ProductId { get; init; }
    public string? CustomerId { get; init; }
    public double Quantity { get; init; } = 1;
    public DateTime? SaleDate { get; init; }
}

public class ResolvePriceForProductHandler : IRequestHandler<ResolvePriceForProductRequest, ResolvePriceForProductResult>
{
    private readonly PriceResolutionEngine _priceResolutionEngine;

    public ResolvePriceForProductHandler(PriceResolutionEngine priceResolutionEngine)
    {
        _priceResolutionEngine = priceResolutionEngine;
    }

    public async Task<ResolvePriceForProductResult> Handle(ResolvePriceForProductRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.ProductId))
        {
            return new ResolvePriceForProductResult();
        }

        var resolution = await _priceResolutionEngine.ResolvePriceAsync(
            request.ProductId,
            request.CustomerId,
            request.Quantity,
            request.SaleDate,
            cancellationToken);

        return new ResolvePriceForProductResult
        {
            CalculatedPrice = resolution.CalculatedPrice,
            PriceSource = resolution.PriceSource,
            PriceSourceName = resolution.PriceSourceName,
            CostPrice = resolution.CostPrice,
            Profit = resolution.Profit,
            ProfitPercent = resolution.ProfitPercent,
        };
    }
}
