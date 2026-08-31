using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Repositories;
using Application.Features.SalesOrderManager;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.SalesOrderItemManager.Commands;

public class CreateSalesOrderItemResult
{
    public SalesOrderItem? Data { get; set; }
}

public class CreateSalesOrderItemRequest : IRequest<CreateSalesOrderItemResult>
{
    public string? SalesOrderId { get; init; }
    public string? ProductId { get; init; }
    public string? Remark { get; init; }
    public double? UnitPrice { get; init; }
    public double? Quantity { get; init; }
    public string? CreatedById { get; init; }
}

public class CreateSalesOrderItemValidator : AbstractValidator<CreateSalesOrderItemRequest>
{
    public CreateSalesOrderItemValidator()
    {
        RuleFor(x => x.SalesOrderId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.UnitPrice).NotEmpty();
        RuleFor(x => x.Quantity).NotEmpty();
    }
}

public class CreateSalesOrderItemHandler : IRequestHandler<CreateSalesOrderItemRequest, CreateSalesOrderItemResult>
{
    private readonly ICommandRepository<SalesOrderItem> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly SalesOrderService _salesOrderService;
    private readonly IQueryContext _queryContext;
    private readonly ILogger<CreateSalesOrderItemHandler> _logger;

    public CreateSalesOrderItemHandler(
        ICommandRepository<SalesOrderItem> repository,
        IUnitOfWork unitOfWork,
        SalesOrderService salesOrderService,
        IQueryContext queryContext,
        ILogger<CreateSalesOrderItemHandler> logger
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _salesOrderService = salesOrderService;
        _queryContext = queryContext;
        _logger = logger;
    }

    public async Task<CreateSalesOrderItemResult> Handle(CreateSalesOrderItemRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateStockAsync(request.ProductId, request.Quantity, request.CreatedById, cancellationToken);
        await ValidateSellingPriceAsync(request.ProductId, request.UnitPrice, request.CreatedById, cancellationToken);

        var entity = new SalesOrderItem();
        entity.CreatedById = request.CreatedById;

        entity.SalesOrderId = request.SalesOrderId;
        entity.ProductId = request.ProductId;
        entity.Summary = request.Remark;
        entity.UnitPrice = request.UnitPrice;
        entity.Quantity = request.Quantity;

        entity.Total = (entity.Quantity * entity.UnitPrice).ToMoney();

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        await _salesOrderService.Recalculate(entity.SalesOrderId ?? "");

        return new CreateSalesOrderItemResult
        {
            Data = entity
        };
    }

    private async Task ValidateStockAsync(string? productId, double? requestedQty, string? userId, CancellationToken cancellationToken)
    {
        var product = await _queryContext.Product
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(x => x.Id == productId)
            .Select(x => new { x.Name, x.Physical })
            .FirstOrDefaultAsync(cancellationToken);

        if (product == null || product.Physical != true)
            return;

        var allowNegativeStock = await _queryContext.Company
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Select(x => x.AllowNegativeStock)
            .FirstOrDefaultAsync(cancellationToken);

        if (allowNegativeStock)
            return;

        var availableStock = await _queryContext.InventoryTransaction
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(x =>
                x.Status == InventoryTransactionStatus.Confirmed &&
                x.ProductId == productId &&
                x.Warehouse != null &&
                x.Warehouse.SystemWarehouse == false)
            .SumAsync(x => (double?)x.Stock ?? 0.0, cancellationToken);

        if ((requestedQty ?? 0) > availableStock)
        {
            _logger.LogWarning(
                "Stock validation failed on SalesOrderItem create. UserId={UserId} ProductId={ProductId} ProductName={ProductName} Available={Available} Requested={Requested}",
                userId, productId, product.Name, availableStock, requestedQty);

            throw new Exception(
                $"Insufficient stock for '{product.Name}'. " +
                $"Available: {availableStock:N2}, Requested: {requestedQty:N2}. " +
                $"Cannot add this Sales Order item.");
        }
    }

    private async Task ValidateSellingPriceAsync(string? productId, double? unitPrice, string? userId, CancellationToken cancellationToken)
    {
        var product = await _queryContext.Product
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(x => x.Id == productId)
            .Select(x => new { x.Name, x.MinSellingPrice, x.MaxSellingPrice })
            .FirstOrDefaultAsync(cancellationToken);

        if (product == null)
            return;

        if (product.MinSellingPrice == null && product.MaxSellingPrice == null)
            return;

        var allowPriceOutsideBand = await _queryContext.Company
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Select(x => x.AllowPriceOutsideBand)
            .FirstOrDefaultAsync(cancellationToken);

        if (allowPriceOutsideBand)
            return;

        var price = unitPrice ?? 0;

        if (product.MinSellingPrice.HasValue && price < product.MinSellingPrice.Value)
        {
            _logger.LogWarning(
                "Selling price validation failed on SalesOrderItem {Operation}. UserId={UserId} ProductId={ProductId} ProductName={ProductName} Min={Min} Requested={Requested}",
                "create", userId, productId, product.Name, product.MinSellingPrice, price);

            throw new Exception(
                $"Unit price {price:N2} is below the minimum selling price for '{product.Name}' " +
                $"({product.MinSellingPrice:N2}). Cannot save this Sales Order item.");
        }

        if (product.MaxSellingPrice.HasValue && price > product.MaxSellingPrice.Value)
        {
            _logger.LogWarning(
                "Selling price validation failed on SalesOrderItem {Operation}. UserId={UserId} ProductId={ProductId} ProductName={ProductName} Max={Max} Requested={Requested}",
                "create", userId, productId, product.Name, product.MaxSellingPrice, price);

            throw new Exception(
                $"Unit price {price:N2} exceeds the maximum selling price for '{product.Name}' " +
                $"({product.MaxSellingPrice:N2}). Cannot save this Sales Order item.");
        }
    }
}
