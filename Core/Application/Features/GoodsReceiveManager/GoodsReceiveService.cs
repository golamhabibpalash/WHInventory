using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Repositories;
using Application.Features.InventoryTransactionManager;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.GoodsReceiveManager;

/// <summary>
/// Shared goods-receive logic, so the ordinary goods-receive page and the purchase-order
/// "Receive Now" action record receipts the same way. Received quantities are always recorded as
/// <see cref="InventoryTransaction"/> lines linked to the goods receive by
/// <c>ModuleName</c>/<c>ModuleId</c>; once the parent is <see cref="GoodsReceiveStatus.Confirmed"/>
/// those lines add stock for real.
/// </summary>
public class GoodsReceiveService
{
    private readonly ICommandRepository<GoodsReceive> _goodsReceiveRepository;
    private readonly IQueryContext _queryContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly NumberSequenceService _numberSequenceService;
    private readonly InventoryTransactionService _inventoryTransactionService;

    public GoodsReceiveService(
        ICommandRepository<GoodsReceive> goodsReceiveRepository,
        IQueryContext queryContext,
        IUnitOfWork unitOfWork,
        NumberSequenceService numberSequenceService,
        InventoryTransactionService inventoryTransactionService
        )
    {
        _goodsReceiveRepository = goodsReceiveRepository;
        _queryContext = queryContext;
        _unitOfWork = unitOfWork;
        _numberSequenceService = numberSequenceService;
        _inventoryTransactionService = inventoryTransactionService;
    }

    /// <summary>
    /// Ordered quantity per product on the purchase order, minus everything already recorded as a
    /// goods-receive line for that order (across all of its goods receives), i.e. what is still
    /// left to receive. Products with nothing remaining are left out.
    /// </summary>
    public async Task<List<RemainingReceiveLine>> GetRemainingLinesAsync(
        string? purchaseOrderId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(purchaseOrderId))
            return new List<RemainingReceiveLine>();

        var orderedLines = await _queryContext.PurchaseOrderItem
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(x => x.PurchaseOrderId == purchaseOrderId)
            .GroupBy(x => x.ProductId)
            .Select(g => new { ProductId = g.Key, Ordered = g.Sum(x => x.Quantity ?? 0.0) })
            .ToListAsync(cancellationToken);

        if (orderedLines.Count == 0)
            return new List<RemainingReceiveLine>();

        var goodsReceiveIds = await _queryContext.GoodsReceive
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(x => x.PurchaseOrderId == purchaseOrderId)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var receivedByProduct = new Dictionary<string, double>();
        if (goodsReceiveIds.Count > 0)
        {
            var receivedRows = await _queryContext.InventoryTransaction
                .AsNoTracking()
                .ApplyIsDeletedFilter(false)
                .Where(x => x.ModuleId != null && goodsReceiveIds.Contains(x.ModuleId)
                         && x.ModuleName == nameof(GoodsReceive) && x.ProductId != null)
                .GroupBy(x => x.ProductId!)
                .Select(g => new { ProductId = g.Key, Received = g.Sum(x => x.Movement ?? 0.0) })
                .ToListAsync(cancellationToken);

            receivedByProduct = receivedRows.ToDictionary(x => x.ProductId, x => x.Received);
        }

        var result = new List<RemainingReceiveLine>();
        foreach (var line in orderedLines)
        {
            if (line.ProductId == null) continue;

            var received = receivedByProduct.GetValueOrDefault(line.ProductId, 0.0);
            var remaining = line.Ordered - received;
            if (remaining <= 0)
                continue;

            var name = await _queryContext.Product
                .Where(x => x.Id == line.ProductId)
                .Select(x => x.Name)
                .FirstOrDefaultAsync(cancellationToken) ?? line.ProductId;

            result.Add(new RemainingReceiveLine
            {
                ProductId = line.ProductId,
                ProductName = name,
                OrderedQuantity = line.Ordered,
                ReceivedQuantity = received,
                RemainingQuantity = remaining
            });
        }

        return result;
    }

    /// <summary>
    /// Creates a <see cref="GoodsReceiveStatus.Draft"/> goods receive header with no lines — the
    /// behaviour of the ordinary "Add goods receive" page. Lines are added separately.
    /// </summary>
    public async Task<GoodsReceive> CreateHeaderAsync(
        DateTime? receiveDate,
        GoodsReceiveStatus status,
        string? description,
        string? purchaseOrderId,
        string? createdById,
        CancellationToken cancellationToken = default)
    {
        var entity = new GoodsReceive();
        entity.CreatedById = createdById;

        entity.Number = _numberSequenceService.GenerateNumber(nameof(GoodsReceive), "", "GR");
        entity.ReceiveDate = receiveDate;
        entity.Status = status;
        entity.Description = description;
        entity.PurchaseOrderId = purchaseOrderId;

        await _goodsReceiveRepository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return entity;
    }

    /// <summary>
    /// Creates a <see cref="GoodsReceiveStatus.Confirmed"/> goods receive for whatever is still
    /// outstanding on the purchase order, recording one line per product at its remaining quantity
    /// and pushing the confirmed status onto those lines so the stock is added for real. The
    /// warehouse is resolved up front so a missing one aborts with nothing persisted. When nothing
    /// remains, no goods receive is created and <c>null</c> is returned.
    /// </summary>
    public async Task<GoodsReceive?> ReceiveRemainingAsync(
        DateTime? receiveDate,
        string? description,
        string? purchaseOrderId,
        string? createdById,
        CancellationToken cancellationToken = default)
    {
        var remainingLines = await GetRemainingLinesAsync(purchaseOrderId, cancellationToken);
        if (remainingLines.Count == 0)
            return null;

        var warehouseId = await GetDefaultWarehouseIdAsync(cancellationToken);
        if (warehouseId == null)
        {
            throw new ValidationException(
                "No warehouse is available to receive stock into. Create a warehouse and try again.");
        }

        var entity = new GoodsReceive();
        entity.CreatedById = createdById;

        entity.Number = _numberSequenceService.GenerateNumber(nameof(GoodsReceive), "", "GR");
        entity.ReceiveDate = receiveDate;
        entity.Status = GoodsReceiveStatus.Confirmed;
        entity.Description = description;
        entity.PurchaseOrderId = purchaseOrderId;

        await _goodsReceiveRepository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        var createdLineIds = new List<string>();
        try
        {
            foreach (var line in remainingLines)
            {
                var child = await _inventoryTransactionService.GoodsReceiveCreateInvenTrans(
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

            // Lines are created Draft (matching the goods-receive page) then confirmed together, so
            // only a Confirmed header adds stock.
            await _inventoryTransactionService.PropagateParentUpdate(
                entity.Id,
                nameof(GoodsReceive),
                entity.ReceiveDate,
                (InventoryTransactionStatus?)entity.Status,
                entity.IsDeleted,
                entity.UpdatedById,
                null,
                cancellationToken
                );
        }
        catch
        {
            // No shared transaction boundary is available, so compensate: remove the lines and the
            // goods receive this call created, leaving no half-recorded receipt behind.
            foreach (var id in createdLineIds)
            {
                await _inventoryTransactionService.GoodsReceiveDeleteInvenTrans(id, createdById, cancellationToken);
            }

            _goodsReceiveRepository.Delete(entity);
            await _unitOfWork.SaveAsync(cancellationToken);

            throw;
        }

        return entity;
    }

    /// <summary>First real (non-system) warehouse — matches the ordinary goods-receive page.</summary>
    private async Task<string?> GetDefaultWarehouseIdAsync(CancellationToken cancellationToken)
    {
        return await _queryContext.Warehouse
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(x => x.SystemWarehouse == false)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}

public record RemainingReceiveLine
{
    public string? ProductId { get; init; }
    public string? ProductName { get; init; }
    public double OrderedQuantity { get; init; }
    public double ReceivedQuantity { get; init; }
    public double RemainingQuantity { get; init; }
}
