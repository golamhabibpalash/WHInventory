using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Domain.Entities;
using Domain.Enums;
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
    private readonly IQueryContext _context;

    public GetPurchaseOrderListForReceiveHandler(IQueryContext context)
    {
        _context = context;
    }

    public async Task<GetPurchaseOrderListForReceiveResult> Handle(GetPurchaseOrderListForReceiveRequest request, CancellationToken cancellationToken)
    {
        // Step 1: total ordered quantity per PO
        var orderedByPO = await _context.PurchaseOrderItem
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(x => x.PurchaseOrderId != null)
            .GroupBy(x => x.PurchaseOrderId!)
            .Select(g => new { PurchaseOrderId = g.Key, TotalOrdered = g.Sum(x => x.Quantity ?? 0.0) })
            .Where(x => x.TotalOrdered > 0)
            .ToListAsync(cancellationToken);

        // Step 2: total received quantity per PO (GoodsReceive → InventoryTransaction join)
        var receivedByPO = await (
            from gr in _context.GoodsReceive.AsNoTracking().ApplyIsDeletedFilter(false)
            where gr.PurchaseOrderId != null
            join it in _context.InventoryTransaction.AsNoTracking().ApplyIsDeletedFilter(false)
                on gr.Id equals it.ModuleId
            where it.ModuleName == nameof(GoodsReceive)
            group it by gr.PurchaseOrderId into g
            select new { PurchaseOrderId = g.Key!, TotalReceived = g.Sum(x => x.Movement ?? 0.0) }
        ).ToListAsync(cancellationToken);

        // Step 3: find POs that still have remaining quantity to receive
        var receivedDict = receivedByPO.ToDictionary(x => x.PurchaseOrderId, x => x.TotalReceived);
        var receivablePOIds = orderedByPO
            .Where(x =>
            {
                var received = receivedDict.TryGetValue(x.PurchaseOrderId, out var r) ? r : 0.0;
                return received < x.TotalOrdered;
            })
            .Select(x => x.PurchaseOrderId)
            .ToHashSet();

        // Step 4: load PO records with vendor name via LEFT JOIN
        var dtos = await (
            from po in _context.PurchaseOrder.AsNoTracking().ApplyIsDeletedFilter(false)
            join v in _context.Vendor.AsNoTracking().ApplyIsDeletedFilter(false)
                on po.VendorId equals v.Id into vLeft
            from v in vLeft.DefaultIfEmpty()
            where receivablePOIds.Contains(po.Id)
                && po.OrderStatus != PurchaseOrderStatus.Draft
                && po.OrderStatus != PurchaseOrderStatus.Cancelled
                && po.OrderStatus != PurchaseOrderStatus.Archived
            orderby po.CreatedAtUtc descending
            select new GetPurchaseOrderListDto
            {
                Id = po.Id,
                Number = po.Number,
                OrderDate = po.OrderDate,
                OrderStatus = po.OrderStatus,
                OrderStatusName = po.OrderStatus.HasValue ? po.OrderStatus.Value.ToFriendlyName() : string.Empty,
                Description = po.Description,
                VendorId = po.VendorId,
                VendorName = v.Name,
                CreatedAtUtc = po.CreatedAtUtc,
            }
        ).Take(500).ToListAsync(cancellationToken);

        return new GetPurchaseOrderListForReceiveResult { Data = dtos };
    }
}
