using FluentValidation;
using MediatR;

namespace Application.Features.SalesOrderManager.Queries;

public class GetProductAvailabilityResult
{
    public ProductAvailability? Data { get; init; }
}

/// <summary>
/// Stock hint for the sales order product picker: on hand, pending delivery on other open sales
/// orders, and available. Same figures the server checks when a line is saved.
/// </summary>
public class GetProductAvailabilityRequest : IRequest<GetProductAvailabilityResult>
{
    public string? ProductId { get; init; }

    /// <summary>The order being edited; its own lines are shown as "in cart" by the page, not as pending.</summary>
    public string? ExcludeSalesOrderId { get; init; }
}

public class GetProductAvailabilityValidator : AbstractValidator<GetProductAvailabilityRequest>
{
    public GetProductAvailabilityValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
    }
}

public class GetProductAvailabilityHandler : IRequestHandler<GetProductAvailabilityRequest, GetProductAvailabilityResult>
{
    private readonly SalesOrderService _salesOrderService;

    public GetProductAvailabilityHandler(SalesOrderService salesOrderService)
    {
        _salesOrderService = salesOrderService;
    }

    public async Task<GetProductAvailabilityResult> Handle(GetProductAvailabilityRequest request, CancellationToken cancellationToken)
    {
        var availability = await _salesOrderService.GetAvailabilityAsync(
            request.ProductId, request.ExcludeSalesOrderId, null, cancellationToken);

        return new GetProductAvailabilityResult
        {
            Data = availability
        };
    }
}
