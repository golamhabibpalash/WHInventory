using Application.Common.Extensions;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.InventoryTransactionManager;

public partial class InventoryTransactionService
{
    private async Task ValidateGoodsReceiveQuantity(
        string? goodsReceiveId,
        string? productId,
        double? movement,
        string? excludeTransactionId,
        CancellationToken cancellationToken)
    {
        var goodsReceive = await _queryContext.GoodsReceive
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == goodsReceiveId, cancellationToken);

        if (goodsReceive?.PurchaseOrderId == null)
            return;

        var purchaseOrderId = goodsReceive.PurchaseOrderId;

        var orderedQty = await _queryContext.PurchaseOrderItem
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(x => x.PurchaseOrderId == purchaseOrderId && x.ProductId == productId)
            .SumAsync(x => x.Quantity ?? 0.0, cancellationToken);

        if (orderedQty <= 0)
        {
            var productName = await _queryContext.Product
                .Where(x => x.Id == productId)
                .Select(x => x.Name)
                .FirstOrDefaultAsync(cancellationToken) ?? productId;
            throw new Exception($"Product '{productName}' is not listed in the linked Purchase Order.");
        }

        var goodsReceiveIds = await _queryContext.GoodsReceive
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(x => x.PurchaseOrderId == purchaseOrderId)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var receivedQuery = _queryContext.InventoryTransaction
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(x => x.ModuleId != null && goodsReceiveIds.Contains(x.ModuleId)
                     && x.ModuleName == nameof(GoodsReceive)
                     && x.ProductId == productId);

        if (excludeTransactionId != null)
            receivedQuery = receivedQuery.Where(x => x.Id != excludeTransactionId);

        var alreadyReceived = await receivedQuery.SumAsync(x => x.Movement ?? 0.0, cancellationToken);
        var remaining = orderedQty - alreadyReceived;

        if ((movement ?? 0.0) > remaining)
        {
            var productName = await _queryContext.Product
                .Where(x => x.Id == productId)
                .Select(x => x.Name)
                .FirstOrDefaultAsync(cancellationToken) ?? productId;
            throw new Exception(
                $"Over-receive not allowed for '{productName}'. " +
                $"Ordered: {orderedQty}, Already received: {alreadyReceived}, Remaining: {remaining}, Requested: {movement}.");
        }
    }

    public async Task<InventoryTransaction> GoodsReceiveCreateInvenTrans(
        string? moduleId,
        string? warehouseId,
        string? productId,
        double? movement,
        string? createdById,
        CancellationToken cancellationToken = default
        )
    {
        var parent = await _queryContext
            .GoodsReceive
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == moduleId, cancellationToken);

        if (parent == null)
        {
            throw new Exception($"Parent entity not found: {moduleId}");
        }

        await ValidateGoodsReceiveQuantity(moduleId, productId, movement, null, cancellationToken);

        var child = new InventoryTransaction();
        child.CreatedById = createdById;

        child.Number = _numberSequenceService.GenerateNumber(nameof(InventoryTransaction), "", "IVT");
        child.ModuleId = parent.Id;
        child.ModuleName = nameof(GoodsReceive);
        child.ModuleCode = "GR";
        child.ModuleNumber = parent.Number;
        child.MovementDate = parent.ReceiveDate;
        child.Status = (InventoryTransactionStatus?)parent.Status;

        child.WarehouseId = warehouseId;
        child.ProductId = productId;
        child.Movement = movement;

        CalculateInvenTrans(child);

        await _inventoryTransactionRepository.CreateAsync(child, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return child;
    }

    public async Task<InventoryTransaction> GoodsReceiveUpdateInvenTrans(
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

        await ValidateGoodsReceiveQuantity(child.ModuleId, productId, movement, id, cancellationToken);

        child.UpdatedById = updatedById;

        child.WarehouseId = warehouseId;
        child.ProductId = productId;
        child.Movement = movement;

        CalculateInvenTrans(child);

        _inventoryTransactionRepository.Update(child);
        await _unitOfWork.SaveAsync(cancellationToken);

        return child;
    }

    public async Task<InventoryTransaction> GoodsReceiveDeleteInvenTrans(
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
    public async Task<List<InventoryTransaction>> GoodsReceiveGetInvenTransList(
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