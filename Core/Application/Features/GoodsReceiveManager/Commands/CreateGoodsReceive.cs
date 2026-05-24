using Application.Common.Repositories;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.GoodsReceiveManager.Commands;

public class CreateGoodsReceiveResult
{
    public GoodsReceive? Data { get; set; }
}

public class CreateGoodsReceiveRequest : IRequest<CreateGoodsReceiveResult>
{
    public DateTime? ReceiveDate { get; init; }
    public string? Status { get; init; }
    public string? Description { get; init; }
    public string? PurchaseOrderId { get; init; }
    public string? CreatedById { get; init; }
}

public class CreateGoodsReceiveValidator : AbstractValidator<CreateGoodsReceiveRequest>
{
    public CreateGoodsReceiveValidator()
    {
        RuleFor(x => x.ReceiveDate).NotEmpty();
        RuleFor(x => x.Status).NotEmpty();
        RuleFor(x => x.PurchaseOrderId).NotEmpty();
    }
}

public class CreateGoodsReceiveHandler : IRequestHandler<CreateGoodsReceiveRequest, CreateGoodsReceiveResult>
{
    private readonly ICommandRepository<GoodsReceive> _deliveryOrderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly NumberSequenceService _numberSequenceService;

    public CreateGoodsReceiveHandler(
        ICommandRepository<GoodsReceive> deliveryOrderRepository,
        IUnitOfWork unitOfWork,
        NumberSequenceService numberSequenceService
        )
    {
        _deliveryOrderRepository = deliveryOrderRepository;
        _unitOfWork = unitOfWork;
        _numberSequenceService = numberSequenceService;
    }

    public async Task<CreateGoodsReceiveResult> Handle(CreateGoodsReceiveRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new GoodsReceive();
        entity.CreatedById = request.CreatedById;

        entity.Number = _numberSequenceService.GenerateNumber(nameof(GoodsReceive), "", "GR");
        entity.ReceiveDate = request.ReceiveDate;
        entity.Status = (GoodsReceiveStatus)int.Parse(request.Status!);
        entity.Description = request.Description;
        entity.PurchaseOrderId = request.PurchaseOrderId;

        await _deliveryOrderRepository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new CreateGoodsReceiveResult
        {
            Data = entity
        };
    }
}
