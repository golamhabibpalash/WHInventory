using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
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
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetPurchaseOrderListForReceiveHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetPurchaseOrderListForReceiveResult> Handle(GetPurchaseOrderListForReceiveRequest request, CancellationToken cancellationToken)
    {
        var receivablePOIds = await _context.PurchaseOrderItem
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .GroupBy(x => x.PurchaseOrderId)
            .Select(g => new
            {
                PurchaseOrderId = g.Key,
                TotalOrdered = g.Sum(x => x.Quantity ?? 0.0),
                TotalReceived = _context.GoodsReceive
                    .AsNoTracking()
                    .ApplyIsDeletedFilter(false)
                    .Where(gr => gr.PurchaseOrderId == g.Key)
                    .SelectMany(gr => _context.InventoryTransaction
                        .AsNoTracking()
                        .ApplyIsDeletedFilter(false)
                        .Where(it => it.ModuleName == nameof(GoodsReceive) && it.ModuleId == gr.Id))
                    .Sum(it => it.Movement ?? 0.0)
            })
            .Where(x => x.TotalOrdered > 0 && x.TotalReceived < x.TotalOrdered)
            .Select(x => x.PurchaseOrderId!)
            .ToListAsync(cancellationToken);

        var entities = await _context.PurchaseOrder
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Include(x => x.Vendor)
            .Include(x => x.Tax)
            .Where(x => receivablePOIds.Contains(x.Id)
                && x.OrderStatus != PurchaseOrderStatus.Draft
                && x.OrderStatus != PurchaseOrderStatus.Cancelled
                && x.OrderStatus != PurchaseOrderStatus.Archived)
            .Take(500)
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GetPurchaseOrderListDto>>(entities);

        return new GetPurchaseOrderListForReceiveResult { Data = dtos };
    }
}
