using Application.Common.Extensions;
using Application.Common.Repositories;
using Application.Common.Tenancy;
using Application.Features.GoodsReceiveManager;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.PurchaseOrderManager.Commands;

/// <summary>
/// Result of "Receive Now": the purchase order was saved by the caller, and this reports what
/// happened to its receipt so the UI can tell the two apart.
/// </summary>
public class ReceivePurchaseOrderNowResult
{
    public string? PurchaseOrderId { get; set; }

    /// <summary>True when a goods receive was created and confirmed as part of this call.</summary>
    public bool Received { get; set; }

    /// <summary>True when the order was already fully received, so nothing was taken in again.</summary>
    public bool AlreadyReceived { get; set; }

    public string? GoodsReceiveId { get; set; }
    public string? GoodsReceiveNumber { get; set; }
    public DateTime? ReceiveDate { get; set; }
    public List<ReceivePurchaseOrderNowLineDto> Lines { get; set; } = new();
}

public class ReceivePurchaseOrderNowLineDto
{
    public string? ProductId { get; set; }
    public string? ProductName { get; set; }
    public double Quantity { get; set; }
}

public class ReceivePurchaseOrderNowRequest : IRequest<ReceivePurchaseOrderNowResult>
{
    public string? PurchaseOrderId { get; init; }
    public DateTime? ReceiveDate { get; init; }
    public string? Description { get; init; }
    public string? CreatedById { get; init; }
}

public class ReceivePurchaseOrderNowValidator : AbstractValidator<ReceivePurchaseOrderNowRequest>
{
    public ReceivePurchaseOrderNowValidator()
    {
        RuleFor(x => x.PurchaseOrderId).NotEmpty().WithMessage("Save the purchase order before receiving.");
    }
}

public class ReceivePurchaseOrderNowHandler : IRequestHandler<ReceivePurchaseOrderNowRequest, ReceivePurchaseOrderNowResult>
{
    // Striped locks: one in-process gate per (tenant, purchase order) hash bucket, so two concurrent
    // Receive Now requests for the same order cannot both read the same "remaining" quantities and
    // both receive them. Bounded memory; mirrors CustomerResolutionService's in-process locking and
    // assumes a single app instance (see docker-compose). The inventory service's per-line
    // over-receive check is the second, database-level guard.
    private static readonly SemaphoreSlim[] _stripes = Enumerable.Range(0, 64).Select(_ => new SemaphoreSlim(1, 1)).ToArray();

    private readonly ICommandRepository<PurchaseOrder> _purchaseOrderRepository;
    private readonly GoodsReceiveService _goodsReceiveService;
    private readonly ITenantContext _tenantContext;

    public ReceivePurchaseOrderNowHandler(
        ICommandRepository<PurchaseOrder> purchaseOrderRepository,
        GoodsReceiveService goodsReceiveService,
        ITenantContext tenantContext
        )
    {
        _purchaseOrderRepository = purchaseOrderRepository;
        _goodsReceiveService = goodsReceiveService;
        _tenantContext = tenantContext;
    }

    public async Task<ReceivePurchaseOrderNowResult> Handle(ReceivePurchaseOrderNowRequest request, CancellationToken cancellationToken = default)
    {
        var purchaseOrder = await _purchaseOrderRepository
            .GetQuery()
            .ApplyIsDeletedFilter(false)
            .Where(x => x.Id == request.PurchaseOrderId)
            .SingleOrDefaultAsync(cancellationToken);

        if (purchaseOrder == null)
        {
            throw new ValidationException("The purchase order was not found. Save it again and retry.");
        }

        if (purchaseOrder.OrderStatus != PurchaseOrderStatus.Confirmed &&
            purchaseOrder.OrderStatus != PurchaseOrderStatus.Open &&
            purchaseOrder.OrderStatus != PurchaseOrderStatus.PartiallyReceived)
        {
            throw new ValidationException(
                "Only a confirmed or partially received purchase order can be received. " +
                "Set the order status to Confirmed and try again.");
        }

        var key = $"{_tenantContext.TenantId}|{purchaseOrder.Id}";
        var stripe = _stripes[(int)((uint)StringComparer.Ordinal.GetHashCode(key) % (uint)_stripes.Length)];

        await stripe.WaitAsync(cancellationToken);
        try
        {
            var remainingLines = await _goodsReceiveService.GetRemainingLinesAsync(purchaseOrder.Id, cancellationToken);
            if (remainingLines.Count == 0)
            {
                // Everything ordered has already been received: do not create a second goods receive.
                return new ReceivePurchaseOrderNowResult
                {
                    PurchaseOrderId = purchaseOrder.Id,
                    Received = false,
                    AlreadyReceived = true
                };
            }

            var goodsReceive = await _goodsReceiveService.ReceiveRemainingAsync(
                request.ReceiveDate,
                request.Description,
                purchaseOrder.Id,
                request.CreatedById,
                cancellationToken);

            if (goodsReceive == null)
            {
                return new ReceivePurchaseOrderNowResult
                {
                    PurchaseOrderId = purchaseOrder.Id,
                    Received = false,
                    AlreadyReceived = true
                };
            }

            return new ReceivePurchaseOrderNowResult
            {
                PurchaseOrderId = purchaseOrder.Id,
                Received = true,
                AlreadyReceived = false,
                GoodsReceiveId = goodsReceive.Id,
                GoodsReceiveNumber = goodsReceive.Number,
                ReceiveDate = goodsReceive.ReceiveDate,
                Lines = remainingLines
                    .Select(x => new ReceivePurchaseOrderNowLineDto
                    {
                        ProductId = x.ProductId,
                        ProductName = x.ProductName,
                        Quantity = x.RemainingQuantity
                    })
                    .ToList()
            };
        }
        finally
        {
            stripe.Release();
        }
    }
}
