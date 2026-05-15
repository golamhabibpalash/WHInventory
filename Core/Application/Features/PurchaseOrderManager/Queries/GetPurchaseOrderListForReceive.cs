using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.PurchaseOrderManager.Queries;

public class GetPurchaseOrderListForReceiveResult
{
    public List<GetPurchaseOrderListDto>? Data { get; init; }
}

public class GetPurchaseOrderListForReceiveRequest : IRequest<GetPurchaseOrderListForReceiveResult>
{
}

public class GetPurchaseOrderListForReceiveHandler : IRequestHandler<GetPurchaseOrderListForReceiveRequest, GetPurchaseOrderListForReceiveResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetPurchaseOrderListForReceiveHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetPurchaseOrderListForReceiveResult> Handle(GetPurchaseOrderListForReceiveRequest request, CancellationToken cancellationToken)
    {
        // Total ordered quantity per PO across all items
        var orderedPerPO = await _context.PurchaseOrderItem
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .GroupBy(x => x.PurchaseOrderId)
            .Select(g => new { PurchaseOrderId = g.Key, TotalOrdered = g.Sum(x => x.Quantity ?? 0.0) })
            .ToListAsync(cancellationToken);

        // GoodsReceive.Id → PurchaseOrderId mapping
        var grToPO = await _context.GoodsReceive
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Select(x => new { GoodsReceiveId = x.Id, x.PurchaseOrderId })
            .ToListAsync(cancellationToken);

        // Total received movement per GoodsReceive record
        var receivedByGR = await _context.InventoryTransaction
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(x => x.ModuleName == nameof(GoodsReceive))
            .GroupBy(x => x.ModuleId)
            .Select(g => new { GoodsReceiveId = g.Key, TotalReceived = g.Sum(x => x.Movement ?? 0.0) })
            .ToListAsync(cancellationToken);

        // Roll up received qty per PurchaseOrderId (in memory)
        var receivedPerPO = grToPO
            .GroupJoin(
                receivedByGR,
                gr => gr.GoodsReceiveId,
                r => r.GoodsReceiveId,
                (gr, rs) => new { gr.PurchaseOrderId, TotalReceived = rs.Sum(r => r.TotalReceived) })
            .GroupBy(x => x.PurchaseOrderId)
            .ToDictionary(g => g.Key!, g => g.Sum(x => x.TotalReceived));

        // Keep only POs that have at least one item with remaining quantity
        var receivablePOIds = orderedPerPO
            .Where(po =>
            {
                var received = po.PurchaseOrderId != null && receivedPerPO.TryGetValue(po.PurchaseOrderId, out var r) ? r : 0.0;
                return po.TotalOrdered > 0 && received < po.TotalOrdered;
            })
            .Select(po => po.PurchaseOrderId!)
            .ToHashSet();

        var entities = await _context.PurchaseOrder
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Include(x => x.Vendor)
            .Include(x => x.Tax)
            .Where(x => receivablePOIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GetPurchaseOrderListDto>>(entities);

        return new GetPurchaseOrderListForReceiveResult { Data = dtos };
    }
}
