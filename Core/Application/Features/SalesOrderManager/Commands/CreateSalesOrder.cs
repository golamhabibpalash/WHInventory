using Application.Common.Repositories;
using Application.Features.CustomerManager.Services;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using Domain.Enums;
using static Domain.Common.Constants;
using FluentValidation;
using MediatR;

namespace Application.Features.SalesOrderManager.Commands;

public class CreateSalesOrderResult
{
    public SalesOrder? Data { get; set; }
}

public class CreateSalesOrderRequest : IRequest<CreateSalesOrderResult>
{
    public DateTime? OrderDate { get; init; }
    public string? OrderStatus { get; init; }
    public string? Description { get; init; }
    public string? CustomerId { get; init; }

    /// <summary>
    /// Alternative to <see cref="CustomerId"/>: the customer's mobile number. The server reuses the
    /// customer with that number or creates one, in the same save as the order.
    /// </summary>
    public string? CustomerPhoneNumber { get; init; }

    /// <summary>Optional name for a customer created from <see cref="CustomerPhoneNumber"/>; defaults to the number.</summary>
    public string? CustomerName { get; init; }
    public string? TaxId { get; init; }
    public string? CreatedById { get; init; }
}

public class CreateSalesOrderValidator : AbstractValidator<CreateSalesOrderRequest>
{
    public CreateSalesOrderValidator()
    {
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

public class CreateSalesOrderHandler : IRequestHandler<CreateSalesOrderRequest, CreateSalesOrderResult>
{
    private readonly ICommandRepository<SalesOrder> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly NumberSequenceService _numberSequenceService;
    private readonly SalesOrderService _salesOrderService;
    private readonly CustomerResolutionService _customerResolutionService;

    public CreateSalesOrderHandler(
        ICommandRepository<SalesOrder> repository,
        IUnitOfWork unitOfWork,
        NumberSequenceService numberSequenceService,
        SalesOrderService salesOrderService,
        CustomerResolutionService customerResolutionService
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _numberSequenceService = numberSequenceService;
        _salesOrderService = salesOrderService;
        _customerResolutionService = customerResolutionService;
    }

    public async Task<CreateSalesOrderResult> Handle(CreateSalesOrderRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new SalesOrder();
        entity.CreatedById = request.CreatedById;

        entity.Number = _numberSequenceService.GenerateNumber(nameof(SalesOrder), "", "SO");
        entity.OrderDate = request.OrderDate;
        entity.OrderStatus = (SalesOrderStatus)int.Parse(request.OrderStatus!);
        entity.Description = request.Description;
        entity.TaxId = request.TaxId;

        // The order number is generated above, before any customer is staged: GenerateNumber
        // saves the context, which would otherwise commit a new customer ahead of the order.
        var mobile = CustomerMobileNumber.TryNormalize(request.CustomerPhoneNumber, out var normalized) ? normalized : null;

        await _customerResolutionService.RunWithMobileLockAsync(mobile, async () =>
        {
            if (mobile != null)
            {
                var customer = await _customerResolutionService.ResolveByMobileAsync(mobile, request.CustomerName, request.CreatedById, cancellationToken);
                entity.CustomerId = customer.Id;
            }
            else
            {
                await _customerResolutionService.EnsureCustomerExistsAsync(request.CustomerId, cancellationToken);
                entity.CustomerId = request.CustomerId;
            }

            // One SaveChanges: a new customer and the order commit (or fail) together.
            await _repository.CreateAsync(entity, cancellationToken);
            await _unitOfWork.SaveAsync(cancellationToken);
        }, cancellationToken);

        await _salesOrderService.Recalculate(entity.Id);

        return new CreateSalesOrderResult
        {
            Data = entity
        };
    }
}
