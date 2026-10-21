using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.DeliveryOrderManager.Commands;

public class CreateDeliveryOrderResult
{
    public DeliveryOrder? Data { get; set; }
}

public class CreateDeliveryOrderRequest : IRequest<CreateDeliveryOrderResult>
{
    public DateTime? DeliveryDate { get; init; }
    public string? Status { get; init; }
    public string? Description { get; init; }
    public string? SalesOrderId { get; init; }
    public string? CreatedById { get; init; }
}

public class CreateDeliveryOrderValidator : AbstractValidator<CreateDeliveryOrderRequest>
{
    public CreateDeliveryOrderValidator()
    {
        RuleFor(x => x.DeliveryDate).NotEmpty();
        RuleFor(x => x.Status).NotEmpty();
        RuleFor(x => x.SalesOrderId).NotEmpty();
    }
}

public class CreateDeliveryOrderHandler : IRequestHandler<CreateDeliveryOrderRequest, CreateDeliveryOrderResult>
{
    private readonly DeliveryOrderService _deliveryOrderService;

    public CreateDeliveryOrderHandler(
        DeliveryOrderService deliveryOrderService
        )
    {
        _deliveryOrderService = deliveryOrderService;
    }

    public async Task<CreateDeliveryOrderResult> Handle(CreateDeliveryOrderRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _deliveryOrderService.CreateAsync(
            request.DeliveryDate,
            (DeliveryOrderStatus)int.Parse(request.Status!),
            request.Description,
            request.SalesOrderId,
            request.CreatedById,
            cancellationToken);

        return new CreateDeliveryOrderResult
        {
            Data = entity
        };
    }
}
