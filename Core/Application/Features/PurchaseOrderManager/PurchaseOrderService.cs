using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.PurchaseOrderManager;

public class PurchaseOrderService
{
    private readonly ICommandRepository<PurchaseOrder> _purchaseOrderRepository;
    private readonly ICommandRepository<PurchaseOrderItem> _purchaseOrderItemRepository;
    private readonly IQueryContext _queryContext;
    private readonly IUnitOfWork _unitOfWork;

    public PurchaseOrderService(
        ICommandRepository<PurchaseOrder> purchaseOrderRepository,
        ICommandRepository<PurchaseOrderItem> purchaseOrderItemRepository,
        IQueryContext queryContext,
        IUnitOfWork unitOfWork
        )
    {
        _purchaseOrderRepository = purchaseOrderRepository;
        _purchaseOrderItemRepository = purchaseOrderItemRepository;
        _queryContext = queryContext;
        _unitOfWork = unitOfWork;
    }

    public async Task Recalculate(string purchaseOrderId)
    {
        var purchaseOrder = await _purchaseOrderRepository
            .GetQuery()
            .ApplyIsDeletedFilter()
            .Where(x => x.Id == purchaseOrderId)
            .Include(x => x.Tax)
            .SingleOrDefaultAsync();

        if (purchaseOrder == null)
            return;

        var purchaseOrderItems = await _purchaseOrderItemRepository
            .GetQuery()
            .ApplyIsDeletedFilter()
            .Where(x => x.PurchaseOrderId == purchaseOrderId)
            .ToListAsync();

        purchaseOrder.BeforeTaxAmount = purchaseOrderItems.Sum(x => x.Total ?? 0);

        var taxPercentage = purchaseOrder.Tax?.Percentage ?? 0;
        purchaseOrder.TaxAmount = (purchaseOrder.BeforeTaxAmount ?? 0) * taxPercentage / 100;

        purchaseOrder.AfterTaxAmount = (purchaseOrder.BeforeTaxAmount ?? 0) + (purchaseOrder.TaxAmount ?? 0);

        _purchaseOrderRepository.Update(purchaseOrder);
        await _unitOfWork.SaveAsync();
    }

    public async Task RecalculatePOStatus(string? purchaseOrderId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(purchaseOrderId))
            return;

        var purchaseOrder = await _purchaseOrderRepository
            .GetQuery()
            .ApplyIsDeletedFilter()
            .Where(x => x.Id == purchaseOrderId)
            .SingleOrDefaultAsync(cancellationToken);

        if (purchaseOrder == null)
            return;

        if (purchaseOrder.OrderStatus == PurchaseOrderStatus.Draft ||
            purchaseOrder.OrderStatus == PurchaseOrderStatus.Cancelled ||
            purchaseOrder.OrderStatus == PurchaseOrderStatus.Archived)
            return;

        var totalOrdered = await _queryContext.PurchaseOrderItem
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(x => x.PurchaseOrderId == purchaseOrderId)
            .SumAsync(x => x.Quantity ?? 0.0, cancellationToken);

        if (totalOrdered <= 0)
            return;

        var goodsReceiveIds = await _queryContext.GoodsReceive
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(x => x.PurchaseOrderId == purchaseOrderId)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var totalReceived = 0.0;
        if (goodsReceiveIds.Count > 0)
        {
            totalReceived = await _queryContext.InventoryTransaction
                .AsNoTracking()
                .ApplyIsDeletedFilter(false)
                .Where(x => x.ModuleName == nameof(GoodsReceive) && goodsReceiveIds.Contains(x.ModuleId!))
                .SumAsync(x => x.Movement ?? 0.0, cancellationToken);
        }

        if (totalReceived <= 0)
            purchaseOrder.OrderStatus = PurchaseOrderStatus.Open;
        else if (totalReceived < totalOrdered)
            purchaseOrder.OrderStatus = PurchaseOrderStatus.PartiallyReceived;
        else
            purchaseOrder.OrderStatus = PurchaseOrderStatus.Closed;

        _purchaseOrderRepository.Update(purchaseOrder);
        await _unitOfWork.SaveAsync(cancellationToken);
    }
}
