using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.InventoryTransactionManager.Queries;

public class GetProductStockResult
{
    public string? ProductId { get; init; }
    public double Stock { get; init; }
}

public class GetProductStockRequest : IRequest<GetProductStockResult>
{
    public string? ProductId { get; init; }
}

public class GetProductStockValidator : AbstractValidator<GetProductStockRequest>
{
    public GetProductStockValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
    }
}

public class GetProductStockHandler : IRequestHandler<GetProductStockRequest, GetProductStockResult>
{
    private readonly IQueryContext _context;

    public GetProductStockHandler(IQueryContext context)
    {
        _context = context;
    }

    public async Task<GetProductStockResult> Handle(GetProductStockRequest request, CancellationToken cancellationToken)
    {
        var stock = await _context.InventoryTransaction
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(x =>
                x.Status == Domain.Enums.InventoryTransactionStatus.Confirmed &&
                x.ProductId == request.ProductId &&
                x.Warehouse != null &&
                x.Warehouse.SystemWarehouse == false)
            .SumAsync(x => (double?)x.Stock ?? 0.0, cancellationToken);

        return new GetProductStockResult
        {
            ProductId = request.ProductId,
            Stock = stock
        };
    }
}
