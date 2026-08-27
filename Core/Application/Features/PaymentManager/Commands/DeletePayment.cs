using Application.Common.Repositories;
using Application.Features.PaymentManager.Services;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.PaymentManager.Commands;

public class DeletePaymentResult
{
    public Payment? Data { get; set; }
    public PaymentSummary? Summary { get; set; }
}

public class DeletePaymentRequest : IRequest<DeletePaymentResult>
{
    public string? Id { get; init; }
    public string? DeletedById { get; init; }
}

public class DeletePaymentValidator : AbstractValidator<DeletePaymentRequest>
{
    public DeletePaymentValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeletePaymentHandler : IRequestHandler<DeletePaymentRequest, DeletePaymentResult>
{
    private readonly ICommandRepository<Payment> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly PaymentService _paymentService;

    public DeletePaymentHandler(
        ICommandRepository<Payment> repository,
        IUnitOfWork unitOfWork,
        PaymentService paymentService)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _paymentService = paymentService;
    }

    public async Task<DeletePaymentResult> Handle(DeletePaymentRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.DeletedById;

        _repository.Delete(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new DeletePaymentResult
        {
            Data = entity,
            Summary = await _paymentService.GetSummaryAsync(
                entity.ModuleName, entity.ModuleId, null, cancellationToken)
        };
    }
}
