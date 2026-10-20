using Application.Common.Repositories;
using Application.Features.CustomerManager.Services;
using Domain.Entities;
using Domain.Enums;
using static Domain.Common.Constants;
using FluentValidation;
using MediatR;

namespace Application.Features.SalesOrderManager.Commands;

public class UpdateSalesOrderResult
{
    public SalesOrder? Data { get; set; }
}

public class UpdateSalesOrderRequest : IRequest<UpdateSalesOrderResult>
{
    public string? Id { get; init; }
    public DateTime? OrderDate { get; init; }
    public string? OrderStatus { get; init; }
    public string? Description { get; init; }
    public string? CustomerId { get; init; }

    /// <summary>Alternative to <see cref="CustomerId"/>; see <see cref="CreateSalesOrderRequest.CustomerPhoneNumber"/>.</summary>
    public string? CustomerPhoneNumber { get; init; }
    public string? CustomerName { get; init; }
    public string? TaxId { get; init; }
    public string? UpdatedById { get; init; }
}

public class UpdateSalesOrderValidator : AbstractValidator<UpdateSalesOrderRequest>
{
    public UpdateSalesOrderValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.OrderDate).NotEmpty();
        RuleFor(x => x.OrderStatus).NotEmpty();
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Customer is required.")
            .When(x => string.IsNullOrWhiteSpace(x.CustomerPhoneNumber));
        RuleFor(x => x.CustomerPhoneNumber)
            .Must(CustomerMobileNumber.IsValid).WithMessage(CustomerMobileNumber.InvalidMessage)
            .When(x => !string.IsNullOrWhiteSpace(x.CustomerPhoneNumber));
        RuleFor(x => x.CustomerName).MaximumLength(NameConsts.MaxLength);
        RuleFor(x => x.TaxId).NotEmpty();
    }
}

public class UpdateSalesOrderHandler : IRequestHandler<UpdateSalesOrderRequest, UpdateSalesOrderResult>
{
    private readonly ICommandRepository<SalesOrder> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly SalesOrderService _salesOrderService;
    private readonly CustomerResolutionService _customerResolutionService;

    public UpdateSalesOrderHandler(
        ICommandRepository<SalesOrder> repository,
        SalesOrderService salesOrderService,
        IUnitOfWork unitOfWork,
        CustomerResolutionService customerResolutionService
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _salesOrderService = salesOrderService;
        _customerResolutionService = customerResolutionService;
    }

    public async Task<UpdateSalesOrderResult> Handle(UpdateSalesOrderRequest request, CancellationToken cancellationToken)
    {

        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        var mobile = CustomerMobileNumber.TryNormalize(request.CustomerPhoneNumber, out var normalized) ? normalized : null;

        await _customerResolutionService.RunWithMobileLockAsync(mobile, async () =>
        {
            // Resolve before touching the order: creating a customer number saves the context,
            // and the order's changes must only be written by the SaveAsync below.
            string? customerId;
            if (mobile != null)
            {
                var customer = await _customerResolutionService.ResolveByMobileAsync(mobile, request.CustomerName, request.UpdatedById, cancellationToken);
                customerId = customer.Id;
            }
            else
            {
                // Only a changed customer is verified, so orders whose customer was later
                // soft-deleted can still be edited.
                if (request.CustomerId != entity.CustomerId)
                {
                    await _customerResolutionService.EnsureCustomerExistsAsync(request.CustomerId, cancellationToken);
                }
                customerId = request.CustomerId;
            }

            entity.UpdatedById = request.UpdatedById;

            entity.OrderDate = request.OrderDate;
            entity.OrderStatus = (SalesOrderStatus)int.Parse(request.OrderStatus!);
            entity.Description = request.Description;
            entity.CustomerId = customerId;
            entity.TaxId = request.TaxId;

            _repository.Update(entity);
            await _unitOfWork.SaveAsync(cancellationToken);
        }, cancellationToken);

        await _salesOrderService.Recalculate(entity.Id);

        return new UpdateSalesOrderResult
        {
            Data = entity
        };
    }
}

