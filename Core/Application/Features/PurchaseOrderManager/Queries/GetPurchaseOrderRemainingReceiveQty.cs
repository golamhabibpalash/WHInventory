using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.PurchaseOrderManager.Queries;

public class GetPurchaseOrderRemainingReceiveQtyDto
{
    public string? ProductId { get; init; }
    public string? ProductName { get; init; }
    public double OrderedQty { get; init; }
    public double ReceivedQty { get; init; }
    public double RemainingQty { get; init; }
}

public class GetPurchaseOrderRemainingReceiveQtyResult
{
    public List<GetPurchaseOrderRemainingReceiveQtyDto>? Data { get; init; }
}

public class GetPurchaseOrderRemainingReceiveQtyRequest : IRequest<GetPurchaseOrderRemainingReceiveQtyResult>
{
    public string? PurchaseOrderId { get; init; }
}

public class GetPurchaseOrderRemainingReceiveQtyHandler : IRequestHandler<GetPurchaseOrderRemainingReceiveQtyRequest, GetPurchaseOrderRemainingReceiveQtyResult>
{
    private readonly IQueryContext _context;

    public GetPurchaseOrderRemainingReceiveQtyHandler(IQueryContext context)
    {
        _context = context;
    }

    public async Task<GetPurchaseOrderRemainingReceiveQtyResult> Handle(GetPurchaseOrderRemainingReceiveQtyRequest request, CancellationToken cancellationToken)
    {
        var orderedItems = await _context.PurchaseOrderItem
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(x => x.PurchaseOrderId == request.PurchaseOrderId)
            .Include(x => x.Product)
            .ToListAsync(cancellationToken);

        var goodsReceiveIds = await _context.GoodsReceive
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(x => x.PurchaseOrderId == request.PurchaseOrderId)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        Dictionary<string, double> receivedDict;
        if (goodsReceiveIds.Count > 0)
        {
            var receivedPerProduct = await _context.InventoryTransaction
                .AsNoTracking()
                .ApplyIsDeletedFilter(false)
                .Where(x => x.ModuleId != null && goodsReceiveIds.Contains(x.ModuleId)
                         && x.ModuleName == nameof(GoodsReceive))
                .GroupBy(x => x.ProductId)
                .Select(g => new { ProductId = g.Key, ReceivedQty = g.Sum(x => x.Movement ?? 0.0) })
                .ToListAsync(cancellationToken);

            receivedDict = receivedPerProduct
                .Where(x => x.ProductId != null)
                .ToDictionary(x => x.ProductId!, x => x.ReceivedQty);
        }
        else
        {
            receivedDict = new Dictionary<string, double>();
        }

        var data = orderedItems
            .GroupBy(x => x.ProductId)
            .Select(g =>
            {
                var productId = g.Key;
                var orderedQty = g.Sum(x => x.Quantity ?? 0.0);
                var receivedQty = productId != null && receivedDict.TryGetValue(productId, out var r) ? r : 0.0;
                return new GetPurchaseOrderRemainingReceiveQtyDto
                {
                    ProductId = productId,
                    ProductName = g.First().Product?.Name ?? string.Empty,
                    OrderedQty = orderedQty,
                    ReceivedQty = receivedQty,
                    RemainingQty = Math.Max(0, orderedQty - receivedQty)
                };
            })
            .ToList();

        return new GetPurchaseOrderRemainingReceiveQtyResult { Data = data };
    }
}
