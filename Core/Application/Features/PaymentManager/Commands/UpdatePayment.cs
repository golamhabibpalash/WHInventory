using Application.Common.Extensions;
using Application.Common.Repositories;
using Application.Features.PaymentManager.Services;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.PaymentManager.Commands;

public class UpdatePaymentResult
{
    public Payment? Data { get; set; }
    public PaymentSummary? Summary { get; set; }
}

public class UpdatePaymentRequest : IRequest<UpdatePaymentResult>
{
    public string? Id { get; init; }
    public DateTime? PaymentDate { get; init; }
    public double? Amount { get; init; }
    public string? PaymentMethodId { get; init; }
    public string? ReferenceNumber { get; init; }
    public string? Notes { get; init; }
    public string? UpdatedById { get; init; }
}

public class UpdatePaymentValidator : AbstractValidator<UpdatePaymentRequest>
{
    public UpdatePaymentValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.PaymentDate).NotEmpty();
        RuleFor(x => x.PaymentMethodId).NotEmpty();
        RuleFor(x => x.Amount)
            .NotEmpty()
            .GreaterThan(0).WithMessage("Payment amount must be greater than zero.");
    }
}

public class UpdatePaymentHandler : IRequestHandler<UpdatePaymentRequest, UpdatePaymentResult>
{
    private readonly ICommandRepository<Payment> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly PaymentService _paymentService;

    public UpdatePaymentHandler(
        ICommandRepository<Payment> repository,
        IUnitOfWork unitOfWork,
        PaymentService paymentService)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _paymentService = paymentService;
    }

    public async Task<UpdatePaymentResult> Handle(UpdatePaymentRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        // Outstanding is measured with this payment excluded, so editing it in place
        // is checked against what the rest of the payments leave owing.
        var summary = await _paymentService.GetSummaryAsync(
            entity.ModuleName, entity.ModuleId, entity.Id, cancellationToken);

        var amount = (request.Amount ?? 0).ToMoney();

        if (amount > summary.AmountOutstanding + PaymentService.Tolerance)
        {
            throw new Exception(
                $"Payment of {amount:N2} exceeds the outstanding balance on {summary.ModuleNumber} " +
                $"({summary.AmountOutstanding:N2} of {summary.DocumentTotal:N2}). " +
                $"Reduce the amount to save this payment.");
        }

        entity.UpdatedById = request.UpdatedById;
        entity.PaymentDate = request.PaymentDate;
        entity.Amount = amount;
        entity.PaymentMethodId = request.PaymentMethodId;
        entity.ReferenceNumber = string.IsNullOrWhiteSpace(request.ReferenceNumber) ? null : request.ReferenceNumber.Trim();
        entity.Notes = request.Notes;

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new UpdatePaymentResult
        {
            Data = entity,
            Summary = await _paymentService.GetSummaryAsync(
                entity.ModuleName, entity.ModuleId, null, cancellationToken)
        };
    }
}
