using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Repositories;
using Application.Features.NumberSequenceManager;
using Application.Features.PurchaseOrderManager;
using Application.Features.WarehouseManager;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.InventoryTransactionManager;

public partial class InventoryTransactionService
{
    private readonly NumberSequenceService _numberSequenceService;
    private readonly WarehouseService _warehouseService;
    private readonly IQueryContext _queryContext;
    private readonly ICommandRepository<InventoryTransaction> _inventoryTransactionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly PurchaseOrderService _purchaseOrderService;

    public InventoryTransactionService(
        NumberSequenceService numberSequenceService,
        WarehouseService warehouseService,
        IQueryContext queryContext,
        ICommandRepository<InventoryTransaction> inventoryTransactionRepository,
        IUnitOfWork unitOfWork,
        PurchaseOrderService purchaseOrderService
        )
    {
        _numberSequenceService = numberSequenceService;
        _warehouseService = warehouseService;
        _queryContext = queryContext;
        _inventoryTransactionRepository = inventoryTransactionRepository;
        _unitOfWork = unitOfWork;
        _purchaseOrderService = purchaseOrderService;
    }

    public double GetStock(string? warehouseId, string? productId, string? currentId = null)
    {
        var baseQuery = _queryContext.InventoryTransaction
            .ApplyIsDeletedFilter(false)
            .Where(x =>
                x.Status == InventoryTransactionStatus.Confirmed &&
                x.WarehouseId == warehouseId &&
                x.ProductId == productId);

        if (currentId != null)
            baseQuery = baseQuery.Where(x => x.Id != currentId);

        var result = baseQuery.Sum(x => x.Stock ?? 0.0);
        return result;
    }

    public void ValidateOutMovementStock(string? warehouseId, string? productId, double? movement, string? excludeId = null)
    {
        if ((movement ?? 0.0) <= 0) return;

        var available = GetStock(warehouseId, productId, excludeId);
        if (available < movement)
        {
            var productName = _queryContext.Product
                .Where(x => x.Id == productId)
                .Select(x => x.Name)
                .FirstOrDefault() ?? productId;
            var warehouseName = _queryContext.Warehouse
                .Where(x => x.Id == warehouseId)
                .Select(x => x.Name)
                .FirstOrDefault() ?? warehouseId;
            throw new ValidationException(
                $"Insufficient stock for '{productName}' in warehouse '{warehouseName}'. " +
                $"Available: {available}, Requested: {movement}.");
        }
    }

    public async Task PropagateParentUpdate(
        string? moduleId,
        string? moduleName,
        DateTime? movementDate,
        InventoryTransactionStatus? status,
        bool? isDeleted,
        string? updatedId,
        string? warehouseId = null,
        CancellationToken cancellationToken = default
        )
    {
        var childs = await _inventoryTransactionRepository
            .GetQuery()
            .Where(x => x.ModuleId == moduleId && x.ModuleName == moduleName)
            .ToListAsync(cancellationToken);

        // When confirming, validate that Out-type transactions have sufficient stock.
        // Only validate children that are transitioning to Confirmed (currently non-Confirmed)
        // or whose warehouse is being changed while already Confirmed.
        if (status == InventoryTransactionStatus.Confirmed)
        {
            var outChildsToValidate = childs
                .Where(c => !c.IsDeleted
                    && c.TransType == InventoryTransType.Out
                    && (c.Status != InventoryTransactionStatus.Confirmed
                        || (warehouseId != null && warehouseId != c.WarehouseId)))
                .ToList();

            if (outChildsToValidate.Any())
            {
                var groups = outChildsToValidate.GroupBy(c => new
                {
                    EffectiveWarehouseId = warehouseId ?? c.WarehouseId,
                    c.ProductId
                });

                foreach (var group in groups)
                {
                    var totalMovement = group.Sum(c => Math.Abs(c.Movement ?? 0.0));
                    ValidateOutMovementStock(group.Key.EffectiveWarehouseId, group.Key.ProductId, totalMovement);
                }
            }
        }

        foreach (var item in childs)
        {
            item.MovementDate = movementDate;
            item.Status = status;
            item.IsDeleted = isDeleted ?? false;
            item.UpdatedById = updatedId;
            item.UpdatedAtUtc = DateTime.UtcNow;
            if (warehouseId != null)
            {
                item.WarehouseId = warehouseId;
            }
        }

        await _unitOfWork.SaveAsync(cancellationToken);
    }


    public InventoryTransaction CalculateInvenTrans(InventoryTransaction? transaction)
    {
        if (transaction == null)
        {
            throw new Exception("Inventory transaction is null");
        }

        var moduleName = transaction.ModuleName;

        if (moduleName != nameof(StockCount) && transaction.Movement <= 0.0)
        {
            throw new ValidationException("Quantity must not zero and should be positive.");
        }

        if (moduleName == nameof(StockCount) && transaction.QtySCCount <= 0.0)
        {
            throw new ValidationException("Quantity must not zero and should be positive.");
        }

        switch (moduleName)
        {
            case nameof(DeliveryOrder):
                DeliveryOrderProcessing(transaction);
                break;
            case nameof(GoodsReceive):
                GoodsReceiveProcessing(transaction);
                break;
            case nameof(SalesReturn):
                SalesReturnProcessing(transaction);
                break;
            case nameof(PurchaseReturn):
                PurchaseReturnProcessing(transaction);
                break;
            case nameof(TransferIn):
                TransferInProcessing(transaction);
                break;
            case nameof(TransferOut):
                TransferOutProcessing(transaction);
                break;
            case nameof(StockCount):
                StockCountProcessing(transaction);
                break;
            case nameof(NegativeAdjustment):
                AdjustmentMinusProcessing(transaction);
                break;
            case nameof(PositiveAdjustment):
                AdjustmentPlusProcessing(transaction);
                break;
            case nameof(Scrapping):
                ScrappingProcessing(transaction);
                break;
            default:
                break;
        }

        return transaction;
    }

    private void CalculateStock(InventoryTransaction transaction)
    {
        if (transaction == null)
        {
            throw new Exception("Inventory transaction is null");
        }

        transaction.Stock = transaction.Movement * (int)(transaction.TransType ?? 0.0);
    }

    // System warehouses are seeded per tenant; a missing one must fail with a clear message
    // rather than a NullReferenceException hidden behind the generic 500 handler.
    private string RequireSystemWarehouseId(Warehouse? warehouse, string warehouseName)
    {
        if (warehouse == null || warehouse.Id == null)
        {
            throw new ValidationException($"System warehouse '{warehouseName}' was not found. Please contact your administrator.");
        }

        return warehouse.Id;
    }

    private InventoryTransaction DeliveryOrderProcessing(InventoryTransaction transaction)
    {
        if (transaction == null)
        {
            throw new Exception("Inventory transaction is null");
        }

        transaction.TransType = InventoryTransType.Out;
        CalculateStock(transaction);
        transaction.WarehouseFromId = transaction.WarehouseId;
        transaction.WarehouseToId = RequireSystemWarehouseId(_warehouseService.GetCustomerWarehouse(), "Customer");

        return transaction;
    }

    private InventoryTransaction GoodsReceiveProcessing(InventoryTransaction transaction)
    {
        if (transaction == null)
        {
            throw new Exception("Inventory transaction is null");
        }

        transaction.TransType = InventoryTransType.In;
        CalculateStock(transaction);
        transaction.WarehouseFromId = RequireSystemWarehouseId(_warehouseService.GetVendorWarehouse(), "Vendor");
        transaction.WarehouseToId = transaction.WarehouseId;

        return transaction;
    }

    private InventoryTransaction SalesReturnProcessing(InventoryTransaction transaction)
    {
        if (transaction == null)
        {
            throw new Exception("Inventory transaction is null");
        }

        transaction.TransType = InventoryTransType.In;
        CalculateStock(transaction);
        transaction.WarehouseFromId = RequireSystemWarehouseId(_warehouseService.GetCustomerWarehouse(), "Customer");
        transaction.WarehouseToId = transaction.WarehouseId;

        return transaction;
    }

    private InventoryTransaction PurchaseReturnProcessing(InventoryTransaction transaction)
    {
        if (transaction == null)
        {
            throw new Exception("Inventory transaction is null");
        }

        transaction.TransType = InventoryTransType.Out;
        CalculateStock(transaction);
        transaction.WarehouseFromId = transaction.WarehouseId;
        transaction.WarehouseToId = RequireSystemWarehouseId(_warehouseService.GetVendorWarehouse(), "Vendor");

        return transaction;
    }

    private InventoryTransaction TransferInProcessing(InventoryTransaction transaction)
    {
        if (transaction == null)
        {
            throw new Exception("Inventory transaction is null");
        }

        transaction.TransType = InventoryTransType.In;
        CalculateStock(transaction);
        transaction.WarehouseFromId = RequireSystemWarehouseId(_warehouseService.GetTransferWarehouse(), "Transfer");
        transaction.WarehouseToId = transaction.WarehouseId;

        return transaction;
    }

    private InventoryTransaction TransferOutProcessing(InventoryTransaction transaction)
    {
        if (transaction == null)
        {
            throw new Exception("Inventory transaction is null");
        }

        transaction.TransType = InventoryTransType.Out;
        CalculateStock(transaction);
        transaction.WarehouseFromId = transaction.WarehouseId;
        transaction.WarehouseToId = RequireSystemWarehouseId(_warehouseService.GetTransferWarehouse(), "Transfer");

        return transaction;
    }

    private InventoryTransaction StockCountProcessing(InventoryTransaction transaction)
    {
        if (transaction == null)
        {
            throw new Exception("Inventory transaction is null");
        }

        transaction.QtySCSys = GetStock(transaction.WarehouseId, transaction.ProductId, transaction.Id);
        transaction.QtySCDelta = transaction.QtySCSys - transaction.QtySCCount;
        transaction.Movement = Math.Abs(transaction.QtySCDelta ?? 0.0);

        if (transaction.QtySCDelta < 0.0)
        {

            transaction.TransType = InventoryTransType.In;
            CalculateStock(transaction);
            transaction.WarehouseFromId = RequireSystemWarehouseId(_warehouseService.GetStockCountWarehouse(), "StockCount");
            transaction.WarehouseToId = transaction.WarehouseId;

        }
        else
        {

            transaction.TransType = InventoryTransType.Out;
            CalculateStock(transaction);
            transaction.WarehouseFromId = transaction.WarehouseId;
            transaction.WarehouseToId = RequireSystemWarehouseId(_warehouseService.GetStockCountWarehouse(), "StockCount");

        }

        return transaction;
    }

    private InventoryTransaction AdjustmentMinusProcessing(InventoryTransaction transaction)
    {
        if (transaction == null)
        {
            throw new Exception("Inventory transaction is null");
        }

        transaction.TransType = InventoryTransType.Out;
        CalculateStock(transaction);
        transaction.WarehouseFromId = transaction.WarehouseId;
        transaction.WarehouseToId = RequireSystemWarehouseId(_warehouseService.GetAdjustmentWarehouse(), "Adjustment");

        return transaction;
    }

    private InventoryTransaction AdjustmentPlusProcessing(InventoryTransaction transaction)
    {
        if (transaction == null)
        {
            throw new Exception("Inventory transaction is null");
        }

        transaction.TransType = InventoryTransType.In;
        CalculateStock(transaction);
        transaction.WarehouseFromId = RequireSystemWarehouseId(_warehouseService.GetAdjustmentWarehouse(), "Adjustment");
        transaction.WarehouseToId = transaction.WarehouseId;

        return transaction;
    }

    private InventoryTransaction ScrappingProcessing(InventoryTransaction transaction)
    {
        if (transaction == null)
        {
            throw new Exception("Inventory transaction is null");
        }

        transaction.TransType = InventoryTransType.Out;
        CalculateStock(transaction);
        transaction.WarehouseFromId = transaction.WarehouseId;
        transaction.WarehouseToId = RequireSystemWarehouseId(_warehouseService.GetScrappingWarehouse(), "Scrapping");

        return transaction;
    }

}
