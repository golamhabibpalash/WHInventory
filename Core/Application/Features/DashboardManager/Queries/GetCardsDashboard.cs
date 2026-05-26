using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.DashboardManager.Queries;


public class GetCardsDashboardDto
{
    public CardsItem? CardsDashboard { get; init; }
}

public class GetCardsDashboardResult
{
    public GetCardsDashboardDto? Data { get; init; }
}

public class GetCardsDashboardRequest : IRequest<GetCardsDashboardResult>
{
}

public class GetCardsDashboardHandler : IRequestHandler<GetCardsDashboardRequest, GetCardsDashboardResult>
{
    private readonly IQueryContext _context;

    public GetCardsDashboardHandler(IQueryContext context)
    {
        _context = context;
    }

    public async Task<GetCardsDashboardResult> Handle(GetCardsDashboardRequest request, CancellationToken cancellationToken)
    {
        var salesTotalTask = _context.SalesOrderItem
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .SumAsync(x => (double?)x.Quantity, cancellationToken);

        var salesReturnTotalTask = _context.InventoryTransaction
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(x => x.ModuleName == nameof(SalesReturn) && x.Status == InventoryTransactionStatus.Confirmed && x.Warehouse!.SystemWarehouse == false)
            .SumAsync(x => (double?)x.Movement, cancellationToken);

        var purchaseTotalTask = _context.PurchaseOrderItem
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .SumAsync(x => (double?)x.Quantity, cancellationToken);

        var purchaseReturnTotalTask = _context.InventoryTransaction
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(x => x.ModuleName == nameof(PurchaseReturn) && x.Status == InventoryTransactionStatus.Confirmed && x.Warehouse!.SystemWarehouse == false)
            .SumAsync(x => (double?)x.Movement, cancellationToken);

        var deliveryOrderTotalTask = _context.InventoryTransaction
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(x => x.ModuleName == nameof(DeliveryOrder) && x.Status == InventoryTransactionStatus.Confirmed && x.Warehouse!.SystemWarehouse == false)
            .SumAsync(x => (double?)x.Movement, cancellationToken);

        var goodsReceiveTotalTask = _context.InventoryTransaction
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(x => x.ModuleName == nameof(GoodsReceive) && x.Status == InventoryTransactionStatus.Confirmed && x.Warehouse!.SystemWarehouse == false)
            .SumAsync(x => (double?)x.Movement, cancellationToken);

        var transferOutTotalTask = _context.InventoryTransaction
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(x => x.ModuleName == nameof(TransferOut) && x.Status == InventoryTransactionStatus.Confirmed && x.Warehouse!.SystemWarehouse == false)
            .SumAsync(x => (double?)x.Movement, cancellationToken);

        var transferInTotalTask = _context.InventoryTransaction
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(x => x.ModuleName == nameof(TransferIn) && x.Status == InventoryTransactionStatus.Confirmed && x.Warehouse!.SystemWarehouse == false)
            .SumAsync(x => (double?)x.Movement, cancellationToken);

        await Task.WhenAll(salesTotalTask, salesReturnTotalTask, purchaseTotalTask, purchaseReturnTotalTask,
            deliveryOrderTotalTask, goodsReceiveTotalTask, transferOutTotalTask, transferInTotalTask);

        var cardsDashboardData = new CardsItem
        {
            SalesTotal = salesTotalTask.Result,
            SalesReturnTotal = salesReturnTotalTask.Result,
            PurchaseTotal = purchaseTotalTask.Result,
            PurchaseReturnTotal = purchaseReturnTotalTask.Result,
            DeliveryOrderTotal = deliveryOrderTotalTask.Result,
            GoodsReceiveTotal = goodsReceiveTotalTask.Result,
            TransferOutTotal = transferOutTotalTask.Result,
            TransferInTotal = transferInTotalTask.Result
        };



        var result = new GetCardsDashboardResult
        {
            Data = new GetCardsDashboardDto
            {
                CardsDashboard = cardsDashboardData
            }
        };

        return result;
    }
}
