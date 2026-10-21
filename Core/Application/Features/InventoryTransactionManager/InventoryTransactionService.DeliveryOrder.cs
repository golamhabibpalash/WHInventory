using Application.Common.Extensions;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.InventoryTransactionManager;

public partial class InventoryTransactionService
{
    private async Task ValidateDeliveryOrderQuantity(
        string? deliveryOrderId,
        string? productId,
        double? movement,
        string? excludeTransactionId,
        CancellationToken cancellationToken)
    {
        var deliveryOrder = await _queryContext.DeliveryOrder
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == deliveryOrderId, cancellationToken);

        if (deliveryOrder?.SalesOrderId == null)
            return;

        var salesOrderId = deliveryOrder.SalesOrderId;

        var orderedQty = await _queryContext.SalesOrderItem
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(x => x.SalesOrderId == salesOrderId && x.ProductId == productId)
            .SumAsync(x => x.Quantity ?? 0.0, cancellationToken);

        if (orderedQty <= 0)
        {
            var productName = await _queryContext.Product
                .Where(x => x.Id == productId)
                .Select(x => x.Name)
                .FirstOrDefaultAsync(cancellationToken) ?? productId;
            throw new ValidationException($"Product '{productName}' is not listed in the linked Sales Order.");
        }

        var deliveryOrderIds = await _queryContext.DeliveryOrder
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(x => x.SalesOrderId == salesOrderId)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var deliveredQuery = _queryContext.InventoryTransaction
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(x => x.ModuleId != null && deliveryOrderIds.Contains(x.ModuleId)
                     && x.ModuleName == nameof(DeliveryOrder)
                     && x.ProductId == productId);

        if (excludeTransactionId != null)
            deliveredQuery = deliveredQuery.Where(x => x.Id != excludeTransactionId);

        var alreadyDelivered = await deliveredQuery.SumAsync(x => x.Movement ?? 0.0, cancellationToken);
        var remaining = orderedQty - alreadyDelivered;

        if ((movement ?? 0.0) > remaining)
        {
            var productName = await _queryContext.Product
                .Where(x => x.Id == productId)
                .Select(x => x.Name)
                .FirstOrDefaultAsync(cancellationToken) ?? productId;
            throw new ValidationException(
                $"Delivered quantity exceeds ordered quantity for '{productName}'. " +
                $"Ordered: {orderedQty}, Already delivered: {alreadyDelivered}, Remaining: {remaining}, Requested: {movement}.");
        }
    }

    public async Task<InventoryTransaction> DeliveryOrderCreateInvenTrans(
        string? moduleId,
        string? warehouseId,
        string? productId,
        double? movement,
        string? createdById,
        CancellationToken cancellationToken = default
        )
    {
        var parent = await _queryContext
            .DeliveryOrder
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == moduleId, cancellationToken);

        if (parent == null)
        {
            throw new ValidationException($"Parent entity not found: {moduleId}");
        }

        await ValidateDeliveryOrderQuantity(moduleId, productId, movement, null, cancellationToken);

        var child = new InventoryTransaction();
        child.CreatedById = createdById;

        child.Number = _numberSequenceService.GenerateNumber(nameof(InventoryTransaction), "", "IVT");
        child.ModuleId = parent.Id;
        child.ModuleName = nameof(DeliveryOrder);
        child.ModuleCode = "DO";
        child.ModuleNumber = parent.Number;
        child.MovementDate = parent.DeliveryDate;
        child.Status = (InventoryTransactionStatus?)parent.Status;

        child.WarehouseId = warehouseId;
        child.ProductId = productId;
        child.Movement = movement;

        if (parent.Status == DeliveryOrderStatus.Confirmed)
        {
            ValidateOutMovementStock(warehouseId, productId, movement);
        }

        CalculateInvenTrans(child);

        await _inventoryTransactionRepository.CreateAsync(child, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return child;
    }

    public async Task<InventoryTransaction> DeliveryOrderUpdateInvenTrans(
        string? id,
        string? warehouseId,
        string? productId,
        double? movement,
        string? updatedById,
        CancellationToken cancellationToken = default
        )
    {
        var child = await _inventoryTransactionRepository.GetAsync(id ?? string.Empty, cancellationToken);

        if (child == null)
        {
            throw new Exception($"Child entity not found: {id}");
        }

        await ValidateDeliveryOrderQuantity(child.ModuleId, productId, movement, id, cancellationToken);

        child.UpdatedById = updatedById;

        child.WarehouseId = warehouseId;
        child.ProductId = productId;
        child.Movement = movement;

        if (child.Status == InventoryTransactionStatus.Confirmed)
        {
            ValidateOutMovementStock(warehouseId, productId, movement, child.Id);
        }

        CalculateInvenTrans(child);

        _inventoryTransactionRepository.Update(child);
        await _unitOfWork.SaveAsync(cancellationToken);

        return child;
    }

    public async Task<InventoryTransaction> DeliveryOrderDeleteInvenTrans(
        string? id,
        string? updatedById,
        CancellationToken cancellationToken = default
        )
    {
        var child = await _inventoryTransactionRepository.GetAsync(id ?? string.Empty, cancellationToken);

        if (child == null)
        {
            throw new Exception($"Child entity not found: {id}");
        }

        child.UpdatedById = updatedById;

        _inventoryTransactionRepository.Delete(child);
        await _unitOfWork.SaveAsync(cancellationToken);

        return child;
    }
    public async Task<List<InventoryTransaction>> DeliveryOrderGetInvenTransList(
        string? moduleId,
        string? moduleName,
        CancellationToken cancellationToken = default
        )
    {
        var childs = await _queryContext
            .InventoryTransaction
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(x => x.ModuleId == moduleId && x.ModuleName == moduleName)
            .ToListAsync(cancellationToken);

        return childs;
    }
}
