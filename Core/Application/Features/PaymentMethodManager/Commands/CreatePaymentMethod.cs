using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.PaymentMethodManager.Commands;

public class CreatePaymentMethodResult
{
    public PaymentMethod? Data { get; set; }
}

public class CreatePaymentMethodRequest : IRequest<CreatePaymentMethodResult>
{
    public string? Name { get; init; }
    public string? Code { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; } = true;
    public string? CreatedById { get; init; }
}

public class CreatePaymentMethodValidator : AbstractValidator<CreatePaymentMethodRequest>
{
    public CreatePaymentMethodValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}

public class CreatePaymentMethodHandler : IRequestHandler<CreatePaymentMethodRequest, CreatePaymentMethodResult>
{
    private readonly ICommandRepository<PaymentMethod> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreatePaymentMethodHandler(ICommandRepository<PaymentMethod> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreatePaymentMethodResult> Handle(CreatePaymentMethodRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new PaymentMethod
        {
            CreatedById = request.CreatedById,
            Name = request.Name,
            Code = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim(),
            Description = request.Description,
            SystemMethod = false,
            IsActive = request.IsActive
        };

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new CreatePaymentMethodResult { Data = entity };
    }
}
