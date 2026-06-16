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

public class UpdateSalesOrderItemResult
{
    public SalesOrderItem? Data { get; set; }
}

public class UpdateSalesOrderItemRequest : IRequest<UpdateSalesOrderItemResult>
{
    public string? Id { get; init; }
    public string? SalesOrderId { get; init; }
    public string? ProductId { get; init; }
    public string? Remark { get; init; }
    public double? UnitPrice { get; init; }
    public double? Quantity { get; init; }
    public string? UpdatedById { get; init; }
}

public class UpdateSalesOrderItemValidator : AbstractValidator<UpdateSalesOrderItemRequest>
{
    public UpdateSalesOrderItemValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.SalesOrderId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.UnitPrice).NotEmpty();
        RuleFor(x => x.Quantity).NotEmpty();
    }
}

public class UpdateSalesOrderItemHandler : IRequestHandler<UpdateSalesOrderItemRequest, UpdateSalesOrderItemResult>
{
    private readonly ICommandRepository<SalesOrderItem> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly SalesOrderService _salesOrderService;
    private readonly IQueryContext _queryContext;
    private readonly ILogger<UpdateSalesOrderItemHandler> _logger;

    public UpdateSalesOrderItemHandler(
        ICommandRepository<SalesOrderItem> repository,
        IUnitOfWork unitOfWork,
        SalesOrderService salesOrderService,
        IQueryContext queryContext,
        ILogger<UpdateSalesOrderItemHandler> logger
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _salesOrderService = salesOrderService;
        _queryContext = queryContext;
        _logger = logger;
    }

    public async Task<UpdateSalesOrderItemResult> Handle(UpdateSalesOrderItemRequest request, CancellationToken cancellationToken)
    {
        await ValidateStockAsync(request.ProductId, request.Quantity, request.UpdatedById, cancellationToken);

        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.UpdatedById;

        entity.SalesOrderId = request.SalesOrderId;
        entity.ProductId = request.ProductId;
        entity.Summary = request.Remark;
        entity.UnitPrice = request.UnitPrice;
        entity.Quantity = request.Quantity;

        entity.Total = entity.UnitPrice * entity.Quantity;

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        await _salesOrderService.Recalculate(entity.SalesOrderId ?? "");

        return new UpdateSalesOrderItemResult
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
                "Stock validation failed on SalesOrderItem update. UserId={UserId} ProductId={ProductId} ProductName={ProductName} Available={Available} Requested={Requested}",
                userId, productId, product.Name, availableStock, requestedQty);

            throw new Exception(
                $"Insufficient stock for '{product.Name}'. " +
                $"Available: {availableStock:N2}, Requested: {requestedQty:N2}. " +
                $"Cannot update this Sales Order item.");
        }
    }
}

