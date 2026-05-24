using Application.Common.Repositories;
using Application.Features.InventoryTransactionManager;
using Application.Features.PurchaseOrderManager;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.GoodsReceiveManager.Commands;

public class DeleteGoodsReceiveResult
{
    public GoodsReceive? Data { get; set; }
}

public class DeleteGoodsReceiveRequest : IRequest<DeleteGoodsReceiveResult>
{
    public string? Id { get; init; }
    public string? DeletedById { get; init; }
}

public class DeleteGoodsReceiveValidator : AbstractValidator<DeleteGoodsReceiveRequest>
{
    public DeleteGoodsReceiveValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeleteGoodsReceiveHandler : IRequestHandler<DeleteGoodsReceiveRequest, DeleteGoodsReceiveResult>
{
    private readonly ICommandRepository<GoodsReceive> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly InventoryTransactionService _inventoryTransactionService;
    private readonly PurchaseOrderService _purchaseOrderService;

    public DeleteGoodsReceiveHandler(
        ICommandRepository<GoodsReceive> repository,
        IUnitOfWork unitOfWork,
        InventoryTransactionService inventoryTransactionService,
        PurchaseOrderService purchaseOrderService
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _inventoryTransactionService = inventoryTransactionService;
        _purchaseOrderService = purchaseOrderService;
    }
    public async Task<DeleteGoodsReceiveResult> Handle(DeleteGoodsReceiveRequest request, CancellationToken cancellationToken)
    {

        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        var purchaseOrderId = entity.PurchaseOrderId;
        entity.UpdatedById = request.DeletedById;

        _repository.Delete(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

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

        await _purchaseOrderService.RecalculatePOStatus(purchaseOrderId, cancellationToken);

        return new DeleteGoodsReceiveResult
        {
            Data = entity
        };
    }
}

