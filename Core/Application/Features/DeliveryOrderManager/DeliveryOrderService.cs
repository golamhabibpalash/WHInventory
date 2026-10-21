using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Repositories;
using Application.Features.InventoryTransactionManager;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.DeliveryOrderManager;

/// <summary>
/// Shared delivery-order logic, so the ordinary delivery page and the sales-order "Deliver Now"
/// action create deliveries the same way. Delivered quantities are always recorded as
/// <see cref="InventoryTransaction"/> lines linked to the delivery order by
/// <c>ModuleName</c>/<c>ModuleId</c>; once the parent is <see cref="DeliveryOrderStatus.Confirmed"/>
/// those lines reduce stock for real.
/// </summary>
public class DeliveryOrderService
{
    private readonly ICommandRepository<DeliveryOrder> _deliveryOrderRepository;
    private readonly ICommandRepository<SalesOrderItem> _salesOrderItemRepository;
    private readonly IQueryContext _queryContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly NumberSequenceService _numberSequenceService;
    private readonly InventoryTransactionService _inventoryTransactionService;

    public DeliveryOrderService(
        ICommandRepository<DeliveryOrder> deliveryOrderRepository,
        ICommandRepository<SalesOrderItem> salesOrderItemRepository,
        IQueryContext queryContext,
        IUnitOfWork unitOfWork,
        NumberSequenceService numberSequenceService,
        InventoryTransactionService inventoryTransactionService
        )
    {
        _deliveryOrderRepository = deliveryOrderRepository;
        _salesOrderItemRepository = salesOrderItemRepository;
        _queryContext = queryContext;
        _unitOfWork = unitOfWork;
        _numberSequenceService = numberSequenceService;
        _inventoryTransactionService = inventoryTransactionService;
    }

    /// <summary>
    /// Ordered quantity per product on the sales order, minus everything already recorded as a
    /// delivery line for that order (across all of its delivery orders), i.e. what is still left
    /// to ship. Products with nothing remaining are left out.
    /// </summary>
    public async Task<List<RemainingDeliveryLine>> GetRemainingLinesAsync(
        string? salesOrderId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(salesOrderId))
            return new List<RemainingDeliveryLine>();

        var orderedLines = await _queryContext.SalesOrderItem
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(x => x.SalesOrderId == salesOrderId && x.Product!.Physical == true)
            .GroupBy(x => x.ProductId)
            .Select(g => new { ProductId = g.Key, Ordered = g.Sum(x => x.Quantity ?? 0.0) })
            .ToListAsync(cancellationToken);

        if (orderedLines.Count == 0)
            return new List<RemainingDeliveryLine>();

        var deliveryOrderIds = await _queryContext.DeliveryOrder
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(x => x.SalesOrderId == salesOrderId)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var deliveredByProduct = new Dictionary<string, double>();
        if (deliveryOrderIds.Count > 0)
        {
            var deliveredRows = await _queryContext.InventoryTransaction
                .AsNoTracking()
                .ApplyIsDeletedFilter(false)
                .Where(x => x.ModuleId != null && deliveryOrderIds.Contains(x.ModuleId)
                         && x.ModuleName == nameof(DeliveryOrder) && x.ProductId != null)
                .GroupBy(x => x.ProductId!)
                .Select(g => new { ProductId = g.Key, Delivered = g.Sum(x => x.Movement ?? 0.0) })
                .ToListAsync(cancellationToken);

            deliveredByProduct = deliveredRows.ToDictionary(x => x.ProductId, x => x.Delivered);
        }

        var result = new List<RemainingDeliveryLine>();
        foreach (var line in orderedLines)
        {
            if (line.ProductId == null) continue;

            var delivered = deliveredByProduct.GetValueOrDefault(line.ProductId, 0.0);
            var remaining = line.Ordered - delivered;
            if (remaining <= 0)
                continue;

            var name = await _queryContext.Product
                .Where(x => x.Id == line.ProductId)
                .Select(x => x.Name)
                .FirstOrDefaultAsync(cancellationToken) ?? line.ProductId;

            result.Add(new RemainingDeliveryLine
            {
                ProductId = line.ProductId,
                ProductName = name,
                OrderedQuantity = line.Ordered,
                DeliveredQuantity = delivered,
                RemainingQuantity = remaining
            });
        }

        return result;
    }

    /// <summary>
    /// Creates a delivery order and, for each physical product on the linked sales order,
    /// records a delivery line for that product's full ordered quantity — the behaviour of the
    /// ordinary "Add delivery order" page. Over-delivery is rejected by the inventory service.
    /// </summary>
    public async Task<DeliveryOrder> CreateAsync(
        DateTime? deliveryDate,
        DeliveryOrderStatus status,
        string? description,
        string? salesOrderId,
        string? createdById,
        CancellationToken cancellationToken = default)
    {
        var entity = new DeliveryOrder();
        entity.CreatedById = createdById;

        entity.Number = _numberSequenceService.GenerateNumber(nameof(DeliveryOrder), "", "DO");
        entity.DeliveryDate = deliveryDate;
        entity.Status = status;
        entity.Description = description;
        entity.SalesOrderId = salesOrderId;

        await _deliveryOrderRepository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        var warehouseId = await GetDefaultWarehouseIdAsync(cancellationToken);
        if (warehouseId != null)
        {
            var items = await _salesOrderItemRepository
                .GetQuery()
                .ApplyIsDeletedFilter(false)
                .Where(x => x.SalesOrderId == entity.SalesOrderId)
                .Include(x => x.Product)
                .ToListAsync(cancellationToken);

            foreach (var item in items)
            {
                if (item?.Product?.Physical ?? false)
                {
                    await _inventoryTransactionService.DeliveryOrderCreateInvenTrans(
                        entity.Id,
                        warehouseId,
                        item.ProductId,
                        item.Quantity,
                        entity.CreatedById,
                        cancellationToken
                        );
                }
            }
        }

        return entity;
    }

    /// <summary>
    /// Creates a <see cref="DeliveryOrderStatus.Confirmed"/> delivery for whatever is still
    /// outstanding on the sales order, recording one line per product at its remaining quantity.
    /// Every line's warehouse and stock is resolved and checked up front, before anything is
    /// written, so a shortage aborts with nothing persisted. When nothing remains, no delivery is
    /// created and <c>null</c> is returned.
    /// </summary>
    public async Task<DeliveryOrder?> DeliverRemainingAsync(
        DateTime? deliveryDate,
        string? description,
        string? salesOrderId,
        string? createdById,
        CancellationToken cancellationToken = default)
    {
        var remainingLines = await GetRemainingLinesAsync(salesOrderId, cancellationToken);
        if (remainingLines.Count == 0)
            return null;

        // Resolve every line's source warehouse first; fail on shortage before writing anything.
        var plan = new List<(RemainingDeliveryLine Line, string WarehouseId)>();
        foreach (var line in remainingLines)
        {
            var warehouseId = await GetWarehouseWithStockAsync(line.ProductId, line.RemainingQuantity, cancellationToken);
            if (warehouseId == null)
            {
                throw new ValidationException(
                    $"Insufficient stock for '{line.ProductName}'. " +
                    $"Required: {line.RemainingQuantity:N2}, none available in any warehouse.");
            }

            plan.Add((line, warehouseId));
        }

        var entity = new DeliveryOrder();
        entity.CreatedById = createdById;

        entity.Number = _numberSequenceService.GenerateNumber(nameof(DeliveryOrder), "", "DO");
        entity.DeliveryDate = deliveryDate;
        entity.Status = DeliveryOrderStatus.Confirmed;
        entity.Description = description;
        entity.SalesOrderId = salesOrderId;

        await _deliveryOrderRepository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        var createdLineIds = new List<string>();
        try
        {
            foreach (var (line, warehouseId) in plan)
            {
                var child = await _inventoryTransactionService.DeliveryOrderCreateInvenTrans(
                    entity.Id,
                    warehouseId,
                    line.ProductId,
                    line.RemainingQuantity,
                    entity.CreatedById,
                    cancellationToken
                    );
                if (child.Id != null)
                    createdLineIds.Add(child.Id);
            }
        }
        catch
        {
            // No shared transaction boundary is available, so compensate: remove the lines and the
            // delivery order this call created, leaving no half-recorded delivery behind.
            foreach (var id in createdLineIds)
            {
                await _inventoryTransactionService.DeliveryOrderDeleteInvenTrans(id, createdById, cancellationToken);
            }

            _deliveryOrderRepository.Delete(entity);
            await _unitOfWork.SaveAsync(cancellationToken);

            throw;
        }

        return entity;
    }

    /// <summary>First real (non-system) warehouse — matches the ordinary delivery page.</summary>
    private async Task<string?> GetDefaultWarehouseIdAsync(CancellationToken cancellationToken)
    {
        return await _queryContext.Warehouse
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(x => x.SystemWarehouse == false)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// First real warehouse holding at least <paramref name="requiredQuantity"/> of the product,
    /// preferring one with enough stock so an immediate delivery can be confirmed.
    /// </summary>
    private async Task<string?> GetWarehouseWithStockAsync(string? productId, double requiredQuantity, CancellationToken cancellationToken)
    {
        var warehouses = await _queryContext.Warehouse
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(x => x.SystemWarehouse == false)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (warehouses.Count == 0)
            return null;

        var stockByWarehouse = await _queryContext.InventoryTransaction
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(x => x.Status == InventoryTransactionStatus.Confirmed
                     && x.ProductId == productId
                     && x.WarehouseId != null
                     && warehouses.Contains(x.WarehouseId))
            .GroupBy(x => x.WarehouseId!)
            .Select(g => new { WarehouseId = g.Key, Stock = g.Sum(x => x.Stock ?? 0.0) })
            .ToListAsync(cancellationToken);

        var best = stockByWarehouse
            .Where(x => x.Stock >= requiredQuantity)
            .OrderByDescending(x => x.Stock)
            .FirstOrDefault();

        if (best != null)
            return best.WarehouseId;

        // Nothing can fully cover it; fall back to the warehouse with the most stock so the
        // inventory service can reject with a precise "available vs requested" message.
        return stockByWarehouse
            .OrderByDescending(x => x.Stock)
            .Select(x => x.WarehouseId)
            .FirstOrDefault()
            ?? warehouses.First();
    }
}

public record RemainingDeliveryLine
{
    public string? ProductId { get; init; }
    public string? ProductName { get; init; }
    public double OrderedQuantity { get; init; }
    public double DeliveredQuantity { get; init; }
    public double RemainingQuantity { get; init; }
}
