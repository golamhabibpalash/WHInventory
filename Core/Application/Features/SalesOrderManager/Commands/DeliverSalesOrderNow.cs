using Application.Common.Extensions;
using Application.Common.Repositories;
using Application.Common.Tenancy;
using Application.Features.DeliveryOrderManager;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.SalesOrderManager.Commands;

/// <summary>
/// Result of "Deliver Now": the sales order was saved by the caller, and this reports what
/// happened to its delivery so the UI can tell the two apart.
/// </summary>
public class DeliverSalesOrderNowResult
{
    public string? SalesOrderId { get; set; }

    /// <summary>True when a delivery was created and confirmed as part of this call.</summary>
    public bool Delivered { get; set; }

    /// <summary>True when the order was already fully delivered, so nothing was shipped again.</summary>
    public bool AlreadyDelivered { get; set; }

    public string? DeliveryOrderId { get; set; }
    public string? DeliveryOrderNumber { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public List<DeliverSalesOrderNowLineDto> Lines { get; set; } = new();
}

public class DeliverSalesOrderNowLineDto
{
    public string? ProductId { get; set; }
    public string? ProductName { get; set; }
    public double Quantity { get; set; }
}

public class DeliverSalesOrderNowRequest : IRequest<DeliverSalesOrderNowResult>
{
    public string? SalesOrderId { get; init; }
    public DateTime? DeliveryDate { get; init; }
    public string? Description { get; init; }
    public string? CreatedById { get; init; }
}

public class DeliverSalesOrderNowValidator : AbstractValidator<DeliverSalesOrderNowRequest>
{
    public DeliverSalesOrderNowValidator()
    {
        RuleFor(x => x.SalesOrderId).NotEmpty().WithMessage("Save the sales order before delivering.");
    }
}

public class DeliverSalesOrderNowHandler : IRequestHandler<DeliverSalesOrderNowRequest, DeliverSalesOrderNowResult>
{
    // Striped locks: one in-process gate per (tenant, sales order) hash bucket, so two concurrent
    // Deliver Now requests for the same order cannot both read the same "remaining" quantities and
    // both ship them. Bounded memory; mirrors CustomerResolutionService's in-process locking and
    // assumes a single app instance (see docker-compose). The inventory service's per-line
    // over-delivery check is the second, database-level guard.
    private static readonly SemaphoreSlim[] _stripes = Enumerable.Range(0, 64).Select(_ => new SemaphoreSlim(1, 1)).ToArray();

    private readonly ICommandRepository<SalesOrder> _salesOrderRepository;
    private readonly DeliveryOrderService _deliveryOrderService;
    private readonly ITenantContext _tenantContext;

    public DeliverSalesOrderNowHandler(
        ICommandRepository<SalesOrder> salesOrderRepository,
        DeliveryOrderService deliveryOrderService,
        ITenantContext tenantContext
        )
    {
        _salesOrderRepository = salesOrderRepository;
        _deliveryOrderService = deliveryOrderService;
        _tenantContext = tenantContext;
    }

    public async Task<DeliverSalesOrderNowResult> Handle(DeliverSalesOrderNowRequest request, CancellationToken cancellationToken = default)
    {
        var salesOrder = await _salesOrderRepository
            .GetQuery()
            .ApplyIsDeletedFilter(false)
            .Where(x => x.Id == request.SalesOrderId)
            .SingleOrDefaultAsync(cancellationToken);

        if (salesOrder == null)
        {
            throw new ValidationException("The sales order was not found. Save it again and retry.");
        }

        if (salesOrder.OrderStatus != SalesOrderStatus.Confirmed)
        {
            throw new ValidationException(
                "Only a confirmed sales order can be delivered. Set the order status to Confirmed and try again.");
        }

        var key = $"{_tenantContext.TenantId}|{salesOrder.Id}";
        var stripe = _stripes[(int)((uint)StringComparer.Ordinal.GetHashCode(key) % (uint)_stripes.Length)];

        await stripe.WaitAsync(cancellationToken);
        try
        {
            var remainingLines = await _deliveryOrderService.GetRemainingLinesAsync(salesOrder.Id, cancellationToken);
            if (remainingLines.Count == 0)
            {
                // Everything ordered has already been shipped: do not create a second delivery.
                return new DeliverSalesOrderNowResult
                {
                    SalesOrderId = salesOrder.Id,
                    Delivered = false,
                    AlreadyDelivered = true
                };
            }

            var deliveryOrder = await _deliveryOrderService.DeliverRemainingAsync(
                request.DeliveryDate,
                request.Description,
                salesOrder.Id,
                request.CreatedById,
                cancellationToken);

            if (deliveryOrder == null)
            {
                return new DeliverSalesOrderNowResult
                {
                    SalesOrderId = salesOrder.Id,
                    Delivered = false,
                    AlreadyDelivered = true
                };
            }

            return new DeliverSalesOrderNowResult
            {
                SalesOrderId = salesOrder.Id,
                Delivered = true,
                AlreadyDelivered = false,
                DeliveryOrderId = deliveryOrder.Id,
                DeliveryOrderNumber = deliveryOrder.Number,
                DeliveryDate = deliveryOrder.DeliveryDate,
                Lines = remainingLines
                    .Select(x => new DeliverSalesOrderNowLineDto
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
