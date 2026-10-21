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
    private readonly GoodsReceiveService _goodsReceiveService;

    public CreateGoodsReceiveHandler(
        GoodsReceiveService goodsReceiveService
        )
    {
        _goodsReceiveService = goodsReceiveService;
    }

    public async Task<CreateGoodsReceiveResult> Handle(CreateGoodsReceiveRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _goodsReceiveService.CreateHeaderAsync(
            request.ReceiveDate,
            (GoodsReceiveStatus)int.Parse(request.Status!),
            request.Description,
            request.PurchaseOrderId,
            request.CreatedById,
            cancellationToken);

        return new CreateGoodsReceiveResult
        {
            Data = entity
        };
    }
}
