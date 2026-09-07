using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.SalesOrderManager;

public class SalesOrderService
{
    private readonly ICommandRepository<SalesOrder> _salesOrderRepository;
    private readonly ICommandRepository<SalesOrderItem> _salesOrderItemRepository;
    private readonly IQueryContext _queryContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SalesOrderService> _logger;

    public SalesOrderService(
        ICommandRepository<SalesOrder> salesOrderRepository,
        ICommandRepository<SalesOrderItem> salesOrderItemRepository,
        IQueryContext queryContext,
        IUnitOfWork unitOfWork,
        ILogger<SalesOrderService> logger
        )
    {
        _salesOrderRepository = salesOrderRepository;
        _salesOrderItemRepository = salesOrderItemRepository;
        _queryContext = queryContext;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Blocks a sales order line whose quantity would exceed what can actually be promised for the
    /// product: on-hand stock minus everything already committed to other open, not-yet-delivered
    /// sales order lines (siblings on the same order included). There is no company-level override —
    /// a physical product that is not available cannot be sold.
    /// <paramref name="excludeSalesOrderItemId"/> is the line being edited, so an update is measured
    /// against its siblings only; pass null on create.
    /// </summary>
    public async Task EnsureAvailableToPromiseAsync(
        string? productId,
        string? salesOrderId,
        double? requestedQuantity,
        string? excludeSalesOrderItemId,
        string? userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(productId))
            return;

        var product = await _queryContext.Product
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(x => x.Id == productId)
            .Select(x => new { x.Name, x.Physical })
            .FirstOrDefaultAsync(cancellationToken);

        // Non-stock (service) products are not constrained by inventory.
        if (product == null || product.Physical != true)
            return;

        var requested = requestedQuantity ?? 0.0;

        // On hand: confirmed ledger movement across real (non-system) warehouses. Once a delivery
        // is confirmed its negative movement is already reflected here.
        var onHand = await _queryContext.InventoryTransaction
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(x =>
                x.Status == InventoryTransactionStatus.Confirmed &&
                x.ProductId == productId &&
                x.Warehouse != null &&
                x.Warehouse.SystemWarehouse == false)
            .SumAsync(x => (double?)x.Stock ?? 0.0, cancellationToken);

        // Sales orders that already have a confirmed delivery — their stock reduction is in
        // onHand above, so their lines must not be counted again as open demand.
        var deliveredSalesOrderIds = _queryContext.DeliveryOrder
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(x => x.Status == DeliveryOrderStatus.Confirmed && x.SalesOrderId != null)
            .Select(x => x.SalesOrderId);

        // Committed: this product's quantity on every other open (Draft/Confirmed), not-yet-delivered
        // sales order line, including other lines on the current order.
        var committed = await _queryContext.SalesOrderItem
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(x =>
                x.ProductId == productId &&
                x.Id != excludeSalesOrderItemId &&
                x.SalesOrder != null &&
                (x.SalesOrder.OrderStatus == SalesOrderStatus.Draft ||
                 x.SalesOrder.OrderStatus == SalesOrderStatus.Confirmed) &&
                !deliveredSalesOrderIds.Contains(x.SalesOrderId))
            .SumAsync(x => x.Quantity ?? 0.0, cancellationToken);

        var availableToPromise = onHand - committed;

        if (requested > availableToPromise)
        {
            _logger.LogWarning(
                "Stock availability check failed on SalesOrderItem. UserId={UserId} SalesOrderId={SalesOrderId} ProductId={ProductId} ProductName={ProductName} OnHand={OnHand} Committed={Committed} AvailableToPromise={AvailableToPromise} Requested={Requested}",
                userId, salesOrderId, productId, product.Name, onHand, committed, availableToPromise, requested);

            throw new Exception(
                $"Insufficient stock for '{product.Name}'. " +
                $"On hand: {onHand:N2}, committed to other open orders: {committed:N2}, " +
                $"available to promise: {availableToPromise:N2}, requested: {requested:N2}. " +
                $"Cannot save this Sales Order item.");
        }
    }

    public async Task Recalculate(string salesOrderId)
    {
        var salesOrder = await _salesOrderRepository
            .GetQuery()
            .ApplyIsDeletedFilter()
            .Where(x => x.Id == salesOrderId)
            .Include(x => x.Tax)
            .SingleOrDefaultAsync();

        if (salesOrder == null)
            return;

        var salesOrderItems = await _salesOrderItemRepository
            .GetQuery()
            .ApplyIsDeletedFilter()
            .Where(x => x.SalesOrderId == salesOrderId)
            .ToListAsync();

        salesOrder.BeforeTaxAmount = salesOrderItems.Sum(x => x.Total ?? 0).ToMoney();

        var taxPercentage = salesOrder.Tax?.Percentage ?? 0;
        salesOrder.TaxAmount = ((salesOrder.BeforeTaxAmount ?? 0) * taxPercentage / 100).ToMoney();

        salesOrder.AfterTaxAmount = ((salesOrder.BeforeTaxAmount ?? 0) + (salesOrder.TaxAmount ?? 0)).ToMoney();

        _salesOrderRepository.Update(salesOrder);
        await _unitOfWork.SaveAsync();
    }
}
