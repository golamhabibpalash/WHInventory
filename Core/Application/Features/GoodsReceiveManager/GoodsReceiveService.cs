using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Repositories;
using Application.Common.Tenancy;
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
/// <c>ModuleName</c>/<c>ModuleId</c>; only <see cref="InventoryTransactionStatus.Confirmed"/> lines
/// count as received — a Draft goods receive reserves nothing.
/// </summary>
public class GoodsReceiveService
{
    // Striped locks: one in-process gate per (tenant, purchase order) hash bucket, so two concurrent
    // operations for the same order (Receive Now vs. an item save that syncs the draft goods receive)
    // cannot interleave. Bounded memory; mirrors CustomerResolutionService's in-process locking and
    // assumes a single app instance (see docker-compose). The inventory service's per-line
    // over-receive check is the second, database-level guard. SemaphoreSlim is not reentrant, so
    // public methods that take this lock must never call each other.
    private static readonly SemaphoreSlim[] _stripes = Enumerable.Range(0, 64).Select(_ => new SemaphoreSlim(1, 1)).ToArray();

    private readonly ICommandRepository<GoodsReceive> _goodsReceiveRepository;
    private readonly IQueryContext _queryContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly NumberSequenceService _numberSequenceService;
    private readonly InventoryTransactionService _inventoryTransactionService;
    private readonly ITenantContext _tenantContext;

    public GoodsReceiveService(
        ICommandRepository<GoodsReceive> goodsReceiveRepository,
        IQueryContext queryContext,
        IUnitOfWork unitOfWork,
        NumberSequenceService numberSequenceService,
        InventoryTransactionService inventoryTransactionService,
        ITenantContext tenantContext
        )
    {
        _goodsReceiveRepository = goodsReceiveRepository;
        _queryContext = queryContext;
        _unitOfWork = unitOfWork;
        _numberSequenceService = numberSequenceService;
        _inventoryTransactionService = inventoryTransactionService;
        _tenantContext = tenantContext;
    }

    private static SemaphoreSlim GetStripe(string key)
    {
        return _stripes[(int)((uint)StringComparer.Ordinal.GetHashCode(key) % (uint)_stripes.Length)];
    }

    /// <summary>
    /// Ordered quantity per product on the purchase order, minus everything already received as a
    /// <see cref="InventoryTransactionStatus.Confirmed"/> goods-receive line for that order (across
    /// all of its goods receives), i.e. what is still left to receive. Draft goods-receive lines do
    /// not count as received. Products with nothing remaining are left out.
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
                         && x.ModuleName == nameof(GoodsReceive) && x.ProductId != null
                         && x.Status == InventoryTransactionStatus.Confirmed)
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
    /// Keeps exactly one <see cref="GoodsReceiveStatus.Draft"/> goods receive in step with what is
    /// still left to receive on a purchase order: its lines mirror the remaining quantities per
    /// product, and it is removed when nothing remains or the order is cancelled, archived or
    /// deleted. Called after every purchase-order and purchase-order-item write. Never throws for
    /// a missing warehouse — without one there is simply nothing to receive into.
    /// </summary>
    public async Task SyncDraftForPurchaseOrderAsync(
        string? purchaseOrderId,
        string? createdById,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(purchaseOrderId))
            return;

        var key = $"{_tenantContext.TenantId}|{purchaseOrderId}";
        var stripe = GetStripe(key);
        await stripe.WaitAsync(cancellationToken);
        try
        {
            var purchaseOrder = await _queryContext.PurchaseOrder
                .AsNoTracking()
                .ApplyIsDeletedFilter(false)
                .Where(x => x.Id == purchaseOrderId)
                .FirstOrDefaultAsync(cancellationToken);

            if (purchaseOrder == null ||
                purchaseOrder.OrderStatus == PurchaseOrderStatus.Cancelled ||
                purchaseOrder.OrderStatus == PurchaseOrderStatus.Archived)
            {
                await RemoveDraftGoodsReceivesAsync(purchaseOrderId, createdById, cancellationToken);
                return;
            }

            var remainingLines = await GetRemainingLinesAsync(purchaseOrderId, cancellationToken);
            if (remainingLines.Count == 0)
            {
                await RemoveDraftGoodsReceivesAsync(purchaseOrderId, createdById, cancellationToken);
                return;
            }

            var warehouseId = await GetDefaultWarehouseIdAsync(cancellationToken);
            if (warehouseId == null)
                return;

            var draftId = await _queryContext.GoodsReceive
                .AsNoTracking()
                .ApplyIsDeletedFilter(false)
                .Where(x => x.PurchaseOrderId == purchaseOrderId && x.Status == GoodsReceiveStatus.Draft)
                .OrderBy(x => x.Number)
                .Select(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            var entity = draftId != null
                ? await _goodsReceiveRepository.GetAsync(draftId, cancellationToken)
                : null;

            if (entity == null)
            {
                entity = await CreateHeaderAsync(
                    purchaseOrder.OrderDate,
                    GoodsReceiveStatus.Draft,
                    purchaseOrder.Description,
                    purchaseOrder.Id,
                    createdById ?? purchaseOrder.CreatedById,
                    cancellationToken);
            }
            else
            {
                entity.UpdatedById = createdById;
                entity.ReceiveDate = purchaseOrder.OrderDate;
                entity.Description = purchaseOrder.Description;
                _goodsReceiveRepository.Update(entity);
                await _unitOfWork.SaveAsync(cancellationToken);
            }

            await SyncDraftLinesAsync(entity, warehouseId, remainingLines, createdById ?? entity.CreatedById, cancellationToken);
        }
        finally
        {
            stripe.Release();
        }
    }

    /// <summary>
    /// Creates a <see cref="GoodsReceiveStatus.Confirmed"/> goods receive for whatever is still
    /// outstanding on the purchase order, recording one line per product at its remaining quantity
    /// and pushing the confirmed status onto those lines so the stock is added for real. Any Draft
    /// goods receive on the order is removed first (it would otherwise hold the same quantities).
    /// The warehouse is resolved up front so a missing one aborts with nothing persisted. When
    /// nothing remains, no goods receive is created and <c>null</c> is returned.
    /// </summary>
    public async Task<GoodsReceive?> ReceiveRemainingAsync(
        DateTime? receiveDate,
        string? description,
        string? purchaseOrderId,
        string? createdById,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(purchaseOrderId))
            return null;

        var key = $"{_tenantContext.TenantId}|{purchaseOrderId}";
        var stripe = GetStripe(key);
        await stripe.WaitAsync(cancellationToken);
        try
        {
            await RemoveDraftGoodsReceivesAsync(purchaseOrderId, createdById, cancellationToken);

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
        finally
        {
            stripe.Release();
        }
    }

    /// <summary>Makes the draft goods receive's lines mirror the remaining quantities exactly.</summary>
    private async Task SyncDraftLinesAsync(
        GoodsReceive entity,
        string warehouseId,
        List<RemainingReceiveLine> remainingLines,
        string? updatedById,
        CancellationToken cancellationToken)
    {
        var existingLines = await _queryContext.InventoryTransaction
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(x => x.ModuleId == entity.Id && x.ModuleName == nameof(GoodsReceive))
            .ToListAsync(cancellationToken);

        var usedLineIds = new HashSet<string>();

        foreach (var line in remainingLines)
        {
            var match = existingLines.FirstOrDefault(x => x.ProductId == line.ProductId && !usedLineIds.Contains(x.Id));
            if (match == null)
            {
                var child = await _inventoryTransactionService.GoodsReceiveCreateInvenTrans(
                    entity.Id,
                    warehouseId,
                    line.ProductId,
                    line.RemainingQuantity,
                    updatedById,
                    cancellationToken);
                if (child.Id != null)
                    usedLineIds.Add(child.Id);
                continue;
            }

            usedLineIds.Add(match.Id);
            if ((match.Movement ?? 0.0) != line.RemainingQuantity || match.WarehouseId != warehouseId)
            {
                await _inventoryTransactionService.GoodsReceiveUpdateInvenTrans(
                    match.Id,
                    warehouseId,
                    line.ProductId,
                    line.RemainingQuantity,
                    updatedById,
                    cancellationToken);
            }
        }

        foreach (var line in existingLines)
        {
            if (line.Id != null && !usedLineIds.Contains(line.Id))
            {
                await _inventoryTransactionService.GoodsReceiveDeleteInvenTrans(
                    line.Id,
                    updatedById,
                    cancellationToken);
            }
        }
    }

    /// <summary>Soft-deletes every Draft goods receive on the order, lines first then header.</summary>
    private async Task RemoveDraftGoodsReceivesAsync(
        string purchaseOrderId,
        string? deletedById,
        CancellationToken cancellationToken)
    {
        var draftIds = await _queryContext.GoodsReceive
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(x => x.PurchaseOrderId == purchaseOrderId && x.Status == GoodsReceiveStatus.Draft)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        foreach (var draftId in draftIds)
        {
            var lineIds = await _queryContext.InventoryTransaction
                .AsNoTracking()
                .ApplyIsDeletedFilter(false)
                .Where(x => x.ModuleId == draftId && x.ModuleName == nameof(GoodsReceive))
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);

            foreach (var lineId in lineIds)
            {
                await _inventoryTransactionService.GoodsReceiveDeleteInvenTrans(lineId, deletedById, cancellationToken);
            }

            var draft = await _goodsReceiveRepository.GetAsync(draftId, cancellationToken);
            if (draft != null)
            {
                draft.UpdatedById = deletedById;
                _goodsReceiveRepository.Delete(draft);
                await _unitOfWork.SaveAsync(cancellationToken);
            }
        }
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
