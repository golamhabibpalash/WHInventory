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
    /// Stock position of a product for selling: confirmed on-hand stock across real (non-system)
    /// warehouses, the quantity committed to open (Draft/Confirmed), not-yet-delivered sales order
    /// lines, and what is left to promise. Lines of <paramref name="excludeSalesOrderId"/> and the
    /// single line <paramref name="excludeSalesOrderItemId"/> are left out of the committed figure.
    /// Returns null for an unknown product.
    /// </summary>
    public async Task<ProductAvailability?> GetAvailabilityAsync(
        string? productId,
        string? excludeSalesOrderId,
        string? excludeSalesOrderItemId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(productId))
            return null;

        var product = await _queryContext.Product
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(x => x.Id == productId)
            .Select(x => new { x.Name, x.Physical })
            .FirstOrDefaultAsync(cancellationToken);

        if (product == null)
            return null;

        // Non-stock (service) products are not constrained by inventory.
        if (product.Physical != true)
            return new ProductAvailability { ProductId = productId, ProductName = product.Name, Physical = false };

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
        // sales order line.
        var committedLines = await _queryContext.SalesOrderItem
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(x =>
                x.ProductId == productId &&
                x.Id != excludeSalesOrderItemId &&
                x.SalesOrderId != excludeSalesOrderId &&
                x.SalesOrder != null &&
                (x.SalesOrder.OrderStatus == SalesOrderStatus.Draft ||
                 x.SalesOrder.OrderStatus == SalesOrderStatus.Confirmed) &&
                !deliveredSalesOrderIds.Contains(x.SalesOrderId))
            .GroupBy(x => x.SalesOrder!.OrderStatus)
            .Select(g => new { Status = g.Key, Quantity = g.Sum(x => x.Quantity ?? 0.0) })
            .ToListAsync(cancellationToken);

        var draft = committedLines.Where(x => x.Status == SalesOrderStatus.Draft).Sum(x => x.Quantity);
        var confirmed = committedLines.Where(x => x.Status == SalesOrderStatus.Confirmed).Sum(x => x.Quantity);

        return new ProductAvailability
        {
            ProductId = productId,
            ProductName = product.Name,
            Physical = true,
            OnHand = onHand,
            CommittedDraft = draft,
            CommittedConfirmed = confirmed
        };
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
        var availability = await GetAvailabilityAsync(productId, null, excludeSalesOrderItemId, cancellationToken);

        if (availability == null || !availability.Physical)
            return;

        var requested = requestedQuantity ?? 0.0;
        var onHand = availability.OnHand;
        var committed = availability.Committed;
        var availableToPromise = availability.AvailableToPromise;

        if (requested > availableToPromise)
        {
            _logger.LogWarning(
                "Stock availability check failed on SalesOrderItem. UserId={UserId} SalesOrderId={SalesOrderId} ProductId={ProductId} ProductName={ProductName} OnHand={OnHand} Committed={Committed} AvailableToPromise={AvailableToPromise} Requested={Requested}",
                userId, salesOrderId, productId, availability.ProductName, onHand, committed, availableToPromise, requested);

            throw new Exception(
                $"Insufficient stock for '{availability.ProductName}'. " +
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

        salesOrder.SubTotalAmount = salesOrderItems.Sum(x => (x.UnitPrice ?? 0) * (x.Quantity ?? 0)).ToMoney();
        salesOrder.DiscountAmount = salesOrderItems.Sum(x => (x.UnitPrice ?? 0) * (x.Quantity ?? 0) * (x.CommissionRate ?? 0) / 100.0).ToMoney();
        salesOrder.BeforeTaxAmount = ((salesOrder.SubTotalAmount ?? 0) - (salesOrder.DiscountAmount ?? 0)).ToMoney();

        var taxPercentage = salesOrder.Tax?.Percentage ?? 0;
        salesOrder.TaxAmount = ((salesOrder.BeforeTaxAmount ?? 0) * taxPercentage / 100).ToMoney();

        salesOrder.AfterTaxAmount = ((salesOrder.BeforeTaxAmount ?? 0) + (salesOrder.TaxAmount ?? 0)).ToMoney();

        _salesOrderRepository.Update(salesOrder);
        await _unitOfWork.SaveAsync();
    }
}

public record ProductAvailability
{
    public string? ProductId { get; init; }
    public string? ProductName { get; init; }
    public bool Physical { get; init; }
    public double OnHand { get; init; }

    /// <summary>Quantity on open Draft sales orders not yet delivered.</summary>
    public double CommittedDraft { get; init; }

    /// <summary>Quantity on Confirmed sales orders not yet delivered.</summary>
    public double CommittedConfirmed { get; init; }

    public double Committed => CommittedDraft + CommittedConfirmed;
    public double AvailableToPromise => OnHand - Committed;
}
